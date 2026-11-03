using UnityEngine;
using UnityEngine.UI;

/// <summary>Compatibility adapter for existing icon callers, using only imported Kenney sprites.</summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class VisualIcon : Image
{
    public enum Shape { Arrow, Replay, Bulb, Star, Smile, Ring, Hand }
    public Shape shape;
    public float Rotation;
    static readonly Sprite[] artwork=new Sprite[7];
    Shape applied=(Shape)(-1);float angle=float.NaN;
    protected override void OnEnable(){base.OnEnable();RefreshArtwork();}
    void LateUpdate()=>RefreshArtwork();
    [ContextMenu("Refresh Artwork")]
    public void RefreshArtwork()
    {
        if(applied!=shape||!sprite)
        {
            int index=Mathf.Clamp((int)shape,0,artwork.Length-1);
            if(!artwork[index])artwork[index]=Resources.Load<Sprite>("KenneyUI/"+shape);
            sprite=artwork[index];preserveAspect=true;applied=shape;
        }
        if(angle!=Rotation){angle=Rotation;SetVerticesDirty();}
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        base.OnPopulateMesh(mesh);
        if(Rotation==0)return;
        var center=rectTransform.rect.center;var rotation=Quaternion.Euler(0,0,Rotation);
        for(int i=0;i<mesh.currentVertCount;i++)
        {var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,i);vertex.position=(Vector3)center+rotation*(vertex.position-(Vector3)center);mesh.SetUIVertex(vertex,i);}
    }
}
