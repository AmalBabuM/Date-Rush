using System.Collections.Generic;
using UnityEngine;
namespace DateRush
{
    public sealed class DateRushConveyors : MonoBehaviour
    {
        const float LoopSeconds = 20f;
        const float UpperBeltEnd = .44f;
        const float RisingBeltStart = .64f;
        const float ExitBeltStart = .9f;
        static readonly Vector2 ConveyorLift = new Vector2(0,64);
        static readonly Vector2 DateSize = new Vector2(60,42);
        static readonly Vector2 UpperLeft = new Vector2(425,235),UpperRight = new Vector2(1640,235);
        static readonly Vector2 RisingLeft = new Vector2(435,493),RisingRight = new Vector2(1190,391);
        static readonly Vector2 ExitLeft = new Vector2(1255,391),ExitRight = new Vector2(1660,391);
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
        int labelsTarget=-1;
        RoundState labelsState=(RoundState)(-1);
        int labelsVersion;
        int refreshedLabelsVersion=-1;
        public void Initialise(DateRushGame owner, Sprite wheel = null)
        {
            game=owner;
            Belt(UpperLeft,UpperRight,"Upper conveyor - left",wheel,pairedWheels:true);
            Belt(new Vector2(26,337),new Vector2(350,361),"Short left transfer",wheel);
            Belt(RisingLeft,RisingRight,"Rising conveyor - right",wheel,pairedWheels:true);
            Belt(ExitLeft,ExitRight,"Right exit conveyor",wheel);
            int dateCount=Mathf.Clamp(game.datesPerWave,1,36);
            for(int i=0;i<dateCount;i++)
            {
                var root=new GameObject("Conveyor date "+(i+1)).transform;root.SetParent(transform);
                int variety=i%DateCatalog.All.Length;
                float progress=i/(float)dateCount;
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
        void Belt(Vector2 start,Vector2 end,string name,Sprite wheel,bool pairedWheels=false)
        {
            start-=ConveyorLift;
            end-=ConveyorLift;
            Vector2 delta=end-start,center=(start+end)/2;float length=delta.magnitude;
            var root=new GameObject(name).transform;root.SetParent(transform);
            float angle=-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
            var beltSprite=DateRushArt.Shape("belt");
            float diameter=beltSprite.rect.height;
            var size=new Vector2(length+diameter,diameter);
            var belt=DateRushArt.Sprite("Conveyor belt",beltSprite,center,size,2,root);
            belt.transform.localScale=Vector3.one;
            belt.transform.rotation=Quaternion.Euler(0,0,angle);
            // Stretch the straight middle and keep both ends round.
            belt.drawMode=SpriteDrawMode.Sliced;
            belt.size=size/100f;
            AddWheel(start,root,wheel);
            if(pairedWheels)
            {
                Vector2 wheelOffset=delta.normalized*42;
                AddWheel(center-wheelOffset,root,wheel);
                AddWheel(center+wheelOffset,root,wheel);
            }
            AddWheel(end,root,wheel);
        }
        void AddWheel(Vector2 position,Transform belt,Sprite wheel)
        {
            var gearRoot=new GameObject("Rotating conveyor wheel").transform;
            gearRoot.SetParent(belt);gearRoot.position=DateRushArt.World(position);
            gears.Add(gearRoot);
            if(wheel)
            {
                var renderer=DateRushArt.Sprite("Conveyor wheel",wheel,position,new Vector2(58,58),6,gearRoot);
                Vector2 pivotOffset=(wheel.pivot-wheel.rect.size*.5f)/wheel.pixelsPerUnit;
                renderer.transform.localPosition=Vector3.Scale(pivotOffset,renderer.transform.localScale);
                return;
            }
            for(int i=0;i<8;i++)
            {
                var tooth=DateRushArt.Sprite("Gear tooth",DateRushArt.Shape("rounded"),position,new Vector2(10,58),4,gearRoot,new Color(.72f,.63f,.48f));
                tooth.transform.rotation=Quaternion.Euler(0,0,i*45);
            }
            DateRushArt.Sprite("Gear ring",DateRushArt.Shape("oval"),position,new Vector2(45,45),5,gearRoot,new Color(.39f,.31f,.22f));
            DateRushArt.Sprite("Gear hub",DateRushArt.Shape("ring"),position,new Vector2(32,32),6,gearRoot,new Color(.8f,.69f,.51f));
        }
        public void ResetDates()
        {
            int openingTargets=3;
            for(int i=0;i<dates.Count;i++)
            {
                var date=dates[i];
                date.progress=Mathf.Repeat(.22f+i/(float)dates.Count,1);
                date.released=false;
                date.root.position=DateRushArt.World(Path(date.progress));
                bool openingTarget=openingTargets>0 && (CanRelease(date,true) || CanRelease(date,false));
                if(openingTarget)openingTargets--;
                SetVariety(date,openingTarget?game.Target:IncomingVariety());
                date.root.gameObject.SetActive(true);
            }
        }
        void Update()
        {
            if(game.State!=RoundState.Playing && game.State!=RoundState.Menu)return;
            float dt=Time.deltaTime*Mathf.Clamp(game.beltSpeed,.25f,3f);
            foreach(var gear in gears)gear.Rotate(0,0,-70*dt);
            UpdateDates(dt);
        }
        void LateUpdate()
        {
            if(labelsTarget==game.Target && labelsState==game.State && refreshedLabelsVersion==labelsVersion)return;
            labelsTarget=game.Target;
            labelsState=game.State;
            refreshedLabelsVersion=labelsVersion;
            foreach(var date in dates)
            {
                var tag=date.label.gameObject;
                bool visible=game.IsTargetDate(date.variety);
                if(tag.activeSelf!=visible)tag.SetActive(visible);
            }
        }
        void UpdateDates(float dt)
        {
            BeltDate waiting=null;
            bool entryClear=true;
            float entryGap=Mathf.Min(.045f,.75f/dates.Count);
            foreach(var date in dates)
            {
                if(date.released)
                {
                    waiting??=date;
                    continue;
                }
                float nextProgress=date.progress+dt/LoopSeconds;
                date.progress=Mathf.Repeat(nextProgress,1);
                date.root.position=DateRushArt.World(Path(date.progress));
                if(nextProgress>=1)SetVariety(date,IncomingVariety());
                if(date.progress<entryGap || date.progress>1-entryGap)entryClear=false;
            }
            if(waiting==null || !entryClear)return;

            // A fresh date replaces the dropped fruit at the offscreen entrance.
            waiting.progress=0;
            waiting.root.position=DateRushArt.World(Path(0));
            SetVariety(waiting,IncomingVariety());
            waiting.released=false;
            waiting.root.gameObject.SetActive(true);
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
            labelsVersion++;
        }
        public static Vector2 Path(float t)=>BeltPath(t)-ConveyorLift;
        static Vector2 BeltPath(float t)
        {
            t=Mathf.Repeat(t,1);
            if(t<UpperBeltEnd)return Vector2.Lerp(new Vector2(1690,183),new Vector2(430,183),t/UpperBeltEnd);
            if(t<.50f){float u=(t-UpperBeltEnd)/(.50f-UpperBeltEnd);return Vector2.Lerp(new Vector2(430,183),new Vector2(230,299),u)+new Vector2(0,-Mathf.Sin(u*Mathf.PI)*35);}
            if(t<.58f)return Vector2.Lerp(new Vector2(230,299),new Vector2(352,308),(t-.50f)/.08f);
            if(t<RisingBeltStart){float u=(t-.58f)/(RisingBeltStart-.58f);return Vector2.Lerp(new Vector2(352,308),new Vector2(435,440),u)+new Vector2(0,-Mathf.Sin(u*Mathf.PI)*22);}
            if(t<ExitBeltStart)return Vector2.Lerp(new Vector2(435,440),new Vector2(1190,339),(t-RisingBeltStart)/(ExitBeltStart-RisingBeltStart));
            return Vector2.Lerp(new Vector2(1190,339),new Vector2(1690,339),(t-ExitBeltStart)/(1-ExitBeltStart));
        }
        public bool TryReleaseDate(int target,float chance,out int variety,out Vector2 position)
        {
            bool preferUpper=game.upperBeltDropChance>=1 || Random.value<game.upperBeltDropChance;
            if(!CollectReleaseDates(preferUpper) && !CollectReleaseDates(!preferUpper))
            {
                variety=default;
                position=default;
                return false;
            }

            if(Random.value<chance)
            {
                bool hasTarget=false;
                for(int i=0;i<candidates.Count;i++)
                    if(candidates[i].variety==target){hasTarget=true;break;}
                if(hasTarget)
                    for(int i=candidates.Count-1;i>=0;i--)
                        if(candidates[i].variety!=target)candidates.RemoveAt(i);
            }
            var source=candidates[Random.Range(0,candidates.Count)];
            variety=source.variety;
            position=Path(source.progress);
            source.released=true;
            source.root.gameObject.SetActive(false);
            return true;
        }
        bool CollectReleaseDates(bool upper)
        {
            candidates.Clear();
            foreach(var date in dates)
                if(CanRelease(date,upper))candidates.Add(date);
            return candidates.Count>0;
        }
        bool CanRelease(BeltDate date,bool upper)
        {
            if(date.released)return false;
            float x=Path(date.progress).x;
            if(upper)return date.progress<UpperBeltEnd && InsideReleaseZone(x,UpperLeft.x,UpperRight.x);
            if(date.progress<RisingBeltStart)return false;
            return date.progress<ExitBeltStart
                ? InsideReleaseZone(x,RisingLeft.x,RisingRight.x)
                : InsideReleaseZone(x,ExitLeft.x,ExitRight.x);
        }
        bool InsideReleaseZone(float x,float left,float right)
        {
            right=Mathf.Min(right,1600);
            float margin=(right-left)*game.releaseEdgeMargin;
            return x>left+margin && x<right-margin;
        }
    }
}

