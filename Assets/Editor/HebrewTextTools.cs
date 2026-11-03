using UnityEditor;
using UnityEngine;
using TMPro;

public static class HebrewTextTools
{
    [MenuItem("Tools/Hebrew/Prepare loaded TMP text")]
    public static void Prepare()
    {
        int count = 0;
        foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (!text.gameObject.scene.IsValid() || text.GetComponent<HebrewTextSupport>()) continue;
            Undo.AddComponent<HebrewTextSupport>(text.gameObject);
            EditorUtility.SetDirty(text.gameObject);
            count++;
        }
        Debug.Log("[Hebrew] Prepared " + count + " text components. Source text and styling were retained.");
    }
}
