using System.Collections.Generic;
using UnityEngine;
namespace DateRush
{
    public static class DateRushArt
    {
        public static readonly Color Gold=new Color(.96f,.69f,.31f),Cream=new Color(1,.92f,.77f),Ink=new Color(.12f,.06f,.04f),Wood=new Color(.27f,.13f,.065f),Mint=new Color(.66f,.94f,.53f),Coral=new Color(1,.38f,.24f);
        internal static readonly Vector2 DateFaceSize=new Vector2(40,29);
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
        internal static Sprite[] LoadFrames(string name,int columns,int rows)
        {
            var texture=Resources.Load<Texture2D>(name);
            if(!texture)return null;

            var frames=new Sprite[columns*rows];
            for(int row=0;row<rows;row++)
            for(int col=0;col<columns;col++)
            {
                int left=col*texture.width/columns,right=(col+1)*texture.width/columns;
                int top=texture.height-row*texture.height/rows,bottom=texture.height-(row+1)*texture.height/rows;
                var frame=UnityEngine.Sprite.Create(texture,new Rect(left,bottom,right-left,top-bottom),new Vector2(.5f,.5f),100);
                frame.name=name+" "+(row*columns+col);
                frames[row*columns+col]=frame;
                owned.Add(frame);
            }
            return frames;
        }
        public static Sprite Shape(string name)
        {
            if(cache.TryGetValue(name,out var result))return result;
            bool beltShape=name=="belt";
            bool dateFace=name=="date calm" || name=="date smile" || name=="date angry";
            int w=beltShape?256:128,h=beltShape?76:128;
            float radius=h*.5f;
            Color beltOutside=new Color32(6,11,35,255),beltInside=new Color32(26,53,72,255);
            var t=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Date Rush - "+name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float nx=(x+.5f)/w*2-1,ny=(y+.5f)/h*2-1,alpha=1;
                if(beltShape)
                {
                    float dx=Mathf.Max(0,Mathf.Abs(x+.5f-w*.5f)-(w*.5f-radius));
                    float dy=y+.5f-radius;
                    float depth=radius-Mathf.Sqrt(dx*dx+dy*dy);
                    var color=Color.Lerp(beltOutside,beltInside,Mathf.InverseLerp(9,13,depth));
                    color.a=Mathf.Clamp01(depth+.5f)*Mathf.Clamp01(14.5f-depth);
                    pixels[y*w+x]=color;
                    continue;
                }
                else if(dateFace)
                {
                    pixels[y*w+x]=DateFacePixel(nx,ny,name);
                    continue;
                }
                else if(name=="snake head")
                {
                    pixels[y*w+x]=SnakeHeadPixel(nx,ny);
                    continue;
                }
                else if(name=="hay bale")
                {
                    pixels[y*w+x]=HayBalePixel(nx,ny);
                    continue;
                }
                else if(name=="dash arcs")
                {
                    float ring=Mathf.Sqrt(nx*nx+ny*ny);
                    float sweep=Mathf.Repeat(Mathf.Atan2(ny,nx)/Mathf.PI*1.5f,1);
                    alpha=Mathf.Clamp01((.1f-Mathf.Abs(ring-.79f))*19)*Mathf.SmoothStep(0,1,sweep)*Mathf.Clamp01((1-sweep)*12);
                }
                else if(name=="dust")
                {
                    float angle=Mathf.Atan2(ny,nx);
                    float edge=.83f+.07f*Mathf.Sin(angle*5)+.04f*Mathf.Cos(angle*8);
                    alpha=Mathf.SmoothStep(0,1,(edge-Mathf.Sqrt(nx*nx+ny*ny))*2.8f);
                }
                else if(name=="oval" || name=="ring")
                {float d=Mathf.Sqrt(nx*nx+ny*ny);alpha=Mathf.Clamp01((1-d)*45);if(name=="ring")alpha*=Mathf.Clamp01((d-.65f)*45);}
                else if(name=="rounded" || name=="rounded border")
                {
                    float outerX=Mathf.Max(Mathf.Abs(nx)-.72f,0),outerY=Mathf.Max(Mathf.Abs(ny)-.72f,0);
                    float cornerRadius=.28f;
                    float outer=Mathf.Clamp01((cornerRadius-Mathf.Sqrt(outerX*outerX+outerY*outerY))*60);
                    if(name=="rounded border")
                    {
                        float innerX=Mathf.Max(Mathf.Abs(nx)-.695f,0),innerY=Mathf.Max(Mathf.Abs(ny)-.695f,0);
                        float inner=Mathf.Clamp01((cornerRadius-Mathf.Sqrt(innerX*innerX+innerY*innerY))*60);
                        alpha=outer*(1-inner);
                    }
                    else alpha=outer;
                }
                else if(name=="plate")
                {
                    float side=Mathf.Abs(nx);
                    float halfWidth=Mathf.Lerp(.78f,.98f,Mathf.InverseLerp(-.82f,.6f,ny));
                    float edge=Mathf.Min(halfWidth-side,Mathf.Min(ny+.82f,.86f-ny));
                    float shine=Mathf.InverseLerp(-.82f,.65f,ny)*(.8f+.2f*side);
                    var steel=Color.Lerp(new Color32(28,45,65,255),new Color32(92,124,146,255),shine);
                    if(ny>.58f)steel=Color.Lerp(new Color32(119,149,166,255),new Color32(51,77,99,255),Mathf.InverseLerp(.58f,.86f,ny));
                    steel.a=Mathf.Clamp01(edge*40);
                    pixels[y*w+x]=steel;
                    continue;
                }
                else if(name=="fall trail")
                {
                    float along=(ny+1)*.5f;
                    float taper=Mathf.Lerp(.94f,.28f,along);
                    float feather=Mathf.Clamp01((taper-Mathf.Abs(nx))*4);
                    float wisps=.78f+.22f*Mathf.Cos(nx*20+along*4);
                    alpha=feather*wisps*Mathf.SmoothStep(0,1,along*10)*(1-along)*(1-along*.4f);
                }
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
                pixels[y*w+x]=new Color(1,1,1,alpha);
            }
            t.SetPixels(pixels);t.Apply();owned.Add(t);
            var border=beltShape?new Vector4(radius,0,radius,0):name=="rounded border"?new Vector4(20,20,20,20):new Vector4(12,12,12,12);
            result=UnityEngine.Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            cache[name]=result;owned.Add(result);return result;
        }
        static Color DateFacePixel(float x,float y,string expression)
        {
            Color ink=new Color32(46,19,17,255),cream=new Color32(255,247,218,255);
            float side=Mathf.Abs(x);
            bool angry=expression=="date angry";
            if(angry && side>.11f && side<.58f && Mathf.Abs(y-(.30f+.5f*side))<.065f)return ink;

            float eyeX=side-.32f,eyeY=y-.19f;
            float eyeHeight=angry?.19f:.27f;
            if(InOval(eyeX,eyeY,.23f,eyeHeight))
            {
                if(InOval(eyeX+.01f,eyeY-.052f,.045f,.052f))return Color.white;
                if(InOval(eyeX-.015f,eyeY,.092f,eyeHeight*.66f))return ink;
                return InOval(eyeX,eyeY,.19f,eyeHeight-.04f)?cream:ink;
            }

            if(expression=="date smile" && y<-.16f && InOval(x,y+.15f,.38f,.43f))
            {
                if(y>-.235f && side<.29f)return cream;
                if(InOval(x,y+.47f,.20f,.075f))return new Color32(235,100,101,255);
                return ink;
            }
            float mouth=angry?-.29f-x*x*1.05f:-.35f+x*x*.85f;
            return side<.29f && Mathf.Abs(y-mouth)<.057f?ink:Color.clear;
        }
        static bool InOval(float x,float y,float width,float height)
            =>x*x/(width*width)+y*y/(height*height)<1;
        static Color SnakeHeadPixel(float x,float y)
        {
            float snout=(x+.37f)*(x+.37f)/.37f+(y+.12f)*(y+.12f)/.31f;
            float cheek=(x-.13f)*(x-.13f)/.65f+y*y/.54f;
            float edge=Mathf.Min(snout,cheek);
            Color dark=new Color32(60,38,23,255);
            if(edge>=.90f)
            {
                dark.a=Mathf.Clamp01((1-edge)*32);
                return dark;
            }

            if(InOval(x+.13f,y-.22f,.30f,.31f))
            {
                if(InOval(x+.247f,y-.34f,.043f,.048f))return Color.white;
                if(InOval(x+.20f,y-.23f,.078f,.22f))return dark;
                return InOval(x+.13f,y-.22f,.25f,.26f)?new Color32(238,190,107,255):dark;
            }
            if(InOval(x+.73f,y+.075f,.065f,.048f))return dark;

            float mouth=-.27f+.13f*Mathf.Sin((x+.85f)*3f);
            if(x<.22f && Mathf.Abs(y-mouth)<.037f)return dark;
            if(y<mouth)return Color.Lerp(new Color32(120,77,47,255),new Color32(222,185,132,255),Mathf.InverseLerp(-.67f,-.24f,y));
            return Color.Lerp(new Color32(145,98,57,255),new Color32(220,180,111,255),Mathf.InverseLerp(-.22f,.71f,y));
        }
        static Color HayBalePixel(float x,float y)
        {
            float r=Mathf.Sqrt(x*x+y*y);
            float angle=Mathf.Atan2(y,x);
            if(r>.92f)return Color.clear;
            float rollLine=Mathf.Abs(Mathf.Sin((r*31f+angle*.16f)*Mathf.PI));
            float straw=Mathf.Sin(x*63f+y*17f)*.08f+Mathf.Sin(y*71f-x*11f)*.06f;
            Color hay=Color.Lerp(new Color32(166,105,38,255),new Color32(247,202,91,255),.54f+.24f*Mathf.Sin(r*25f+angle*2f)+straw);
            hay=Color.Lerp(hay,new Color32(125,77,31,255),Mathf.Clamp01(rollLine*.28f+r*.18f));
            if(r>.79f)hay=Color.Lerp(hay,new Color32(119,73,30,255),Mathf.InverseLerp(.79f,.93f,r));
            // Two dark jute ropes wrap around the bale and remain visible as it rolls.
            float rope= Mathf.Abs(Mathf.Sin(angle*2f+Mathf.Sin(r*8f)*.18f));
            if(rope<.055f && r<.84f)hay=Color.Lerp(new Color32(104,66,35,255),new Color32(222,176,93,255),rope/.055f);
            if(r>.84f && r<.91f)hay=Color.Lerp(hay,new Color32(232,180,76,255),.45f);
            return hay;
        }
        public static SpriteRenderer Sprite(string name,Sprite sprite,Vector2 position,Vector2 size,int order,Transform parent,Color? tint=null)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.position=World(position);
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sortingOrder=order;renderer.color=tint??Color.white;
            SetSprite(renderer,sprite,size);
            return renderer;
        }
        internal static void SetSprite(SpriteRenderer renderer, Sprite sprite, Vector2 size)
        {
            renderer.sprite=sprite;
            renderer.sharedMaterial=MaterialFor(sprite);
            float pixelsToWorld = sprite.pixelsPerUnit / 100f;
            renderer.transform.localScale=new Vector3(size.x/sprite.rect.width*pixelsToWorld,size.y/sprite.rect.height*pixelsToWorld,1);
        }
        public static Material MaterialFor(Sprite sprite)
        {
            if(materials.TryGetValue(sprite.texture,out var existing))return existing;
            var result=new Material(Shader.Find("Sprites/Default")){mainTexture=sprite.texture};
            materials[sprite.texture]=result;owned.Add(result);return result;
        }
        public static Sprite Date(int variety)=>Load("Date"+variety);
        public static SpriteRenderer DateSprite(string name,int variety,Vector2 pos,Vector2 size,int order,Transform parent,Sprite replacement=null)
        {
            if(replacement)return Sprite(name,replacement,pos,size,order,parent);
            bool hasArt=Resources.Load<Texture2D>("Date"+variety)!=null;
            return Sprite(name,hasArt?Date(variety):Shape("oval"),pos,size,order,parent,hasArt?Color.white:DateCatalog.All[variety].color);
        }
        public static void Release(){foreach(var obj in owned)if(obj!=null)Object.Destroy(obj);owned.Clear();cache.Clear();materials.Clear();}
    }
}

