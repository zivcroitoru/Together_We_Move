using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public sealed class BridgeController : NetworkBehaviour
{
    [SerializeField] private BridgeJuice bridgeJuice;

    private NetworkVariable<int> playersOnButton = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isLeverPulled = new NetworkVariable<bool>(false);
    private readonly Dictionary<int, int> localHelpers = new();
    private int localButtonCount;
    private bool localLeverPulled;
    private int leverPlayerId;

    public BoxCollider2D Surface => bridgeJuice != null ? bridgeJuice.Surface : null;
    public bool CanObserveLocally => !IsSpawned || IsServer;
    public int HelperMask
    {
        get
        {
            int mask = leverPlayerId == 0 ? 0 : 1 << (leverPlayerId - 1);
            foreach (var helper in localHelpers)
                if (helper.Value > 0 && helper.Key >= 1 && helper.Key <= 2)
                    mask |= 1 << (helper.Key - 1);
            return mask;
        }
    }

    public override void OnNetworkSpawn()
    {
        playersOnButton.OnValueChanged += OnButtonsChanged;
        isLeverPulled.OnValueChanged += OnLeverChanged;
        EvaluateStateClient();
    }

    public override void OnNetworkDespawn()
    {
        playersOnButton.OnValueChanged -= OnButtonsChanged;
        isLeverPulled.OnValueChanged -= OnLeverChanged;
    }

    private void OnButtonsChanged(int previous, int current) => EvaluateStateClient();
    private void OnLeverChanged(bool previous, bool current) => EvaluateStateClient();

    // Unspawned scenes use local state; spawned games retain server authority.
    // A legacy/anonymous call can open a bridge but cannot earn cooperation credit.
    public void OnButtonEnter(int playerId = 0)
    {
        if (!CanObserveLocally) return;
        localHelpers.TryGetValue(playerId, out int count);
        localHelpers[playerId] = count + 1;
        if (IsSpawned) playersOnButton.Value++;
        else localButtonCount++;
        EvaluateStateClient();
    }

    public void OnButtonExit(int playerId = 0)
    {
        if (!CanObserveLocally) return;
        localHelpers.TryGetValue(playerId, out int count);
        if (count == 0) return;
        localHelpers[playerId] = count - 1;
        if (IsSpawned) playersOnButton.Value = Mathf.Max(0, playersOnButton.Value - 1);
        else localButtonCount = Mathf.Max(0, localButtonCount - 1);
        EvaluateStateClient();
    }

    public void PullLever(int playerId = 0)
    {
        if (!CanObserveLocally) return;
        if (!localLeverPulled) leverPlayerId = playerId;
        localLeverPulled = true;
        if (IsSpawned) isLeverPulled.Value = true;
        EvaluateStateClient();
    }

    private void EvaluateStateClient()
    {
        bool open = IsSpawned ? isLeverPulled.Value || playersOnButton.Value > 0
            : localLeverPulled || localButtonCount > 0;
        if (open)
            bridgeJuice?.ShowBridge();
        else
            bridgeJuice?.HideBridge();
    }
}
