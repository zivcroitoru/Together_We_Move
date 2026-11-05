using UnityEngine;
using UnityEngine.UI;
namespace SharedGameAudio
{
    [RequireComponent(typeof(Button))]
    public sealed class AudioButton : MonoBehaviour
    {
        void OnEnable(){GetComponent<Button>().onClick.AddListener(Click);}
        void OnDisable(){GetComponent<Button>().onClick.RemoveListener(Click);}
        void Click(){AudioManager.PlaySound(SoundId.Button);}
    }
}
