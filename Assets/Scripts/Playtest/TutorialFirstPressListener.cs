using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Observes a real iPad control press without changing its gameplay callback.</summary>
public sealed class TutorialFirstPressListener : MonoBehaviour, IPointerDownHandler
{
    private TutorialManager tutorial;
    private int playerId;
    private string buttonName;

    public void Configure(TutorialManager owner, int id, string button)
    {
        tutorial = owner;
        playerId = id;
        buttonName = button;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (tutorial) tutorial.OnFirstButtonPressed(playerId, buttonName);
    }
}
