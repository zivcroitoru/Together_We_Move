using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Legacy Text-compatible rendering adapter. Text stays in logical order
/// in the Inspector, scripts and animations. Existing Text references remain valid.</summary>
[AddComponentMenu("UI/Hebrew Legacy Text")]
public sealed class HebrewLegacyText : Text
{
    private readonly FastStringBuilder buffer = new FastStringBuilder(2048);

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        if (!HebrewTextSupport.HasHebrew(m_Text)) { base.OnPopulateMesh(helper); return; }
        string logical = m_Text;
        Font authoredFont = font;
        try
        {
            foreach (char c in logical)
                if (c >= '\u0590' && c <= '\u05ff' && (!font || !font.HasCharacter(c)))
                { font = HebrewTextSupport.HebrewFont; break; }
            // Determine wrapping in logical order first, then apply bidi per displayed line.
            cachedTextGenerator.Populate(logical, GetGenerationSettings(rectTransform.rect.size));
            var lines = cachedTextGenerator.lines;
            var rendered = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                int start = lines[i].startCharIdx;
                int end = i + 1 < lines.Count ? lines[i + 1].startCharIdx : logical.Length;
                string line = logical.Substring(start, end - start).TrimEnd('\r', '\n');
                if (i > 0) rendered.Append('\n');
                rendered.Append(HebrewTextSupport.Fix(line, buffer, false, supportRichText));
            }
            m_Text = rendered.ToString();
            base.OnPopulateMesh(helper);
        }
        finally { m_Text = logical; if (font != authoredFont) font = authoredFont; }
    }
}
