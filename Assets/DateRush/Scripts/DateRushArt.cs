using System.Collections.Generic;
using UnityEngine;
namespace DateRush
{
    public static class DateRushArt
    {
        public static readonly Color Gold=new Color(.96f,.69f,.31f),Cream=new Color(1,.92f,.77f),Ink=new Color(.12f,.06f,.04f),Wood=new Color(.27f,.13f,.065f),Mint=new Color(.66f,.94f,.53f),Coral=new Color(1,.38f,.24f);
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        static readonly List<Object> owned=new List<Object>();
        static readonly Dictionary<Texture,Material> materials=new Dictionary<Texture,Material>();
        public static Vector3 World(Vector2 p)=>new Vector3((p.x-800)/100,(450-p.y)/100,0);
        public static Vector2 Logical(Vector3 p)=>new Vector2(p.x*100+800,450-p.y*100);
        public static Sprite Load(string name)
        {
            if(cache.TryGetValue(name,out var result))return result;
            var texture=Resources.Load<Texture2D>(name);if(texture==null)return Shape(name=="Basket"?"basket":"rounded");
            result=UnityEngine.Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
            cache[name]=result;owned.Add(result);return result;
        }
        public static Sprite Shape(string name)
        {
            if(cache.TryGetValue(name,out var result))return result;
            int w=name=="wood"?256:128,h=name=="wood"?64:128;
            var t=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Date Rush - "+name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float nx=(x+.5f)/w*2-1,ny=(y+.5f)/h*2-1,alpha=1,shade=1;
                if(name=="oval" || name=="ring")
                {float d=Mathf.Sqrt(nx*nx+ny*ny);alpha=Mathf.Clamp01((1-d)*45);if(name=="ring")alpha*=Mathf.Clamp01((d-.65f)*45);}
                else if(name=="rounded")
                {float dx=Mathf.Max(Mathf.Abs(nx)-.82f,0),dy=Mathf.Max(Mathf.Abs(ny)-.82f,0);alpha=Mathf.Clamp01((.18f-Mathf.Sqrt(dx*dx+dy*dy))*60);}
                else if(name=="basket")
                {
                    float halfWidth=.72f+(ny+1)*.13f;
                    float top=Mathf.Sqrt(Mathf.Max(0,1-nx*nx))*.13f;
                    alpha=Mathf.Abs(nx)<halfWidth && ny<.65f+top && ny>-.82f ? 1:0;
                    bool rim=ny>.43f && ny<.67f;
                    float weave=((x/7+y/6)%2==0)?.15f:0;
                    pixels[y*w+x]=new Color(.48f+weave+(rim?.18f:0),.28f+weave,.1f+weave*.5f,alpha);
                    continue;
                }
                else if(name=="wood")shade=.8f+.12f*Mathf.Sin(y*.7f+Mathf.Sin(x*.04f)*1.3f)+.04f*Mathf.Sin(x*.19f+y*1.8f);
                pixels[y*w+x]=new Color(shade,shade,shade,alpha);
            }
            t.SetPixels(pixels);t.Apply();owned.Add(t);
            result=UnityEngine.Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));
            cache[name]=result;owned.Add(result);return result;
        }
        public static SpriteRenderer Sprite(string name,Sprite sprite,Vector2 position,Vector2 size,int order,Transform parent,Color? tint=null)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.position=World(position);
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=order;renderer.color=tint??Color.white;
            renderer.sharedMaterial=MaterialFor(sprite);
            obj.transform.localScale=new Vector3(size.x/sprite.rect.width,size.y/sprite.rect.height,1);return renderer;
        }
        public static Material MaterialFor(Sprite sprite)
        {
            if(materials.TryGetValue(sprite.texture,out var existing))return existing;
            var result=new Material(Shader.Find("Sprites/Default")){mainTexture=sprite.texture};
            materials[sprite.texture]=result;owned.Add(result);return result;
        }
        public static Sprite Date(int variety)=>Load("Date"+variety);
        public static SpriteRenderer DateSprite(string name,int variety,Vector2 pos,Vector2 size,int order,Transform parent)
        {
            bool hasArt=Resources.Load<Texture2D>("Date"+variety)!=null;
            return Sprite(name,hasArt?Date(variety):Shape("oval"),pos,size,order,parent,hasArt?Color.white:DateCatalog.All[variety].color);
        }
        public static void Release(){foreach(var obj in owned)if(obj!=null)Object.Destroy(obj);owned.Clear();cache.Clear();materials.Clear();}
    }
}

