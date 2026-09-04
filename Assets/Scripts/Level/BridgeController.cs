using UnityEngine;

public sealed class BridgeController : MonoBehaviour
{
    [SerializeField] private BridgeJuice bridgeJuice;

    private bool isLeverPulled;
    private int playersOnButton;

    public void OnButtonEnter()
    {
        playersOnButton++;
        EvaluateState();
    }

    public void OnButtonExit()
    {
        playersOnButton = Mathf.Max(0, playersOnButton - 1);
        EvaluateState();
    }

    public void PullLever()
    {
        isLeverPulled = true;
        EvaluateState();
    }

    private void EvaluateState()
    {
        if (isLeverPulled || playersOnButton > 0)
        {
            bridgeJuice?.ShowBridge();
        }
        else
        {
            bridgeJuice?.HideBridge();
        }
    }
}