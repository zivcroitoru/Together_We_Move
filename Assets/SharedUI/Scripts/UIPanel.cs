using System.Collections;
using UnityEngine;
using UnityEngine.Events;
namespace SharedUI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIPanel : MonoBehaviour
    {
        [Min(0)] public float fadeSeconds=.35f;
        public UnityEvent onShown=new UnityEvent(),onHidden=new UnityEvent();
        Coroutine routine;
        public void Show()=>SetVisible(true);
        public void Hide()=>SetVisible(false);
        public void SetVisible(bool visible)
        {
            if(routine!=null)StopCoroutine(routine);
            var group=GetComponent<CanvasGroup>();group.interactable=visible;group.blocksRaycasts=visible;
            if(!isActiveAndEnabled){group.alpha=visible?1:0;return;}
            routine=StartCoroutine(Fade(visible));
        }
        IEnumerator Fade(bool visible)
        {
            var group=GetComponent<CanvasGroup>();float start=group.alpha,elapsed=0,target=visible?1:0;
            while(elapsed<fadeSeconds){elapsed+=Time.unscaledDeltaTime;group.alpha=Mathf.Lerp(start,target,Mathf.Clamp01(elapsed/fadeSeconds));yield return null;}
            group.alpha=target;routine=null;if(visible)onShown.Invoke();else onHidden.Invoke();
        }
    }
}
