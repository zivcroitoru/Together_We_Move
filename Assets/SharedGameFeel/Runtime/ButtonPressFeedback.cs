using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SharedGameFeel
{
    // Tints a non-interactive overlay; does not resize a touch target or fight Button ColorTint/UIPulse.
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Image overlay;
        float releasedAt = -10;
        bool held;
        public void OnPointerDown(PointerEventData e)
        {
            if (!GetComponent<Button>().IsInteractable()) return;
            if (!overlay)
            {
                var go = new GameObject("Press sheen", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                overlay = go.GetComponent<Image>(); overlay.raycastTarget = false;
                var graphic = GetComponent<Button>().targetGraphic as Image;
                if (graphic) { overlay.sprite = graphic.sprite; overlay.type = graphic.type; }
                var rect = overlay.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            held = true; overlay.color = new Color(1, 1, 1, .10f);
        }
        public void OnPointerUp(PointerEventData e) { held = false; releasedAt = Time.unscaledTime; }
        public void OnPointerExit(PointerEventData e) => OnPointerUp(e);
        void Update()
        {
            if (overlay && !held) overlay.color = new Color(1, 1, 1, .10f * (1-Mathf.Clamp01((Time.unscaledTime-releasedAt)/.12f)));
        }
        void OnDisable() { held = false; if (overlay) overlay.color = Color.clear; }
    }
}
