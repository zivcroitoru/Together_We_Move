# Hebrew rendering

The existing TMP RTL flag alone does not reorder mixed Hebrew/Latin/numeric runs. The project now uses the RTLTMPro runtime processor at pinned commit `f480419bbbffed1be3c129d68cc0182afcfbcac3`, vendored with its MIT license in `Assets/ThirdParty/RTLTMPro`. No package manifest changes were needed for this integration.

## Existing UI

73 TMP components across 33 scene/prefab files now have `HebrewTextSupport`. It chains any existing preprocessor, uses RTLTMPro's number/tag-aware processing, and leaves the serialized source and text getter in logical order. It does not alter sizes, colors, alignment, margins, spacing or animations. English-only text bypasses processing. Explicit paragraphs retain their original order. A dirty callback routes TMP's SetText/SetCharArray fast paths through preprocessing as well.

12 legacy Text components in the TMP example scenes now use the Text-compatible `HebrewLegacyText` subclass, preserving their component file IDs and references. It processes the generated display text, not the serialized translation. Legacy line wrapping is measured before processing each line. English-only legacy text uses the original renderer.

The bundled Liberation Sans TTF includes Hebrew letters and final forms. A shared dynamic fallback atlas fills missing Hebrew glyphs while keeping authored TMP fonts/materials. A copy of this existing font is in `Resources/Localization/Hebrew.ttf` so it is also present in player builds. Legacy labels with missing Hebrew glyphs use this source for rendering. Different fonts can have different glyph metrics; check wrapping on the target device.

## New UI and dynamic updates

- Continue writing normal logical Hebrew strings, for example `label.text = "ציון 12.5%"`. Do not reverse strings or pre-fix translations.
- New TMP and TMP UGUI labels are discovered automatically before canvas/camera rendering at runtime. For immediate `ForceMeshUpdate()` on a newly created label, first call `HebrewTextSupport.Ensure(label)`.
- For an Editor preview, add HebrewTextSupport or choose **Tools > Hebrew > Prepare loaded TMP text**. Save the scene or prefab.
- For new legacy labels use **Add Component > UI > Hebrew Legacy Text**, which remains compatible with code typed as `UnityEngine.UI.Text`.
- Keep the existing alignment and font settings. Do not also run another RTL fixer on the same text; this would process it twice.
- TMP rich-text parsing remains enabled according to each component's existing setting. Test complex nested rich text and legacy rich text visually; RTLTMPro is not a claim of complete Unicode bidi conformance.

## Verification and limits

C# compilation and isolated RTLTMPro cases cover Hebrew, final letters, Latin words, digit order, decimals, percentages, brackets and rich text. The original source strings in scenes/prefabs remain unchanged. The two original hidden win labels contain `!הפי`, which appears to be previously pre-reversed text. It has deliberately not been rewritten: correcting already reversed source content requires an author decision, rather than guessing a translation.

The Editor in this workspace previously failed to start because no valid Unity license was available. Live rendering, wrapping, animation and visual punctuation placement still need verification on a licensed Editor. Check both levels, both hint bubbles, results, history, a mixed label such as `שלום Unity 123!`, `ציון 12.5%`, `שלב (2/10)`, final letters `ך ם ן ף ץ`, an English-only label, explicit newlines, soft wrapping, and an update through both `.text` and `.SetText(...)`. Include legacy rich text in that check. Do not treat the isolated processing checks as a screenshot-based visual pass.
