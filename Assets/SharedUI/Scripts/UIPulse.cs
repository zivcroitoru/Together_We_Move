using UnityEngine;
namespace SharedUI
{
    public sealed class UIPulse : MonoBehaviour
    {
        public float amplitude=.12f, frequency=3;
        Vector3 baseline;
        void OnEnable(){baseline=transform.localScale;}
        void Update(){transform.localScale=baseline*(1+amplitude*Mathf.Sin(Time.unscaledTime*frequency));}
        void OnDisable(){transform.localScale=baseline;}
    }
}
