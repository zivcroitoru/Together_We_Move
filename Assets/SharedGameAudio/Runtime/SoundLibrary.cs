using System;
using System.Collections.Generic;
using UnityEngine;
namespace SharedGameAudio
{
    public enum SoundId { Jump, Land, Move, Push, PlateOn, PlateOff, Switch, BridgeOn, BridgeOff, Cooperate, Goal, Tutorial, Hint, Retry, Button, Score, Star, Confetti, Victory }
    [Serializable] public sealed class SoundEntry
    {
        public SoundId id;
        public AudioClip[] clips;
        [Range(0,1)] public float volume=.4f;
        [Range(0,.2f)] public float pitchVariation=.035f;
        [Min(0)] public float cooldown=.1f;
        [Range(1,8)] public int maxVoices=2;
        [Range(0,256)] public int priority=128;
    }
    [CreateAssetMenu(menuName="Shared Game Audio/Sound Library")]
    public sealed class SoundLibrary : ScriptableObject
    {
        public SoundEntry[] sounds=Array.Empty<SoundEntry>();
        public AudioClip menuMusic, levelOneMusic, levelTwoMusic;
        Dictionary<SoundId,SoundEntry> lookup;
        public SoundEntry Get(SoundId id)
        {
            if(lookup==null){lookup=new Dictionary<SoundId,SoundEntry>();foreach(var entry in sounds)if(entry!=null)lookup[entry.id]=entry;}
            return lookup.TryGetValue(id,out var result)?result:null;
        }
        void OnEnable(){lookup=null;}
        void OnValidate(){lookup=null;}
    }
}
