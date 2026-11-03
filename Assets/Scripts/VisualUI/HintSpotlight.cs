using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws dimming only outside the real targets. It never intercepts touches.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HintSpotlight : MaskableGraphic
{
    public readonly List<Rect> Holes = new();
    public void Refresh() => SetVerticesDirty();
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var bounds=rectTransform.rect;
        var xs=new List<float>{bounds.xMin,bounds.xMax};var ys=new List<float>{bounds.yMin,bounds.yMax};
        foreach(var h in Holes){xs.Add(Mathf.Clamp(h.xMin,bounds.xMin,bounds.xMax));xs.Add(Mathf.Clamp(h.xMax,bounds.xMin,bounds.xMax));ys.Add(Mathf.Clamp(h.yMin,bounds.yMin,bounds.yMax));ys.Add(Mathf.Clamp(h.yMax,bounds.yMin,bounds.yMax));}
        xs.Sort();ys.Sort();
        for(int x=0;x<xs.Count-1;x++)for(int y=0;y<ys.Count-1;y++)
        {
            var cell=Rect.MinMaxRect(xs[x],ys[y],xs[x+1],ys[y+1]);bool clear=false;foreach(var h in Holes)if(h.Contains(cell.center)){clear=true;break;}if(clear)continue;
            int n=mesh.currentVertCount;mesh.AddVert(new Vector2(cell.xMin,cell.yMin),color,Vector2.zero);mesh.AddVert(new Vector2(cell.xMin,cell.yMax),color,Vector2.zero);mesh.AddVert(new Vector2(cell.xMax,cell.yMax),color,Vector2.zero);mesh.AddVert(new Vector2(cell.xMax,cell.yMin),color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
    }
}
