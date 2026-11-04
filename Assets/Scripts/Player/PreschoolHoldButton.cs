using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Reuses existing buttons; one child's release cannot cancel another held finger.</summary>
public sealed class PreschoolHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private PreschoolTouchControls controls;
    private PlayerController player;
    private float direction;
    private readonly System.Collections.Generic.HashSet<int> fingers = new();
    public void Initialize(PreschoolTouchControls owner,PlayerController actor,float value) { controls=owner;player=actor;direction=value; }
    public void OnPointerDown(PointerEventData data) { fingers.Add(data.pointerId); controls?.HoldButton(player,data.pointerId,direction,true); }
    public void OnPointerUp(PointerEventData data) => Release(data.pointerId);
    public void OnPointerExit(PointerEventData data) => Release(data.pointerId);
    private void Release(int id) { if(fingers.Remove(id)) controls?.HoldButton(player,id,direction,false); }
    private void OnDisable() { foreach(int id in fingers) controls?.HoldButton(player,id,direction,false); fingers.Clear(); }
}
