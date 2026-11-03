using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace SharedUI
{
    [CreateAssetMenu(menuName="Shared UI/Theme")]
    public sealed class UITheme : ScriptableObject
    {
        public Color primary=new Color(.16f,.55f,.86f), secondary=new Color(.86f,.23f,.28f), ink=new Color(.08f,.19f,.25f);
        public Color surface=new Color(1,.985f,.93f), progress=new Color(.20f,.72f,.44f), gold=new Color(1,.72f,.12f);
        public TMP_FontAsset font;
        public Sprite panel;
        public Vector2 referenceResolution=new Vector2(2048,1536);
        public float headingSize=32, bodySize=28, minimumSize=16, spacing=24, controlSize=160;
        public float fadeSeconds=.35f, scoreSeconds=1.25f, pulseAmplitude=.12f;
        public ColorBlock buttonColors=ColorBlock.defaultColorBlock;
    }
}
