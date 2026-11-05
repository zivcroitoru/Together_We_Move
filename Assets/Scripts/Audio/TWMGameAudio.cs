using System.Collections;
using SharedGameAudio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SharedUI;
using AudioManager=SharedGameAudio.AudioManager;
/// <summary>Additive observers only: never writes to gameplay, physics or input state.</summary>
public sealed class TWMGameAudio : MonoBehaviour
{
    static TWMGameAudio instance;
    PlayerController[] players;
    PressurePlate[] plates;Lever[] switches;BridgeController[] bridges;
    bool[] grounded,occupied,flipped,open;
    int[] jumps;Vector3[] positions;float[] nextStep;
    CooperationTracker tracker;GameManager manager;PlaytestUI ui;UIProgress score;
    bool finished,results;int scoreBucket;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){if(instance)return;var root=new GameObject("TWM audio event adapter");DontDestroyOnLoad(root);instance=root.AddComponent<TWMGameAudio>();}
    void Awake(){SceneManager.sceneLoaded+=Loaded;StartCoroutine(Bind());}
    void Loaded(Scene scene,LoadSceneMode mode){StopAllCoroutines();StartCoroutine(Bind());}
    IEnumerator Bind()
    {
        if(tracker)tracker.Milestone-=Milestone;
        // Existing GameManager builds its UI and tracker during Start.
        yield return null;
        manager=FindFirstObjectByType<GameManager>();tracker=manager?manager.GetComponent<CooperationTracker>():null;ui=manager?manager.GetComponent<PlaytestUI>():null;
        if(tracker)tracker.Milestone+=Milestone;
        players=FindObjectsByType<PlayerController>(FindObjectsSortMode.None);plates=FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);
        switches=FindObjectsByType<Lever>(FindObjectsSortMode.None);bridges=FindObjectsByType<BridgeController>(FindObjectsSortMode.None);
        grounded=new bool[players.Length];jumps=new int[players.Length];positions=new Vector3[players.Length];nextStep=new float[players.Length];
        for(int i=0;i<players.Length;i++){grounded[i]=players[i].IsGrounded;jumps[i]=players[i].SuccessfulJumpCount;positions[i]=players[i].transform.position;}
        occupied=new bool[plates.Length];flipped=new bool[switches.Length];open=new bool[bridges.Length];
        for(int i=0;i<plates.Length;i++)occupied[i]=plates[i].IsOccupied;
        for(int i=0;i<switches.Length;i++)flipped[i]=switches[i].IsFlipped;
        for(int i=0;i<bridges.Length;i++)open[i]=bridges[i].Surface&&bridges[i].Surface.enabled;
        foreach(var button in FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(!button.GetComponent<AudioButton>())button.gameObject.AddComponent<AudioButton>();
        foreach(var body in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None))
            if(!body.GetComponent<PlayerController>()&&body.bodyType==RigidbodyType2D.Dynamic&&(body.name.ToLowerInvariant().Contains("box")||body.name.ToLowerInvariant().Contains("crate"))&&!body.GetComponent<PushAudio>())body.gameObject.AddComponent<PushAudio>();
        // Instantiate after gameplay selected its own Canvas; this overlay never becomes its canvas.
        if(!FindFirstObjectByType<AudioSettingsUI>())
        {var prefab=Resources.Load<GameObject>("SharedGameAudio/AudioSettings");if(prefab){var settings=Instantiate(prefab);DontDestroyOnLoad(settings);}}
        finished=results=false;scoreBucket=0;score=null;
        var audio=AudioManager.Instance;
        if(audio&&audio.library)audio.PlayMusic(SceneManager.GetActiveScene().name.Contains("Level_02")?audio.library.levelTwoMusic:manager?audio.library.levelOneMusic:audio.library.menuMusic);
    }
    void Update()
    {
        if(players==null||grounded==null)return;
        for(int i=0;i<players.Length;i++)
        {
            var player=players[i];if(!player)continue;bool ground=player.IsGrounded;var pos=player.transform.position;
            if(!player.InputLocked)
            {
                if(player.SuccessfulJumpCount!=jumps[i])AudioManager.PlaySound(SoundId.Jump,player.PlayerId);
                if(ground&&!grounded[i])AudioManager.PlaySound(SoundId.Land,player.PlayerId);
                if(ground&&Mathf.Abs(pos.x-positions[i].x)>.002f&&Time.unscaledTime>=nextStep[i])
                {AudioManager.PlaySound(SoundId.Move,player.PlayerId);nextStep[i]=Time.unscaledTime+.42f;}
            }
            jumps[i]=player.SuccessfulJumpCount;grounded[i]=ground;positions[i]=pos;
        }
        for(int i=0;i<plates.Length;i++)if(plates[i]&&occupied[i]!=plates[i].IsOccupied){occupied[i]=plates[i].IsOccupied;AudioManager.PlaySound(occupied[i]?SoundId.PlateOn:SoundId.PlateOff);}
        for(int i=0;i<switches.Length;i++)if(switches[i]&&flipped[i]!=switches[i].IsFlipped){flipped[i]=switches[i].IsFlipped;AudioManager.PlaySound(SoundId.Switch);}
        for(int i=0;i<bridges.Length;i++)if(bridges[i]&&bridges[i].Surface&&open[i]!=bridges[i].Surface.enabled){open[i]=bridges[i].Surface.enabled;AudioManager.PlaySound(open[i]?SoundId.BridgeOn:SoundId.BridgeOff);}
        if(manager&&manager.LevelCompleted&&!finished)
        {finished=true;var audio=AudioManager.Instance;var victory=audio&&audio.library?audio.library.Get(SoundId.Victory):null;if(victory!=null&&victory.clips.Length>0)audio.PlayMusic(victory.clips[0],.25f,false);}
        if(ui&&ui.ResultsVisible&&!results)
        {results=true;foreach(var progress in FindObjectsByType<UIProgress>(FindObjectsSortMode.None))if(progress.indicators.Length>0){score=progress;break;}StartCoroutine(Stars());}
        if(results&&score){int bucket=Mathf.FloorToInt(score.value*10);if(bucket>scoreBucket){scoreBucket=bucket;AudioManager.PlaySound(SoundId.Score);}}
    }
    IEnumerator Stars(){for(int i=0;i<3;i++){yield return new WaitForSecondsRealtime(.22f);if(ui&&ui.ResultsVisible)AudioManager.PlaySound(SoundId.Star);}}
    void Milestone(string type,int player,string target)
    {if(type=="assisted_crossing")AudioManager.PlaySound(SoundId.Cooperate);else if(type=="goal_reached")AudioManager.PlaySound(SoundId.Goal);}
    public static void Notify(string type)
    {
        switch(type)
        {
            case "hint_requested":case "automatic_highlight":case "automatic_demonstration":AudioManager.PlaySound(SoundId.Hint);break;
            case "retry_requested":case "player_recovered":AudioManager.PlaySound(SoundId.Retry);break;
            case "tutorial_action_learned":case "tutorial_first_button_pressed":case "tutorial_short_completed":AudioManager.PlaySound(SoundId.Tutorial);break;
        }
    }
    void OnDestroy(){SceneManager.sceneLoaded-=Loaded;if(tracker)tracker.Milestone-=Milestone;if(instance==this)instance=null;}
}
