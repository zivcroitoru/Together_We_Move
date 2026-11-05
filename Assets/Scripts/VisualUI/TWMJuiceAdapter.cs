using System.Collections;
using SharedGameJuice;
using SharedUI;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Level-local, read-only gameplay adapter. The exported SharedGameJuice assembly has no TWM references.</summary>
public sealed class TWMJuiceAdapter : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        foreach(var root in scene.GetRootGameObjects())
            foreach(var manager in root.GetComponentsInChildren<GameManager>(true))
                if(!manager.GetComponent<TWMJuiceAdapter>())manager.gameObject.AddComponent<TWMJuiceAdapter>();
    }
    PressurePlate[] plates;
    Lever[] levers;
    SpriteJuice[] plateEffects, leverEffects;
    bool[] occupied, flipped;
    CooperationTracker tracker;
    UIProgress progress;
    ScoreJuice score;
    public bool IsBound { get; private set; }
    IEnumerator Start()
    {
        yield return null; // GameManager creates UI and tracker in Start.
        plates=FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);levers=FindObjectsByType<Lever>(FindObjectsSortMode.None);
        occupied=new bool[plates.Length];flipped=new bool[levers.Length];plateEffects=new SpriteJuice[plates.Length];leverEffects=new SpriteJuice[levers.Length];
        for(int i=0;i<plates.Length;i++)
        {
            occupied[i]=plates[i].IsOccupied;
            var color=plates[i].Allowed==PressurePlate.AllowedPlayer.Player1?new Color(.22f,.64f,1):plates[i].Allowed==PressurePlate.AllowedPlayer.Player2?new Color(1,.32f,.40f):new Color(1,.78f,.20f);
            plateEffects[i]=Attach(plates[i],color);
        }
        for(int i=0;i<levers.Length;i++){flipped[i]=levers[i].IsFlipped;leverEffects[i]=Attach(levers[i],new Color(1,.76f,.22f));}
        foreach(var bridge in FindObjectsByType<BridgeJuice>(FindObjectsSortMode.None))
        {var fx=bridge.GetComponent<BridgePresentation>();if(!fx)fx=bridge.gameObject.AddComponent<BridgePresentation>();fx.surface=bridge.Surface;}
        foreach(var goal in FindObjectsByType<SplitStarGoal>(FindObjectsSortMode.None))
        {
            // Keep the existing half-star and win pops, pulse only a separate halo.
            var renderers=goal.GetComponentsInChildren<SpriteRenderer>(true);
            foreach(var renderer in renderers) if(renderer.gameObject.activeInHierarchy){var fx=renderer.gameObject.AddComponent<SpriteJuice>();fx.Initialize(renderer,new Color(1,.79f,.22f),true,false);}
        }
        foreach(var goal in FindObjectsByType<LevelGoal>(FindObjectsSortMode.None))
        {var renderer=goal.GetComponentInChildren<SpriteRenderer>();if(renderer){var fx=goal.gameObject.AddComponent<SpriteJuice>();fx.Initialize(renderer,new Color(1,.79f,.22f),true,false);}}
        tracker=GetComponent<CooperationTracker>();if(tracker)tracker.Milestone+=Milestone;
        foreach(var bar in FindObjectsByType<UIProgress>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(bar.fill&&bar.indicators.Length>0){progress=bar;score=bar.gameObject.AddComponent<ScoreJuice>();score.fill=bar.fill;score.stars=bar.indicators;bar.duration=.5f;break;}
        IsBound=true;
    }
    SpriteJuice Attach(Component owner,Color color)
    {
        var renderer=owner.GetComponentInChildren<SpriteRenderer>();if(!renderer)return null;
        // Use the actual switch palette (scene sprites are usually white-tinted).
        string name=(renderer.sprite?renderer.sprite.name:owner.name).ToLowerInvariant();
        if(name.Contains("blue"))color=new Color(.22f,.64f,1);else if(name.Contains("red"))color=new Color(1,.32f,.40f);else if(name.Contains("green"))color=new Color(.35f,.85f,.45f);else if(name.Contains("yellow"))color=new Color(1,.78f,.2f);
        var fx=owner.gameObject.AddComponent<SpriteJuice>();fx.Initialize(renderer,color);return fx;
    }
    void LateUpdate()
    {
        if(!IsBound)return;
        for(int i=0;i<plates.Length;i++)if(plates[i])
        {bool pressed=plates[i].IsOccupied;if(pressed&&!occupied[i])plateEffects[i]?.Press();occupied[i]=pressed;}
        for(int i=0;i<levers.Length;i++)if(levers[i])
        {bool pressed=levers[i].IsFlipped;if(pressed&&!flipped[i])leverEffects[i]?.Press();flipped[i]=pressed;}
        if(progress&&score&&progress.gameObject.activeInHierarchy)score.SetProgress(progress.value);
    }
    void Milestone(string type,int player,string target)
    {
        if(type!="goal_reached"&&type!="assisted_crossing")return;
        foreach(var actor in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            if(actor.PlayerId==player)JuiceBurst.At(actor.transform.position+Vector3.up*.6f,type=="goal_reached"?new Color(1,.8f,.22f):new Color(.5f,.86f,1),JuiceBurst.Style.Sparkle,12);
    }
    void OnDestroy(){if(tracker)tracker.Milestone-=Milestone;}
}
