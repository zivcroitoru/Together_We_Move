using UnityEngine;

namespace SharedGameJuice
{
    /// <summary>Observes existing reveal and collision timing. Disappearing decks never leave solid-looking ghosts.</summary>
    public sealed class BridgePresentation : MonoBehaviour
    {
        public Collider2D surface;
        public Color accent = new Color(.45f,.85f,1);
        SpriteRenderer[] pieces;
        Color[] colors;
        bool[] visible;
        float[] appeared;
        bool wasSolid;
        public int Transitions { get; private set; }
        void Start()
        {
            pieces = GetComponentsInChildren<SpriteRenderer>(true); colors = new Color[pieces.Length];
            visible = new bool[pieces.Length]; appeared = new float[pieces.Length];
            for(int i=0;i<pieces.Length;i++){ colors[i]=pieces[i].color; visible[i]=pieces[i].gameObject.activeInHierarchy; appeared[i]=-10; }
            wasSolid = surface && surface.enabled;
        }
        void LateUpdate()
        {
            if (pieces == null) return;
            bool solid = surface && surface.enabled, changed = false;
            Vector3 origin = transform.position;
            for (int i=0;i<pieces.Length;i++)
            {
                if (!pieces[i]) continue;
                bool on = pieces[i].gameObject.activeInHierarchy && pieces[i].enabled;
                if(on != visible[i]) { visible[i]=on; appeared[i]=Time.unscaledTime; changed=true; origin=pieces[i].bounds.center; }
                if(on) { var c=colors[i]; c.a *= solid ? 1 : Mathf.Lerp(.32f,.65f,Mathf.Clamp01((Time.unscaledTime-appeared[i])/.18f)); pieces[i].color=c; }
            }
            if(solid != wasSolid || changed)
            {
                Transitions++;
                // The pool is bounded; a bridge emits once per transition frame, never per log.
                JuiceBurst.At(origin,accent,JuiceBurst.Style.Bridge,solid?9:5);
            }
            wasSolid=solid;
        }
        void OnDestroy(){if(pieces==null)return;for(int i=0;i<pieces.Length;i++)if(pieces[i])pieces[i].color=colors[i];}
    }
}
