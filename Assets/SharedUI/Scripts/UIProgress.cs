using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace SharedUI
{
    public sealed class UIProgress : MonoBehaviour
    {
        public RectTransform fill;
        public TMP_Text label;
        public RectTransform[] indicators=System.Array.Empty<RectTransform>();
        public float pulseAmplitude=.12f;
        [Min(0)] public float duration=1.25f;
        [Range(0,1)] public float value;
        Coroutine runningAnimation;
        public void SetValue(float normalized)
        {
            if(runningAnimation!=null)StopCoroutine(runningAnimation);runningAnimation=null;ResetIndicators();Apply(normalized);
        }
        public void AnimateTo(float normalized)
        {
            if(runningAnimation!=null)StopCoroutine(runningAnimation);
            if(!isActiveAndEnabled){Apply(normalized);return;}
            runningAnimation=StartCoroutine(Animate(Mathf.Clamp01(normalized)));
        }
        void Apply(float normalized)
        {
            value=Mathf.Clamp01(normalized);if(fill)fill.anchorMax=new Vector2(value,1);
            if(label)label.text=Mathf.RoundToInt(value*100)+"%";
        }
        IEnumerator Animate(float target)
        {
            float start=value,elapsed=0;
            while(elapsed<duration){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);Apply(Mathf.Lerp(start,target,1-Mathf.Pow(1-t,3)));foreach(var indicator in indicators)if(indicator)indicator.localScale=Vector3.one*(1+pulseAmplitude*Mathf.Sin(t*Mathf.PI));yield return null;}
            Apply(target);ResetIndicators();runningAnimation=null;
        }
        void ResetIndicators(){foreach(var indicator in indicators)if(indicator)indicator.localScale=Vector3.one;}
        void OnDisable(){if(runningAnimation!=null)StopCoroutine(runningAnimation);runningAnimation=null;ResetIndicators();}
        void OnValidate(){if(fill)Apply(value);}
    }
}

