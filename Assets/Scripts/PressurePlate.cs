using UnityEngine;
using System.Collections.Generic;

public sealed class PressurePlate : MonoBehaviour
{
    public enum AllowedPlayer
    {
        Any,
        Player1,
        Player2
    }

    [SerializeField] private BridgeController bridgeController;

    [Header("Player allowed")]
    [SerializeField] private AllowedPlayer allowedPlayer = AllowedPlayer.Any;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite unpressedSprite;
    [SerializeField] private Sprite pressedSprite;

    private readonly Dictionary<PlayerController, HashSet<Collider2D>> occupants = new();
    public BridgeController Bridge => bridgeController;
    public AllowedPlayer Allowed => allowedPlayer;
    public bool IsOccupied => occupants.Count > 0;
    public bool ContainsPlayer(PlayerController player) => occupants.ContainsKey(player);

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer) spriteRenderer.sprite = unpressedSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null || !IsAllowed(player))
            return;

        if (!occupants.TryGetValue(player, out var colliders))
        {
            colliders = new HashSet<Collider2D>();
            occupants.Add(player, colliders);
            bridgeController?.OnButtonEnter(player.PlayerId);
        }
        colliders.Add(other);
        if (spriteRenderer) spriteRenderer.sprite = pressedSprite;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null || !IsAllowed(player))
            return;

        if (!occupants.TryGetValue(player, out var colliders)) return;
        colliders.Remove(other);
        if (colliders.Count == 0)
        {
            occupants.Remove(player);
            bridgeController?.OnButtonExit(player.PlayerId);
        }
        if (occupants.Count == 0 && spriteRenderer) spriteRenderer.sprite = unpressedSprite;
    }

    private void OnDisable()
    {
        foreach (var player in occupants.Keys)
            if (player) bridgeController?.OnButtonExit(player.PlayerId);
        occupants.Clear();
        if (spriteRenderer) spriteRenderer.sprite = unpressedSprite;
    }

    private bool IsAllowed(PlayerController player)
    {
        if (allowedPlayer == AllowedPlayer.Any)
            return true;

        if (allowedPlayer == AllowedPlayer.Player1)
            return player.PlayerId == 1;

        if (allowedPlayer == AllowedPlayer.Player2)
            return player.PlayerId == 2;

        return false;
    }
}
