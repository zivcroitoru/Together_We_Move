using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;

public static class TilemapPaintingFixer
{
    [MenuItem("Tools/Tilemap/Fix Ground Painting")]
    public static void FixGroundPainting()
    {
        // ---------------------------------------------------------
        // 1. Find the selected Rule Tile / Tile asset
        // ---------------------------------------------------------
        TileBase tile = Selection.activeObject as TileBase;

        // If nothing useful is selected, try finding GrassGroundRule automatically.
        if (tile == null)
        {
            string[] guids = AssetDatabase.FindAssets("GrassGroundRule");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);

                if (tile != null)
                    break;
            }
        }

        if (tile == null)
        {
            Debug.LogError(
                "Could not find a TileBase.\n" +
                "Select GrassGroundRule.asset in the Project window and run this again."
            );
            return;
        }

        // ---------------------------------------------------------
        // 2. Find or create Grid
        // ---------------------------------------------------------
        GameObject gridObject = GameObject.Find("Grid");

        if (gridObject == null)
        {
            gridObject = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridObject, "Create Grid");
        }

        Grid grid = gridObject.GetComponent<Grid>();

        if (grid == null)
            grid = Undo.AddComponent<Grid>(gridObject);

        grid.cellSize = new Vector3(1f, 1f, 0f);
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        gridObject.transform.position = Vector3.zero;
        gridObject.transform.rotation = Quaternion.identity;
        gridObject.transform.localScale = Vector3.one;

        // ---------------------------------------------------------
        // 3. Find or create GroundTileMap
        // ---------------------------------------------------------
        Transform child = gridObject.transform.Find("GroundTileMap");

        GameObject tilemapObject;

        if (child != null)
        {
            tilemapObject = child.gameObject;
        }
        else
        {
            tilemapObject = new GameObject("GroundTileMap");
            Undo.RegisterCreatedObjectUndo(tilemapObject, "Create Ground Tilemap");

            tilemapObject.transform.SetParent(gridObject.transform);
        }

        tilemapObject.transform.localPosition = Vector3.zero;
        tilemapObject.transform.localRotation = Quaternion.identity;
        tilemapObject.transform.localScale = Vector3.one;

        // ---------------------------------------------------------
        // 4. Make sure all Tilemap components exist
        // ---------------------------------------------------------
        Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();

        if (tilemap == null)
            tilemap = Undo.AddComponent<Tilemap>(tilemapObject);

        TilemapRenderer renderer =
            tilemapObject.GetComponent<TilemapRenderer>();

        if (renderer == null)
            renderer = Undo.AddComponent<TilemapRenderer>(tilemapObject);

        TilemapCollider2D collider =
            tilemapObject.GetComponent<TilemapCollider2D>();

        if (collider == null)
            collider = Undo.AddComponent<TilemapCollider2D>(tilemapObject);

        tilemap.color = Color.white;
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        tilemap.orientation = Tilemap.Orientation.XY;

        renderer.mode = TilemapRenderer.Mode.Chunk;

        // ---------------------------------------------------------
        // 5. FORCE this Tilemap to become the painting target
        // ---------------------------------------------------------
        GridPaintingState.scenePaintTarget = tilemapObject;

        // ---------------------------------------------------------
        // 6. FORCE the selected Rule Tile into Unity's brush
        // ---------------------------------------------------------
        GridBrush brush = GridPaintingState.gridBrush as GridBrush;

        if (brush == null)
        {
            brush = ScriptableObject.CreateInstance<GridBrush>();
            brush.name = "Ground Painting Brush";

            GridPaintingState.gridBrush = brush;
        }

        brush.UpdateSizeAndPivot(
            Vector3Int.one,
            Vector3Int.zero
        );

        brush.SetTile(
            Vector3Int.zero,
            tile
        );

        // Make sure the brush isn't invisible/tinted.
        if (brush.cells.Length > 0)
        {
            brush.cells[0].color = Color.white;
            brush.cells[0].matrix = Matrix4x4.identity;
        }

        // ---------------------------------------------------------
        // 7. Switch Unity directly to Tilemap Paint mode
        // ---------------------------------------------------------
        ToolManager.SetActiveTool<PaintTool>();

        Selection.activeGameObject = tilemapObject;

        tilemap.RefreshAllTiles();

        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(tilemapObject);

        SceneView.RepaintAll();

        Debug.Log(
            $"✅ TILEMAP FIXED\n" +
            $"Target: {tilemapObject.name}\n" +
            $"Brush Tile: {tile.name}\n\n" +
            $"Move your mouse into Scene view and paint."
        );
    }
    [MenuItem("Tools/Tilemap/Create Bridge Tilemap")]
public static void CreateBridgeTilemap()
{
    // Find or create Grid.
    GameObject gridObject = GameObject.Find("Grid");

    if (gridObject == null)
    {
        gridObject = new GameObject("Grid");
        Undo.RegisterCreatedObjectUndo(gridObject, "Create Grid");

        Grid grid = Undo.AddComponent<Grid>(gridObject);

        grid.cellSize = Vector3.one;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
    }

    // Find existing bridge tilemap.
    Transform existing =
        gridObject.transform.Find("BridgeTileMap");

    GameObject bridgeObject;

    if (existing != null)
    {
        bridgeObject = existing.gameObject;
    }
    else
    {
        bridgeObject = new GameObject("BridgeTileMap");

        Undo.RegisterCreatedObjectUndo(
            bridgeObject,
            "Create Bridge Tilemap"
        );

        bridgeObject.transform.SetParent(
            gridObject.transform
        );
    }

    bridgeObject.transform.localPosition = Vector3.zero;
    bridgeObject.transform.localRotation = Quaternion.identity;
    bridgeObject.transform.localScale = Vector3.one;

    // Tilemap.
    Tilemap tilemap =
        bridgeObject.GetComponent<Tilemap>();

    if (tilemap == null)
        tilemap = Undo.AddComponent<Tilemap>(bridgeObject);

    // Renderer.
    TilemapRenderer renderer =
        bridgeObject.GetComponent<TilemapRenderer>();

    if (renderer == null)
        renderer =
            Undo.AddComponent<TilemapRenderer>(bridgeObject);

    // Collider makes the completed bridge walkable.
    TilemapCollider2D collider =
        bridgeObject.GetComponent<TilemapCollider2D>();

    if (collider == null)
        collider =
            Undo.AddComponent<TilemapCollider2D>(bridgeObject);

    // Use the same layer as the ground.
    GameObject ground =
        GameObject.Find("GroundTileMap");

    if (ground != null)
        bridgeObject.layer = ground.layer;

    tilemap.color = Color.white;
    tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
    tilemap.orientation = Tilemap.Orientation.XY;

    renderer.mode = TilemapRenderer.Mode.Chunk;

    // Make BridgeTileMap the current painting target.
    GridPaintingState.scenePaintTarget = bridgeObject;

    Selection.activeGameObject = bridgeObject;

    ToolManager.SetActiveTool<PaintTool>();

    SceneView.RepaintAll();

    Debug.Log(
        "✅ BridgeTileMap created!\n" +
        "Paint your bridge tiles onto BridgeTileMap."
    );
}


    // -------------------------------------------------------------
    // DEBUG TEST:
    // Actually places one tile at (0, 0).
    // This proves whether the Tilemap + Rule Tile work.
    // -------------------------------------------------------------
    [MenuItem("Tools/Tilemap/Test Place Ground Tile")]
    public static void TestPlaceTile()
    {
        GameObject tilemapObject = GameObject.Find("GroundTileMap");

        if (tilemapObject == null)
        {
            Debug.LogError("GroundTileMap was not found.");
            return;
        }

        Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();

        if (tilemap == null)
        {
            Debug.LogError("GroundTileMap has no Tilemap component.");
            return;
        }

        TileBase tile = Selection.activeObject as TileBase;

        if (tile == null)
        {
            string[] guids = AssetDatabase.FindAssets("GrassGroundRule");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);

                if (tile != null)
                    break;
            }
        }

        if (tile == null)
        {
            Debug.LogError("Could not find GrassGroundRule.");
            return;
        }

        Undo.RecordObject(tilemap, "Place Test Ground Tile");

        tilemap.SetTile(
            Vector3Int.zero,
            tile
        );

        tilemap.RefreshAllTiles();

        EditorUtility.SetDirty(tilemap);

        Debug.Log("✅ Test tile placed at Grid cell (0, 0).");
    }
}