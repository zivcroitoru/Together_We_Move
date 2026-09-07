using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;

public static class SetupOnScreenControls
{
    [MenuItem("Tools/Generate On-Screen Controls", false, 10)]
    public static void GenerateControls()
    {
        // 1. Locate Players
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController p1 = null, p2 = null;

        foreach (var p in players)
        {
            var field = typeof(PlayerController).GetField("isPlayerTwo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isP2 = field != null && (bool)field.GetValue(p);
            if (isP2) p2 = p; else p1 = p;
        }

        if (!p1 && players.Length > 0) p1 = players[0];
        if (!p2 && players.Length > 1) p2 = players[1];

        // 2. Setup Canvas & Scaler for iPad 4:3
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (!canvas)
        {
            GameObject cObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(cObj, "Create Canvas");
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2048, 1536);
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        // 3. Ensure EventSystem exists
        if (!Object.FindFirstObjectByType<EventSystem>())
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // 4. Generate Procedural Bubble & Arrow Sprites
        Sprite bubbleSprite = GetOrCreateCircleSprite();
        Sprite leftArrowSprite = CreateProceduralArrowSprite(ArrowDirection.Left);
        Sprite rightArrowSprite = CreateProceduralArrowSprite(ArrowDirection.Right);
        Sprite upArrowSprite = CreateProceduralArrowSprite(ArrowDirection.Up);

        Color bubblyBlue = new Color(0.25f, 0.72f, 1f, 0.95f);
        Color bubblyRed  = new Color(1f, 0.35f, 0.35f, 0.95f);

        // 5. Build Pads
        CreatePlayerPad("P1_Controls", canvas.transform, new Vector2(0f, 0f), 1f, bubblyBlue, bubbleSprite, leftArrowSprite, rightArrowSprite, upArrowSprite, p1);
        CreatePlayerPad("P2_Controls", canvas.transform, new Vector2(1f, 0f), -1f, bubblyRed, bubbleSprite, leftArrowSprite, rightArrowSprite, upArrowSprite, p2 ?? p1);

        Debug.Log("Image-based on-screen controls generated successfully!");
    }

    private enum ArrowDirection { Left, Right, Up }

    private static void CreatePlayerPad(string groupName, Transform canvas, Vector2 anchor, float xOffsetSign, Color color, 
        Sprite bgSprite, Sprite leftSpr, Sprite rightSpr, Sprite upSpr, PlayerController target)
    {
        Transform old = canvas.Find(groupName);
        if (old) Undo.DestroyObjectImmediate(old.gameObject);

        GameObject root = new GameObject(groupName, typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        Undo.RegisterCreatedObjectUndo(root, "Create " + groupName);

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = anchor;
        rootRt.anchorMax = anchor;
        rootRt.pivot = anchor;
        rootRt.anchoredPosition = Vector2.zero;

        float buttonSize = 160f;
        float spacing = 185f;
        float btnY = 130f;
        float edgeMargin = 120f;

        float leftX  = xOffsetSign > 0 ? edgeMargin                 : -edgeMargin - (spacing * 2);
        float rightX = xOffsetSign > 0 ? edgeMargin + spacing       : -edgeMargin - spacing;
        float jumpX  = xOffsetSign > 0 ? edgeMargin + (spacing * 2) : -edgeMargin;

        GameObject leftBtn  = MakeImageButton("Btn_Left",  leftSpr,  root.transform, new Vector2(leftX, btnY), buttonSize, color, bgSprite);
        GameObject rightBtn = MakeImageButton("Btn_Right", rightSpr, root.transform, new Vector2(rightX, btnY), buttonSize, color, bgSprite);
        GameObject upBtn    = MakeImageButton("Btn_Jump",  upSpr,    root.transform, new Vector2(jumpX, btnY), buttonSize, color, bgSprite);

        if (target)
        {
            WireHold(leftBtn.GetComponent<Button>(), target, -1f);
            WireHold(rightBtn.GetComponent<Button>(), target, 1f);

            Button jBtn = upBtn.GetComponent<Button>();
            UnityEventTools.RemovePersistentListener(jBtn.onClick, target.Jump);
            UnityEventTools.AddPersistentListener(jBtn.onClick, target.Jump);
            EditorUtility.SetDirty(jBtn);
        }
    }

    private static GameObject MakeImageButton(string name, Sprite iconSprite, Transform parent, Vector2 pos, float size, Color col, Sprite bgSprite)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(parent.GetComponent<RectTransform>().anchorMin.x, 0f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);

        Image img = obj.GetComponent<Image>();
        img.sprite = bgSprite;
        img.color = col;

        // Child Arrow Image
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(obj.transform, false);

        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(size * 0.45f, size * 0.45f); // Scaled inside bubble
        iconRt.anchoredPosition = Vector2.zero;

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;

        return obj;
    }

    private static void WireHold(Button btn, PlayerController player, float dir)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        UnityEventTools.AddFloatPersistentListener(down.callback, player.SetTouchMove, dir);
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        UnityEventTools.AddFloatPersistentListener(up.callback, player.SetTouchMove, 0f);
        trigger.triggers.Add(up);

        EditorUtility.SetDirty(trigger);
    }

    private static Sprite CreateProceduralArrowSprite(ArrowDirection dir)
    {
        int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Color[] fill = new Color[res * res];
        for (int i = 0; i < fill.Length; i++) fill[i] = Color.clear;
        tex.SetPixels(fill);

        float pad = 24f;
        Vector2 a, b, c;

        switch (dir)
        {
            case ArrowDirection.Left:
                a = new Vector2(pad, res * 0.5f);
                b = new Vector2(res - pad, res - pad);
                c = new Vector2(res - pad, pad);
                break;
            case ArrowDirection.Right:
                a = new Vector2(res - pad, res * 0.5f);
                b = new Vector2(pad, pad);
                c = new Vector2(pad, res - pad);
                break;
            default: // Up
                a = new Vector2(res * 0.5f, res - pad);
                b = new Vector2(pad, pad);
                c = new Vector2(res - pad, pad);
                break;
        }

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                if (IsPointInTriangle(new Vector2(x, y), a, b, c))
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    private static bool IsPointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
        float t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;
        if ((s < 0) != (t < 0) && s != 0 && t != 0) return false;
        float d = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
        return d < 0 ? (s <= 0 && s + t >= d) : (s >= 0 && s + t <= d);
    }

    private static Sprite GetOrCreateCircleSprite()
    {
        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (knob) return knob;

        int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float r = res * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, d <= r ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }
}