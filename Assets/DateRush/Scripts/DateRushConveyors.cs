using System.Collections.Generic;
using UnityEngine;
namespace DateRush
{
    public sealed class DateRushConveyors : MonoBehaviour
    {
        sealed class Tray {public Transform root;public float progress,pulse;public int variety;}
        readonly List<Transform> gears=new List<Transform>();
        readonly List<Tray> trays=new List<Tray>();
        readonly List<int> candidates=new List<int>();
        DateRushGame game;
        public void Initialise(DateRushGame owner)
        {
            game=owner;
            Belt(new Vector2(425,235),new Vector2(1640,235),"Upper conveyor - left");
            Belt(new Vector2(26,337),new Vector2(350,361),"Short left transfer");
            Belt(new Vector2(435,493),new Vector2(1190,391),"Rising conveyor - right");
            Belt(new Vector2(1228,391),new Vector2(1660,391),"Right exit conveyor");
            for(int i=0;i<12;i++)
            {
                var root=new GameObject("Date tray "+(i+1)).transform;root.SetParent(transform);
                int variety=i%6;
                DateRushArt.Sprite("Woven date tray",DateRushArt.Load("Basket"),new Vector2(0,0),new Vector2(100,41),7,root);
                for(int j=0;j<3;j++)DateRushArt.DateSprite("Dates",variety,new Vector2(-21+j*23,-16-(j%2)*7),new Vector2(42,29),8,root);
                foreach(Transform child in root)child.localPosition+=new Vector3(8,-4.5f,0);
                var tag=new GameObject("Variety label",typeof(TextMesh));tag.transform.SetParent(root,false);tag.transform.localPosition=new Vector3(0,.58f,0);
                var text=tag.GetComponent<TextMesh>();text.text=DateCatalog.All[variety].name;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=48;text.characterSize=.04f;text.anchor=TextAnchor.MiddleCenter;text.color=DateRushArt.Cream;
                text.GetComponent<MeshRenderer>().sortingOrder=12;text.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
                trays.Add(new Tray{root=root,progress=i/12f,variety=variety});
            }
            UpdateTrays(0);
        }
        void Belt(Vector2 start,Vector2 end,string name)
        {
            Vector2 delta=end-start,center=(start+end)/2;float length=delta.magnitude;
            var root=new GameObject(name).transform;root.SetParent(transform);
            var outer=DateRushArt.Sprite("Carved wood rim",DateRushArt.Shape("rounded"),center,new Vector2(length+66,67),1,root,new Color(.65f,.37f,.17f));
            float angle=-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;outer.transform.rotation=Quaternion.Euler(0,0,angle);
            var inner=DateRushArt.Sprite("Moving wood belt",DateRushArt.Shape("wood"),center,new Vector2(length+49,48),2,root,new Color(.34f,.17f,.08f));inner.transform.rotation=outer.transform.rotation;
            int count=Mathf.CeilToInt(length/400);
            for(int i=0;i<=count;i++)
            {
                Vector2 p=Vector2.Lerp(start,end,i/(float)count);
                var gearRoot=new GameObject("Rotating brass gear").transform;gearRoot.SetParent(root);gearRoot.position=DateRushArt.World(p);
                for(int j=0;j<8;j++)
                {
                    var tooth=DateRushArt.Sprite("Gear tooth",DateRushArt.Shape("rounded"),p,new Vector2(10,58),4,gearRoot,new Color(.72f,.63f,.48f));
                    tooth.transform.rotation=Quaternion.Euler(0,0,j*45);
                }
                DateRushArt.Sprite("Gear ring",DateRushArt.Shape("oval"),p,new Vector2(45,45),5,gearRoot,new Color(.39f,.31f,.22f));
                DateRushArt.Sprite("Gear hub",DateRushArt.Shape("ring"),p,new Vector2(32,32),6,gearRoot,new Color(.8f,.69f,.51f));gears.Add(gearRoot);
            }
        }
        public void ResetTrays(){for(int i=0;i<trays.Count;i++)trays[i].progress=i/(float)trays.Count;UpdateTrays(0);}
        void Update()
        {
            if(game.State!=RoundState.Playing && game.State!=RoundState.Menu)return;
            float dt=Time.deltaTime;foreach(var gear in gears)gear.Rotate(0,0,-70*dt);UpdateTrays(dt);
        }
        void UpdateTrays(float dt)
        {
            foreach(var tray in trays)
            {
                tray.progress=Mathf.Repeat(tray.progress+dt/30,1);tray.pulse=Mathf.Max(0,tray.pulse-dt);
                tray.root.position=DateRushArt.World(Path(tray.progress));tray.root.localScale=Vector3.one*(1+Mathf.Sin(tray.pulse*18)*tray.pulse*.14f);
            }
        }
        public static Vector2 Path(float t)
        {
            t=Mathf.Repeat(t,1);
            if(t<.44f)return Vector2.Lerp(new Vector2(1690,183),new Vector2(430,183),t/.44f);
            if(t<.50f){float u=(t-.44f)/.06f;return Vector2.Lerp(new Vector2(430,183),new Vector2(300,302),u)+new Vector2(0,-Mathf.Sin(u*Mathf.PI)*35);}
            if(t<.58f)return Vector2.Lerp(new Vector2(300,302),new Vector2(352,308),(t-.50f)/.08f);
            if(t<.64f){float u=(t-.58f)/.06f;return Vector2.Lerp(new Vector2(352,308),new Vector2(435,440),u)+new Vector2(0,-Mathf.Sin(u*Mathf.PI)*22);}
            if(t<.9f)return Vector2.Lerp(new Vector2(435,440),new Vector2(1190,339),(t-.64f)/.26f);
            return Vector2.Lerp(new Vector2(1190,339),new Vector2(1690,339),(t-.9f)/.1f);
        }
        public Vector2 ReleasePoint(int target,float chance,out int variety,out int trayIndex)
        {
            candidates.Clear();bool preferTarget=Random.value<chance;
            for(int i=0;i<trays.Count;i++)if(trays[i].progress>.65f && trays[i].progress<.95f && (!preferTarget || trays[i].variety==target))candidates.Add(i);
            if(candidates.Count==0)
                for(int i=0;i<trays.Count;i++)if(trays[i].progress>.65f && trays[i].progress<.95f)candidates.Add(i);
            trayIndex=candidates.Count>0?candidates[Random.Range(0,candidates.Count)]:0;
            var tray=trays[trayIndex];variety=tray.variety;
            // Every variety must remain catchable even when its physical tray is on the upper belt.
            // A target date visibly comes from a target-coloured release tray in that case.
            if(preferTarget && variety!=target){variety=target;SetVariety(tray,target);}
            return Path(tray.progress)+new Vector2(0,36);
        }
        void SetVariety(Tray tray,int variety)
        {
            tray.variety=variety;
            foreach(var renderer in tray.root.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.name=="Dates"){renderer.sprite=DateRushArt.Date(variety);renderer.sharedMaterial=DateRushArt.MaterialFor(renderer.sprite);renderer.color=Color.white;}
            tray.root.GetComponentInChildren<TextMesh>().text=DateCatalog.All[variety].name;
        }
        public void Pulse(int index){if(index>=0 && index<trays.Count)trays[index].pulse=.6f;}
    }
}

