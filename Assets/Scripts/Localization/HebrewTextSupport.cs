using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RTLTMPro;

/// <summary>Rendering-only RTL adapter. The serialized text and text getter stay logical.
/// Uses RTLTMPro's number/tag-aware processing, rather than reversing source strings.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class HebrewTextSupport : MonoBehaviour, ITextPreprocessor
{
    private static Font source;

    public static void Ensure(TMP_Text text)
    {
        if (!text.GetComponent<HebrewTextSupport>()) text.gameObject.AddComponent<HebrewTextSupport>();
    }

    private void OnEnable() { enabled = false; }

    internal static bool HasHebrew(string text)
    {
        if (text == null) return false;
        foreach (char c in text) if (c >= '\u0590' && c <= '\u05ff') return true;
        return false;
    }

    public string PreprocessText(string text) => text;

    internal static string Fix(string text, FastStringBuilder output, bool tmpLayout, bool richText)
    {
        // Process paragraphs independently so explicit newlines retain their order.
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (!HasHebrew(lines[i]))
            {
                if (tmpLayout) { output.SetValue(lines[i]); output.Reverse(); lines[i] = output.ToString(); }
                continue;
            }
            output.Clear();
            RTLSupport.FixRTL(lines[i], output, farsi: false, fixTextTags: richText, preserveNumbers: true);
            if (tmpLayout) output.Reverse(); // RTLTMPro's own buffer operation, never source text.
            lines[i] = output.ToString();
        }
        return string.Join("\n", lines);
    }

    internal static Font HebrewFont => source ? source : (source = Resources.Load<Font>("Localization/Hebrew"));

}
