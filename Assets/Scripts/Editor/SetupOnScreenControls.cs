using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.InputSystem.UI;

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
            if (p.PlayerId == 2) p2 = p; else p1 = p;
        }

        if (!p1 || !p2 || players.Length != 2)
        {
            Debug.LogError("Controls need exactly one Blue and one Red player. Check Is Player Two; identity cannot be guessed.");
            return;
        }

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
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // Use the same licensed artwork as the serialized level controls.
        Sprite blueSprite = Artwork("Blue"), redSprite = Artwork("Red");
        Sprite leftArrowSprite = Artwork("Left"), rightArrowSprite = Artwork("Right"), upArrowSprite = Artwork("Jump");
        if(!blueSprite||!redSprite||!leftArrowSprite||!rightArrowSprite||!upArrowSprite)
            throw new System.InvalidOperationException("Import Assets/Art/KenneyControls before generating controls.");
        Color bubblyBlue = Color.white, bubblyRed = Color.white;

        // 5. Build Pads
        CreatePlayerPad("P1_Controls", canvas.transform, new Vector2(0f, 0f), 1f, bubblyBlue, blueSprite, leftArrowSprite, rightArrowSprite, upArrowSprite, p1);
        CreatePlayerPad("P2_Controls", canvas.transform, new Vector2(1f, 0f), -1f, bubblyRed, redSprite, leftArrowSprite, rightArrowSprite, upArrowSprite, p2);

        Debug.Log("Image-based on-screen controls generated successfully!");
    }


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
        img.preserveAspect = true;
        var button=obj.GetComponent<Button>();button.targetGraphic=img;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.08f,1.08f,1.08f);colors.pressedColor=new Color(.82f,.82f,.82f);colors.selectedColor=Color.white;colors.fadeDuration=.08f;button.colors=colors;

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
        iconImg.preserveAspect = true;
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

    private static Sprite Artwork(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/KenneyControls/"+name+".png");
}
