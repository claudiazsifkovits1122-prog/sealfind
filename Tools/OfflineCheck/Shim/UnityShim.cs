// Managed Mini-Ersatz für die von Daten/Generator/Art genutzten UnityEngine-Typen.
// Damit laufen Tests und Szenen-Vorschau ohne Unity-Installation (z.B. in CI).
// NICHT in Unity importieren – liegt deshalb außerhalb von Assets/.
using System;
namespace UnityEngine {
  public struct Vector2 {
    public float x, y;
    public Vector2(float x, float y) { this.x = x; this.y = y; }
    public static Vector2 zero => new Vector2(0,0); public static Vector2 one => new Vector2(1,1); public static Vector2 up => new Vector2(0,1);
    public float sqrMagnitude => x*x+y*y; public float magnitude => (float)Math.Sqrt(sqrMagnitude);
    public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x+b.x, a.y+b.y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x-b.x, a.y-b.y);
    public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
    public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x*d, a.y*d);
    public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x*d, a.y*d);
    public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x/d, a.y/d);
    public static bool operator ==(Vector2 a, Vector2 b) => (a-b).sqrMagnitude < 1e-10f;
    public static bool operator !=(Vector2 a, Vector2 b) => !(a==b);
    public override bool Equals(object o) => o is Vector2 v && v == this; public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
    public static float Dot(Vector2 a, Vector2 b) => a.x*b.x + a.y*b.y;
    public static float Distance(Vector2 a, Vector2 b) => (a-b).magnitude;
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return a + (b-a)*t; }
    public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
    public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
    public override string ToString() => $"({x:0.00}, {y:0.00})";
  }
  public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
  public struct Vector4 { public float x,y,z,w; public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;} public float sqrMagnitude => x*x+y*y+z*z+w*w; }
  public struct Color {
    public float r,g,b,a;
    public Color(float r,float g,float b,float a=1f){this.r=r;this.g=g;this.b=b;this.a=a;}
    public static Color white => new Color(1,1,1,1); public static Color clear => new Color(0,0,0,0); public static Color black => new Color(0,0,0,1);
    public static Color Lerp(Color x, Color y, float t){ t=Mathf.Clamp01(t); return new Color(x.r+(y.r-x.r)*t, x.g+(y.g-x.g)*t, x.b+(y.b-x.b)*t, x.a+(y.a-x.a)*t); }
    public static Color operator *(Color x, Color y) => new Color(x.r*y.r, x.g*y.g, x.b*y.b, x.a*y.a);
    public static Color operator *(Color x, float k) => new Color(x.r*k, x.g*k, x.b*k, x.a*k);
    public static implicit operator Color(Color32 c) => new Color(c.r/255f, c.g/255f, c.b/255f, c.a/255f);
    public static implicit operator Color32(Color c) => new Color32((byte)(Mathf.Clamp01(c.r)*255+0.5f),(byte)(Mathf.Clamp01(c.g)*255+0.5f),(byte)(Mathf.Clamp01(c.b)*255+0.5f),(byte)(Mathf.Clamp01(c.a)*255+0.5f));
  }
  public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
  public struct Rect {
    float _x,_y,_w,_h;
    public Rect(float x,float y,float w,float h){_x=x;_y=y;_w=w;_h=h;}
    public static Rect MinMaxRect(float a,float b,float c,float d) => new Rect(a,b,c-a,d-b);
    public float x => _x; public float y => _y; public float width => _w; public float height => _h;
    public float xMin { get => _x; set { float xm = xMax; _x = value; _w = xm - value; } }
    public float yMin { get => _y; set { float ym = yMax; _y = value; _h = ym - value; } }
    public float xMax { get => _x+_w; set => _w = value - _x; }
    public float yMax { get => _y+_h; set => _h = value - _y; }
    public Vector2 center => new Vector2(_x+_w/2, _y+_h/2);
    public bool Contains(Vector2 p) => p.x >= xMin && p.x < xMax && p.y >= yMin && p.y < yMax;
    public bool Overlaps(Rect o) => o.xMax > xMin && o.xMin < xMax && o.yMax > yMin && o.yMin < yMax;
  }
  public static class Mathf {
    public const float PI = (float)Math.PI; public const float Deg2Rad = PI/180f; public const float Rad2Deg = 180f/PI;
    public static float Sin(float f)=>(float)Math.Sin(f); public static float Cos(float f)=>(float)Math.Cos(f); public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);
    public static float Sqrt(float f)=>(float)Math.Sqrt(f); public static float Abs(float f)=>Math.Abs(f); public static int Abs(int f)=>Math.Abs(f);
    public static float Min(float a,float b)=>a<b?a:b; public static float Max(float a,float b)=>a>b?a:b; public static int Min(int a,int b)=>a<b?a:b; public static int Max(int a,int b)=>a>b?a:b;
    public static float Pow(float a,float b)=>(float)Math.Pow(a,b); public static float Exp(float a)=>(float)Math.Exp(a);
    public static float Floor(float f)=>(float)Math.Floor(f); public static float Ceil(float f)=>(float)Math.Ceiling(f); public static float Round(float f)=>(float)Math.Round(f);
    public static int FloorToInt(float f)=>(int)Math.Floor(f); public static int CeilToInt(float f)=>(int)Math.Ceiling(f); public static int RoundToInt(float f)=>(int)Math.Round(f);
    public static float Clamp(float v,float a,float b)=>v<a?a:v>b?b:v; public static int Clamp(int v,int a,int b)=>v<a?a:v>b?b:v; public static float Clamp01(float v)=>Clamp(v,0,1);
    public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t); public static float InverseLerp(float a,float b,float v)=>a!=b?Clamp01((v-a)/(b-a)):0;
    public static float PingPong(float t,float len){ t = t % (len*2); return len - Math.Abs(t-len); }
    public static float Sign(float f)=>f>=0?1:-1; public static bool Approximately(float a,float b)=>Math.Abs(a-b)<1e-6f;
  }
  public enum FilterMode { Point, Bilinear, Trilinear } public enum TextureWrapMode { Repeat, Clamp } public enum TextureFormat { RGBA32 = 4 } public enum SpriteMeshType { FullRect, Tight }
  public class Object { public string name; public static void Destroy(Object o){} }
  public class Texture2D : Object {
    public int width, height; public FilterMode filterMode; public TextureWrapMode wrapMode; public Color32[] Pixels;
    public Texture2D(int w,int h,TextureFormat f,bool mip){width=w;height=h;}
    public void SetPixels(Color[] c){ Pixels = Array.ConvertAll(c, x => (Color32)x); } public void SetPixels32(Color32[] c){ Pixels = c; } public void Apply(bool a,bool b){}
  }
  public class Sprite : Object {
    public Texture2D texture; public Vector4 border; public Vector2 pivot; public float ppu;
    public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu, uint extrude, SpriteMeshType m, Vector4 border) => new Sprite{texture=t,border=border,pivot=pivot,ppu=ppu};
    public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu, uint extrude, SpriteMeshType m) => new Sprite{texture=t,pivot=pivot,ppu=ppu};
    public static bool operator ==(Sprite a, Sprite b) => ReferenceEquals(a,b); public static bool operator !=(Sprite a, Sprite b) => !ReferenceEquals(a,b);
    public override bool Equals(object o) => ReferenceEquals(this,o); public override int GetHashCode() => 0;
  }
  public static class Debug { public static void LogWarning(object o) => Console.WriteLine(o); public static void LogException(Exception e) => Console.WriteLine(e); }
  public static class JsonUtility {
    static readonly System.Text.Json.JsonSerializerOptions O = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
    public static string ToJson(object o) => System.Text.Json.JsonSerializer.Serialize(o, o.GetType(), O);
    public static T FromJson<T>(string s) => System.Text.Json.JsonSerializer.Deserialize<T>(s, O);
  }
  public static class Application { public static string persistentDataPath => System.IO.Path.GetTempPath(); }
}
