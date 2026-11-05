using UnityEngine;
namespace SharedGameAudio
{
    /// <summary>Optional reusable scene cue, including menu scenes in a second game.</summary>
    public sealed class SceneMusic : MonoBehaviour
    {
        public AudioClip clip;
        void Start(){if(AudioManager.Instance)AudioManager.Instance.PlayMusic(clip);}
    }
}
