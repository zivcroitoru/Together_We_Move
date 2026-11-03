using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace SharedUI
{
    /// <summary>Explicit theme application avoids overwriting per-instance art and colors on enable.</summary>
    public sealed class UIStyle : MonoBehaviour
    {
        public enum Role { Primary, Secondary, Surface, Ink, Progress, Gold }
        public UITheme theme;
        public Role role;
        public Graphic graphic;
        public TMP_Text label;
        public Selectable selectable;
        [ContextMenu("Apply Theme")]
        public void Apply()
        {
            if(!theme)return;
            if(graphic)graphic.color=role switch {Role.Primary=>theme.primary,Role.Secondary=>theme.secondary,Role.Surface=>theme.surface,Role.Progress=>theme.progress,Role.Gold=>theme.gold,_=>theme.ink};
            if(label){label.font=theme.font;label.fontSize=theme.bodySize;label.fontSizeMin=theme.minimumSize;label.fontSizeMax=theme.bodySize;}
            if(selectable)selectable.colors=theme.buttonColors;
        }
    }
}
