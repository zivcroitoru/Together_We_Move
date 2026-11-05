using UnityEngine;
using UnityEngine.UI;

namespace SharedGameJuice
{
    /// <summary>Explicit 0..1 progress input; works with any game's score UI.</summary>
    public sealed class ScoreJuice : MonoBehaviour
    {
        public RectTransform fill;
        public RectTransform[] stars = System.Array.Empty<RectTransform>();
        float previous, glowUntil;
        int earned;
        Image glow;
        Image[] sparks;
        Vector2[] velocity;
        float burstAt = -10;
        public int EarnedPops { get; private set; }
        void Prepare()
        {
            if (glow || !fill) return;
            var go = new GameObject("Score sheen",typeof(RectTransform),typeof(Image)); go.transform.SetParent(fill,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(-6,-6);rect.offsetMax=new Vector2(6,6);
            glow=go.GetComponent<Image>();glow.sprite=Resources.Load<Sprite>("SharedGameJuice/Glow");glow.raycastTarget=false;glow.color=Color.clear;
            sparks=new Image[8];velocity=new Vector2[8];
            for(int i=0;i<sparks.Length;i++)
            {
                var item=new GameObject("Score sparkle",typeof(RectTransform),typeof(Image));item.transform.SetParent(fill.parent,false);
                sparks[i]=item.GetComponent<Image>();sparks[i].sprite=Resources.Load<Sprite>("SharedGameJuice/Sparkle");sparks[i].raycastTarget=false;
                sparks[i].rectTransform.sizeDelta=new Vector2(13,13);sparks[i].color=Color.clear;
            }
        }
        public void SetProgress(float value)
        {
            Prepare();value=Mathf.Clamp01(value);
            if (!glow || sparks == null) return; // Unassigned prefab instances are safe to customize.
            if(value>previous+.0001f)glowUntil=Time.unscaledTime+.2f;
            int target=Mathf.CeilToInt(value*3-.001f);
            if(target>earned)
            {
                earned=target;EarnedPops++;burstAt=Time.unscaledTime;
                for(int i=0;i<sparks.Length;i++)
                {
                    var rect=sparks[i].rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(value,.5f);rect.anchoredPosition=Vector2.zero;
                    velocity[i]=new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4))*Random.Range(35,65);
                }
            }
            if(value<previous)earned=target;
            previous=value;
        }
        void LateUpdate()
        {
            if(!glow)return;
            glow.color=new Color(1,.88f,.28f,Mathf.Clamp01((glowUntil-Time.unscaledTime)/.2f)*.16f);
            float age=Time.unscaledTime-burstAt;
            for(int i=0;i<sparks.Length;i++)
            {sparks[i].color=new Color(1,.78f,.24f,1-Mathf.Clamp01(age/.4f));sparks[i].rectTransform.anchoredPosition=velocity[i]*age;}
            for(int i=0;i<stars.Length;i++)
            {
                if(!stars[i]||i>=earned)continue;
                // Existing score UI owns the root star pulse. Punch a visual graphic child only.
                var graphic=stars[i].GetComponent<Graphic>();
                if(graphic&&age<.3f)graphic.color=Color.Lerp(new Color(1,.72f,.12f),new Color(1,.9f,.42f),Mathf.Sin(age/.3f*Mathf.PI)*.35f);
            }
        }
    }
}
