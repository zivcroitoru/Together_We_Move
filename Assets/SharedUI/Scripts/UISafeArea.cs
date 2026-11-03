using UnityEngine;
namespace SharedUI
{
    /// <summary>Optional safe-area container. Attach beneath a full-screen Canvas.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UISafeArea : MonoBehaviour
    {
        Rect previous;Vector2 size;
        void OnEnable()=>Refresh();
        void Update(){if(previous!=Screen.safeArea||size!=new Vector2(Screen.width,Screen.height))Refresh();}
        void Refresh()
        {
            if(Screen.width<=0||Screen.height<=0)return;
            previous=Screen.safeArea;size=new Vector2(Screen.width,Screen.height);var rect=(RectTransform)transform;
            rect.anchorMin=previous.min/size;rect.anchorMax=previous.max/size;rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
