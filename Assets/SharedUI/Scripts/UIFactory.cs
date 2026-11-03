using UnityEngine;
using UnityEngine.UI;
namespace SharedUI
{
    /// <summary>Loads prefab templates; callers own content, layout, and event bindings.</summary>
    public static class UIFactory
    {
        public static RectTransform Create(string template,string name,Transform parent,Vector2 min,Vector2 max)
        {
            var prefab=Resources.Load<GameObject>("SharedUI/"+template);
            if(!prefab)throw new System.InvalidOperationException("Missing SharedUI template: "+template);
            var rect=Object.Instantiate(prefab,parent,false).GetComponent<RectTransform>();
            rect.name=name;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            return rect;
        }
        public static RectTransform Box(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var rect=Create("Panel",name,parent,min,max);rect.GetComponent<Image>().color=color;return rect;
        }
    }
}
