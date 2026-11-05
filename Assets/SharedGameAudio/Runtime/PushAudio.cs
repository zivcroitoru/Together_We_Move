using UnityEngine;
namespace SharedGameAudio
{
    /// <summary>Add to an existing movable box. Observes contacts without changing physics.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PushAudio : MonoBehaviour
    {
        Rigidbody2D body;float next;Vector2 previous;
        void Awake(){body=GetComponent<Rigidbody2D>();previous=body.position;}
        bool moved;
        void FixedUpdate(){moved=Mathf.Abs(body.position.x-previous.x)>.002f;previous=body.position;}
        void OnCollisionStay2D(Collision2D collision)
        {
            if(!moved||Time.unscaledTime<next||collision.rigidbody==null)return;
            for(int i=0;i<collision.contactCount;i++)if(Mathf.Abs(collision.GetContact(i).normal.x)>.6f)
            {AudioManager.PlaySound(SoundId.Push);next=Time.unscaledTime+.35f;break;}
        }
    }
}
