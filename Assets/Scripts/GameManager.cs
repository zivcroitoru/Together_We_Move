using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CooperationTracker), typeof(CooperationHints), typeof(PlaytestUI))]
public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private WinSequence winSequence;
    [Tooltip("Short celebration before the result card appears. No automatic scene advance.")]
    [SerializeField, Min(0f)] private float transitionDelay = 0.5f;
    [Header("Flexible completion")]
    [Tooltip("Give the other player time to join before accepting a partial finish.")]
    [SerializeField, Min(0f)] private float partialCompletionGraceSeconds = 6f;

    private CooperationTracker tracker;
    private CooperationHints hints;
    private PlaytestUI ui;
    private PlaytestAttempt attempt;
    private PlayerController[] players;
    private float firstGoalTime = -1f;
    private bool levelCompleted, attemptFinished, transitionPending, retryReloadPending;
    private readonly bool[] retryRequested = new bool[3];
    private float? previousScore, previousBest;

    public bool LevelCompleted => levelCompleted;
    public bool IsTransitioning => transitionPending;
    public CooperationTracker Tracker => tracker;
    public int AttemptNumber => attempt?.Number ?? 0;
    public float Score => tracker?.Progress?.Score ?? 0f;

    private void Start()
    {
        tracker = GetComponent<CooperationTracker>();
        hints = GetComponent<CooperationHints>();
        ui = GetComponent<PlaytestUI>();
        players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        string level = gameObject.scene.path;
        previousScore = PlaytestData.Store.PreviousScore(level);
        previousBest = PlaytestData.Store.BestScore(level);
        tracker.Initialize();
        attempt = PlaytestData.Store.BeginAttempt(level, tracker.Progress.TotalUnits);
        tracker.Milestone += OnMilestone;
        hints.Initialize(this);
        ui.Initialize(this, winSequence);
        GetComponent<TutorialManager>()?.Initialize(this);
    }

    private void Update()
    {
        if (attempt == null) return;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (!levelCompleted && !transitionPending)
            {
                if (keyboard.qKey.wasPressedThisFrame) hints.RequestHint(1, "player_key");
                if (keyboard.slashKey.wasPressedThisFrame) hints.RequestHint(2, "player_key");
            }
            else if (levelCompleted && ui.ResultsVisible)
            {
                if (keyboard.rKey.wasPressedThisFrame) RequestRetry(1, "player_key");
                if (keyboard.enterKey.wasPressedThisFrame) RequestRetry(2, "player_key");
            }
        }

        if (levelCompleted || transitionPending) return;
        var progress = tracker.Progress;
        var onboarding=GetComponent<TutorialManager>();
        if(onboarding&&onboarding.IsActive&&!onboarding.ControlsLearned)return;
        if (progress.GoalCount < 1) return;
        if (progress.GoalCount == 2 || Time.unscaledTime - firstGoalTime >= partialCompletionGraceSeconds)
            CompleteLevel();
    }

    public void RegisterGoal(int playerId)
    {
        if (levelCompleted || transitionPending || tracker == null) return;
        if (tracker.RegisterGoal(playerId) && firstGoalTime < 0f) firstGoalTime = Time.unscaledTime;
    }

    private void OnMilestone(string type, int playerId, string target)
    {
        Log(type, playerId, "gameplay_collider", target);
        hints.NotifyProgress();
    }

    public void CompleteLevel()
    {
        if (levelCompleted || transitionPending || tracker == null) return;
        var progress = tracker.Progress;
        var onboarding=GetComponent<TutorialManager>();
        if(onboarding&&onboarding.IsActive&&!onboarding.ControlsLearned)return;
        if (progress.GoalCount < 1) return;
        if (progress.GoalCount < 2 && Time.unscaledTime - firstGoalTime < partialCompletionGraceSeconds) return;
        FinishLevel();
    }

    private void FinishLevel()
    {
        levelCompleted = true;
        tracker.StopObserving();
        FinishAttempt(tracker.Progress.IsFull ? "full" : "partial");
        foreach (var player in players) if (player) player.SetInputLocked(true);
        hints.Hide();
        StartCoroutine(RevealResults());
    }

    private IEnumerator RevealResults()
    {
        yield return new WaitForSecondsRealtime(transitionDelay);
        ui.ShowResults(tracker.Progress.IsFull, Score, previousScore, previousBest);
    }

    public void RequestHint(int playerId) => hints.RequestHint(playerId, "player_slot_button");

    public void RequestRetry(int playerId) => RequestRetry(playerId, "player_slot_button");
    public void RequestRetry(int playerId, string attribution)
    {
        if (!levelCompleted || !ui.ResultsVisible || (transitionPending && !retryReloadPending) || !CooperationProgress.ValidPlayer(playerId)
            || retryRequested[playerId]) return;
        // Accept both players' requests in this frame, but reload only once.
        retryRequested[playerId] = true;
        Log("retry_requested", playerId, attribution);
        if (!transitionPending)
        {
            transitionPending = true;
            retryReloadPending = true;
            StartCoroutine(ReloadAfterRequests());
        }
    }

    private IEnumerator ReloadAfterRequests()
    {
        yield return null;
        ReloadCurrentScene();
    }

    public void RestartAfterFall(int playerId)
    {
        if (levelCompleted || transitionPending) return;
        Log("player_fell", playerId, "gameplay_collider");
        // Preserve an earned partial finish if the other child falls during the grace period.
        if (tracker.Progress.GoalCount >= 1 && (!GetComponent<TutorialManager>() || !GetComponent<TutorialManager>().IsActive || GetComponent<TutorialManager>().ControlsLearned))
        {
            FinishLevel();
            return;
        }
        transitionPending = true;
        tracker.StopObserving();
        FinishAttempt("fall_restart");
        ReloadCurrentScene();
    }

    private void ReloadCurrentScene() => SceneManager.LoadScene(gameObject.scene.path);

    public bool HasNextLevel => gameObject.scene.buildIndex >= 0 &&
        gameObject.scene.buildIndex + 1 < SceneManager.sceneCountInBuildSettings;

    public void Continue()
    {
        if (!levelCompleted || transitionPending) return;
        transitionPending = true;
        Log("continue", 0, "shared_button");
        // The last level returns to the first enabled level without inventing a retry requester.
        SceneManager.LoadScene(HasNextLevel ? gameObject.scene.buildIndex + 1 : 0);
    }

    public void StartNewPair()
    {
        if (!levelCompleted || transitionPending) return;
        transitionPending = true;
        PlaytestData.Store.StartNewPair();
        TutorialSession.Reset();
        SceneManager.LoadScene(0);
    }

    // Receiver used by the WebGL plugin on asynchronous IndexedDB failures.
    public void OnBrowserStorageError(string message)
    {
        if (ui != null) ui.ShowStorageError(message);
        Debug.LogError("[Playtest] " + message);
    }

    public void Log(string type, int playerId, string attribution, string detail = "", string outcome = "",
        string tutorialRunId = "", string tutorialAction = "", float tutorialElapsedSeconds = -1f, float tutorialPhaseSeconds = -1f)
    {
        var progress = tracker?.Progress;
        PlaytestData.Store.Record(attempt, type, playerId, Score, progress?.EarnedUnits ?? 0,
            progress?.TotalUnits ?? 0, attribution, detail, outcome,tutorialRunId,tutorialAction,tutorialElapsedSeconds,tutorialPhaseSeconds);
    }

    private void FinishAttempt(string outcome)
    {
        if (attemptFinished || attempt == null) return;
        attemptFinished = true;
        Log("attempt_finished", 0, "system", outcome: outcome);
    }

    private void OnApplicationQuit() => FinishAttempt("quit");
    private void OnDestroy()
    {
        if (tracker != null) tracker.Milestone -= OnMilestone;
        FinishAttempt("scene_exit");
    }

    private void OnValidate()
    {
        partialCompletionGraceSeconds = Mathf.Max(0f, partialCompletionGraceSeconds);
    }
}
