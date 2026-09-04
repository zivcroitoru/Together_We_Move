using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class BridgeTilemapJuice : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float delayBetweenTiles = 0.08f;
    [SerializeField] private float popDuration = 0.18f;

    [Header("Juice")]
    [SerializeField] private float startYOffset = -0.25f;
    [SerializeField] private float startScale = 0.55f;
    [SerializeField] private float overshootScale = 1.15f;

    private Tilemap tilemap;
    private BoxCollider2D bridgeCollider;

    private readonly List<Vector3Int> cells = new();
    private readonly List<TileBase> tiles = new();

    private bool hasBuilt;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        bridgeCollider = GetComponent<BoxCollider2D>();

        if (tilemap == null)
        {
            Debug.LogError("[BRIDGE] No Tilemap found!");
            return;
        }

        // Bridge should not be walkable before it appears.
        if (bridgeCollider != null)
        {
            bridgeCollider.enabled = false;
        }
        else
        {
            Debug.LogWarning("[BRIDGE] No BoxCollider2D found!");
        }

        // Remember every painted bridge tile.
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cell);

            if (tile == null)
                continue;

            cells.Add(cell);
            tiles.Add(tile);
        }

        // Build bridge from left to right.
        SortTilesLeftToRight();

        Debug.Log($"[BRIDGE] Stored {cells.Count} bridge tiles.");

        // Hide all bridge tiles.
        foreach (Vector3Int cell in cells)
        {
            tilemap.SetTile(cell, null);
        }
    }

    public void Play()
    {
        if (hasBuilt)
            return;

        hasBuilt = true;

        Debug.Log("[BRIDGE] Building bridge!");

        StartCoroutine(BuildBridge());
    }

    private IEnumerator BuildBridge()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            StartCoroutine(
                PopTile(cells[i], tiles[i])
            );

            // Delay before spawning the next piece.
            if (i < cells.Count - 1)
            {
                yield return new WaitForSeconds(
                    delayBetweenTiles
                );
            }
        }

        // Wait until the final piece finishes popping.
        yield return new WaitForSeconds(popDuration);

        // Entire bridge is now safe to walk on.
        if (bridgeCollider != null)
        {
            bridgeCollider.enabled = true;

            Debug.Log("[BRIDGE] Bridge complete - collider enabled!");
        }
    }

    private IEnumerator PopTile(
        Vector3Int cell,
        TileBase tile)
    {
        // Restore the tile.
        tilemap.SetTile(cell, tile);

        // Allow the tile's transform to be animated.
        tilemap.SetTileFlags(
            cell,
            TileFlags.None
        );

        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / popDuration
            );

            float scale;
            float yOffset;

            // -----------------------
            // POP UP
            // -----------------------
            if (t < 0.7f)
            {
                float part = t / 0.7f;

                float eased =
                    1f - Mathf.Pow(1f - part, 3f);

                scale = Mathf.Lerp(
                    startScale,
                    overshootScale,
                    eased
                );

                yOffset = Mathf.Lerp(
                    startYOffset,
                    0.05f,
                    eased
                );
            }

            // -----------------------
            // SETTLE
            // -----------------------
            else
            {
                float part =
                    (t - 0.7f) / 0.3f;

                scale = Mathf.Lerp(
                    overshootScale,
                    1f,
                    part
                );

                yOffset = Mathf.Lerp(
                    0.05f,
                    0f,
                    part
                );
            }

            Matrix4x4 matrix =
                Matrix4x4.TRS(
                    new Vector3(
                        0f,
                        yOffset,
                        0f
                    ),
                    Quaternion.identity,
                    new Vector3(
                        scale,
                        scale,
                        1f
                    )
                );

            tilemap.SetTransformMatrix(
                cell,
                matrix
            );

            yield return null;
        }

        // Finish perfectly aligned.
        tilemap.SetTransformMatrix(
            cell,
            Matrix4x4.identity
        );

        tilemap.RefreshTile(cell);
    }

    private void SortTilesLeftToRight()
    {
        List<(Vector3Int cell, TileBase tile)> combined = new();

        for (int i = 0; i < cells.Count; i++)
        {
            combined.Add(
                (cells[i], tiles[i])
            );
        }

        combined.Sort(
            (a, b) =>
                a.cell.x.CompareTo(b.cell.x)
        );

        cells.Clear();
        tiles.Clear();

        foreach (var item in combined)
        {
            cells.Add(item.cell);
            tiles.Add(item.tile);
        }
    }
}