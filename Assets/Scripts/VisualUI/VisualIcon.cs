using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent, text-free icons; no font or RTL renderer.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class VisualIcon : MaskableGraphic
{
    public enum Shape { Arrow, Replay, Bulb, Star, Smile, Ring, Hand }
    public Shape shape;
    public float Rotation;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        switch (shape)
        {
            case Shape.Hand:
                // Pointing index, rounded palm, curled fingers and thumb.
                Line(mesh,new Vector2(-.06f,-.12f),new Vector2(-.06f,.34f),.12f);
                Disc(mesh,new Vector2(-.06f,.34f),.06f);
                Triangle(mesh,new Vector2(-.12f,-.30f),new Vector2(.24f,-.30f),new Vector2(.25f,.02f));
                Triangle(mesh,new Vector2(-.12f,-.30f),new Vector2(.25f,.02f),new Vector2(-.12f,.06f));
                for(int i=0;i<3;i++){float x=.05f+i*.07f;Line(mesh,new Vector2(x,-.13f),new Vector2(x,.06f-i*.025f),.085f);Disc(mesh,new Vector2(x,.06f-i*.025f),.043f);}
                Line(mesh,new Vector2(-.07f,-.21f),new Vector2(-.25f,-.05f),.12f);Disc(mesh,new Vector2(-.25f,-.05f),.06f);
                break;
            case Shape.Arrow:
                Line(mesh,new Vector2(-.36f,0),new Vector2(.32f,0),.13f);
                Triangle(mesh,new Vector2(.42f,0),new Vector2(.07f,.34f),new Vector2(.07f,-.34f));break;
            case Shape.Replay:
                Arc(mesh, .30f, 45, 325, .10f);
                Triangle(mesh,new Vector2(.36f,.26f),new Vector2(.02f,.34f),new Vector2(.27f,.02f));break;
            case Shape.Bulb:
                Arc(mesh,.25f,0,360,.08f,new Vector2(0,.13f));
                Line(mesh,new Vector2(-.12f,-.12f),new Vector2(-.10f,-.30f),.08f);
                Line(mesh,new Vector2(.12f,-.12f),new Vector2(.10f,-.30f),.08f);
                Line(mesh,new Vector2(-.10f,-.30f),new Vector2(.10f,-.30f),.08f);
                for(int i=0;i<5;i++){float a=(i*45)*Mathf.Deg2Rad;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(mesh,d*.34f+Vector2.up*.13f,d*.44f+Vector2.up*.13f,.035f);}break;
            case Shape.Star:
                for(int i=0;i<10;i++)Triangle(mesh,Vector2.zero,StarPoint(i),StarPoint(i+1));break;
            case Shape.Smile:
                Arc(mesh,.39f,0,360,.045f);
                Disc(mesh,new Vector2(-.14f,.12f),.045f);Disc(mesh,new Vector2(.14f,.12f),.045f);
                Arc(mesh,.22f,205,335,.045f,new Vector2(0,.03f));break;
            case Shape.Ring:Arc(mesh,.44f,0,360,.035f);break;
        }
    }
    Vector2 StarPoint(int i){float a=(90+i*36)*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?.45f:.20f);}
    Vector2 Position(Vector2 p){float a=Rotation*Mathf.Deg2Rad;p=new Vector2(p.x*Mathf.Cos(a)-p.y*Mathf.Sin(a),p.x*Mathf.Sin(a)+p.y*Mathf.Cos(a));var r=rectTransform.rect;return r.center+p*Mathf.Min(r.width,r.height);}
    void Triangle(VertexHelper m,Vector2 a,Vector2 b,Vector2 c){int n=m.currentVertCount;m.AddVert(Position(a),color,Vector2.zero);m.AddVert(Position(b),color,Vector2.zero);m.AddVert(Position(c),color,Vector2.zero);m.AddTriangle(n,n+1,n+2);}
    void Line(VertexHelper m,Vector2 a,Vector2 b,float w){var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*w*.5f;Triangle(m,a+p,b+p,b-p);Triangle(m,a+p,b-p,a-p);}
    void Arc(VertexHelper m,float radius,float from,float to,float width,Vector2 center=default){for(int i=0;i<48;i++){float a=Mathf.Lerp(from,to,i/48f)*Mathf.Deg2Rad,b=Mathf.Lerp(from,to,(i+1)/48f)*Mathf.Deg2Rad;Line(m,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width);}}
    void Disc(VertexHelper m,Vector2 center,float radius){for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Triangle(m,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);}}
}
