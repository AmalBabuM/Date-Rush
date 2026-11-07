using System.Collections.Generic;
using UnityEngine;
namespace DateRush
{
    public sealed class DateRushConveyors : MonoBehaviour
    {
        const int DateCount = 18;
        const float LoopSeconds = 20f;
        static readonly Vector2 ConveyorLift = new Vector2(0,64);
        static readonly Vector2 DateSize = new Vector2(60,42);
        sealed class BeltDate
        {
            public Transform root;
            public SpriteRenderer fruit;
            public TextMesh label;
            public float progress;
            public bool released;
            public int variety;
        }
        readonly List<Transform> gears=new List<Transform>();
        readonly List<BeltDate> dates=new List<BeltDate>();
        readonly List<BeltDate> candidates=new List<BeltDate>();
        DateRushGame game;
        public void Initialise(DateRushGame owner, Sprite wheel = null)
        {
            game=owner;
            Belt(new Vector2(425,235),new Vector2(1640,235),"Upper conveyor - left",wheel);
            Belt(new Vector2(26,337),new Vector2(350,361),"Short left transfer",wheel);
            Belt(new Vector2(435,493),new Vector2(1190,391),"Rising conveyor - right",wheel);
            Belt(new Vector2(1255,391),new Vector2(1660,391),"Right exit conveyor",wheel);
            for(int i=0;i<DateCount;i++)
            {
                var root=new GameObject("Conveyor date "+(i+1)).transform;root.SetParent(transform);
                int variety=i%DateCatalog.All.Length;
                float progress=i/(float)DateCount;
                Vector2 position=Path(progress);
                root.position=DateRushArt.World(position);
                var fruit=game.CreateDateSprite("Date fruit",variety,position,DateSize,8,root);
                var tag=new GameObject("Target date tag",typeof(TextMesh));tag.transform.SetParent(root,false);tag.transform.localPosition=new Vector3(0,.38f,0);
                DateRushArt.Sprite("Tag background",DateRushArt.Shape("rounded"),position+new Vector2(0,-38),new Vector2(86,25),11,tag.transform,DateRushArt.Cream);
                var text=tag.GetComponent<TextMesh>();text.text=DateCatalog.All[variety].name;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=48;text.characterSize=.04f;text.anchor=TextAnchor.MiddleCenter;text.color=DateRushArt.Ink;
                text.GetComponent<MeshRenderer>().sortingOrder=12;text.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
                tag.SetActive(false);
                dates.Add(new BeltDate{root=root,fruit=fruit,label=text,progress=progress,variety=variety});
            }
        }
        void Belt(Vector2 start,Vector2 end,string name,Sprite wheel)
        {
            start-=ConveyorLift;
            end-=ConveyorLift;
            Vector2 delta=end-start,center=(start+end)/2;float length=delta.magnitude;
            var root=new GameObject(name).transform;root.SetParent(transform);
            float angle=-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
            BeltSurface("Carved wood rim","belt rim",center,length,angle,1,root,new Color(.65f,.37f,.17f));
            BeltSurface("Moving wood belt","wood",center,length,angle,2,root,new Color(.34f,.17f,.08f));
            int count=Mathf.CeilToInt(length/400);
            for(int i=0;i<=count;i++)
            {
                Vector2 p=Vector2.Lerp(start,end,i/(float)count);
                var gearRoot=new GameObject("Rotating brass gear").transform;gearRoot.SetParent(root);gearRoot.position=DateRushArt.World(p);
                gears.Add(gearRoot);
                if(wheel)
                {
                    var renderer = DateRushArt.Sprite("Conveyor wheel",wheel,p,new Vector2(58,58),6,gearRoot);
                    Vector2 pivotOffset = (wheel.pivot - wheel.rect.size * .5f) / wheel.pixelsPerUnit;
                    renderer.transform.localPosition = Vector3.Scale(pivotOffset, renderer.transform.localScale);
                    continue;
                }
                for(int j=0;j<8;j++)
                {
                    var tooth=DateRushArt.Sprite("Gear tooth",DateRushArt.Shape("rounded"),p,new Vector2(10,58),4,gearRoot,new Color(.72f,.63f,.48f));
                    tooth.transform.rotation=Quaternion.Euler(0,0,j*45);
                }
                DateRushArt.Sprite("Gear ring",DateRushArt.Shape("oval"),p,new Vector2(45,45),5,gearRoot,new Color(.39f,.31f,.22f));
                DateRushArt.Sprite("Gear hub",DateRushArt.Shape("ring"),p,new Vector2(32,32),6,gearRoot,new Color(.8f,.69f,.51f));
            }
        }
        static void BeltSurface(string name,string shape,Vector2 center,float length,float angle,int order,Transform parent,Color tint)
        {
            var sprite=DateRushArt.Shape(shape);
            float diameter=sprite.rect.height;
            var size=new Vector2(length+diameter,diameter);
            var renderer=DateRushArt.Sprite(name,sprite,center,size,order,parent,tint);
            renderer.transform.localScale=Vector3.one;
            renderer.transform.rotation=Quaternion.Euler(0,0,angle);
            // Slice horizontally so the straight middle stretches while the end caps stay round.
            renderer.drawMode=SpriteDrawMode.Sliced;
            renderer.size=size/100f;
        }
        public void ResetDates()
        {
            for(int i=0;i<dates.Count;i++)
            {
                var date=dates[i];
                date.progress=i/(float)dates.Count;
                date.released=false;
                date.root.position=DateRushArt.World(Path(date.progress));
                SetVariety(date,IncomingVariety());
                date.root.gameObject.SetActive(true);
            }
        }
        void Update()
        {
            if(game.State!=RoundState.Playing && game.State!=RoundState.Menu)return;
            float dt=Time.deltaTime;foreach(var gear in gears)gear.Rotate(0,0,-70*dt);UpdateDates(dt);
        }
        void LateUpdate()
        {
            foreach(var date in dates)
            {
                var tag=date.label.gameObject;
                bool visible=game.IsTargetDate(date.variety);
                if(tag.activeSelf!=visible)tag.SetActive(visible);
            }
        }
        void UpdateDates(float dt)
        {
            foreach(var date in dates)
            {
                float nextProgress=date.progress+dt/LoopSeconds;
                date.progress=Mathf.Repeat(nextProgress,1);
                bool enteredBelt=nextProgress>=1;
                if(date.released && !enteredBelt)continue;
                date.root.position=DateRushArt.World(Path(date.progress));
                if(!enteredBelt)continue;

                SetVariety(date,IncomingVariety());
                date.released=false;
                date.root.gameObject.SetActive(true);
            }
        }
        int IncomingVariety()
        {
            int varietyCount=DateCatalog.All.Length;
            if(game.State!=RoundState.Playing || Random.value>=game.targetChance)
                return Random.Range(0,varietyCount);

            // Stock the next target in advance so some can reach the drop zone before it changes.
            return Random.value<2f/3f?game.Target:(game.Target+1)%varietyCount;
        }
        void SetVariety(BeltDate date,int variety)
        {
            if(date.variety==variety)return;
            date.variety=variety;
            DateRushArt.SetSprite(date.fruit,game.GetDateSprite(variety),DateSize);
            date.fruit.color=Color.white;
            date.label.text=DateCatalog.All[variety].name;
        }
        public static Vector2 Path(float t)=>BeltPath(t)-ConveyorLift;
        static Vector2 BeltPath(float t)
        {
            t=Mathf.Repeat(t,1);
            if(t<.44f)return Vector2.Lerp(new Vector2(1690,183),new Vector2(430,183),t/.44f);
            if(t<.50f){float u=(t-.44f)/.06f;return Vector2.Lerp(new Vector2(430,183),new Vector2(230,299),u)+new Vector2(0,-Mathf.Sin(u*Mathf.PI)*35);}
            if(t<.58f)return Vector2.Lerp(new Vector2(230,299),new Vector2(352,308),(t-.50f)/.08f);
            if(t<.64f){float u=(t-.58f)/.06f;return Vector2.Lerp(new Vector2(352,308),new Vector2(435,440),u)+new Vector2(0,-Mathf.Sin(u*Mathf.PI)*22);}
            if(t<.9f)return Vector2.Lerp(new Vector2(435,440),new Vector2(1190,339),(t-.64f)/.26f);
            return Vector2.Lerp(new Vector2(1190,339),new Vector2(1690,339),(t-.9f)/.1f);
        }
        public bool TryReleaseDate(int target,float chance,out int variety,out Vector2 position)
        {
            candidates.Clear();bool preferTarget=Random.value<chance;
            foreach(var date in dates)
                if(CanRelease(date) && (!preferTarget || date.variety==target))candidates.Add(date);
            if(candidates.Count==0)
                foreach(var date in dates)if(CanRelease(date))candidates.Add(date);
            if(candidates.Count==0)
            {
                variety=default;
                position=default;
                return false;
            }

            var source=candidates[Random.Range(0,candidates.Count)];
            variety=source.variety;
            position=Path(source.progress);
            source.released=true;
            source.root.gameObject.SetActive(false);
            return true;
        }
        static bool CanRelease(BeltDate date)=>!date.released && date.progress>.65f && date.progress<.95f;
    }
}

