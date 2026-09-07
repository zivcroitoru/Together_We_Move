using UnityEngine;

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

    private int playersOnPlate;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        spriteRenderer.sprite = unpressedSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null || !IsAllowed(player))
            return;

        playersOnPlate++;

        if (playersOnPlate == 1)
        {
            spriteRenderer.sprite = pressedSprite;
            bridgeController?.OnButtonEnter();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null || !IsAllowed(player))
            return;

        playersOnPlate = Mathf.Max(0, playersOnPlate - 1);

        if (playersOnPlate == 0)
        {
            spriteRenderer.sprite = unpressedSprite;
            bridgeController?.OnButtonExit();
        }
    }

    private bool IsAllowed(PlayerController player)
    {
        if (allowedPlayer == AllowedPlayer.Any)
            return true;

        if (allowedPlayer == AllowedPlayer.Player1)
            return player.gameObject.name == "Player1_Blue";

        if (allowedPlayer == AllowedPlayer.Player2)
            return player.gameObject.name == "Player2_Red";

        return false;
    }
}