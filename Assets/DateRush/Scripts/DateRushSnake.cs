using UnityEngine;

namespace DateRush
{
    public sealed class DateRushSnake : MonoBehaviour
    {
        const float Floor = 792f;
        const float BiteReach = 49f;
        const float BodyLength = 1.8f;
        readonly Vector3[] points=new Vector3[22];
        readonly Transform[] markings=new Transform[7];
        DateRushGame game;
        Transform head;
        LineRenderer outline,body,tongue;
        float x=1680f,phase;

        internal void Initialise(DateRushGame owner)
        {
            game=owner;
            transform.position=DateRushArt.World(new Vector2(x,Floor));
            var taper=new AnimationCurve(new Keyframe(0,.32f),new Keyframe(.3f,.30f),new Keyframe(.73f,.18f),new Keyframe(1,.027f));
            outline=Stroke("Snake outline",transform,22,points.Length,new Color32(24,46,25,255));
            body=Stroke("Snake body",transform,23,points.Length,new Color32(112,148,64,255));
            body.endColor=new Color32(67,110,49,255);
            outline.widthCurve=body.widthCurve=taper;
            body.widthMultiplier=.78f;

            var origin=new Vector2(x,Floor);
            for(int i=0;i<markings.Length;i++)
                markings[i]=DateRushArt.Sprite("Snake marking",DateRushArt.Shape("oval"),origin,new Vector2(15,13),24,transform,new Color32(48,90,40,255)).transform;

            // Keep the head at unit scale so its tongue stays attached while the head turns.
            head=new GameObject("Snake head pivot").transform;
            head.SetParent(transform,false);
            head.position=transform.position;
            DateRushArt.Sprite("Snake head",DateRushArt.Shape("snake head"),origin,new Vector2(56,41),26,head);
            tongue=Stroke("Forked tongue",head,25,5,new Color32(226,80,83,255));
            tongue.startWidth=tongue.endWidth=.025f;
            tongue.SetPositions(new[] {new Vector3(-.17f,-.035f,0),new Vector3(-.40f,-.035f,0),new Vector3(-.49f,.005f,0),new Vector3(-.40f,-.035f,0),new Vector3(-.49f,-.076f,0)});
            DrawSlither();
        }

        static LineRenderer Stroke(string name,Transform parent,int order,int count,Color color)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent,false);
            line.useWorldSpace=false;
            line.positionCount=count;
            line.numCapVertices=5;
            line.numCornerVertices=4;
            line.sharedMaterial=DateRushArt.MaterialFor(DateRushArt.Shape("opaque"));
            line.startColor=line.endColor=color;
            line.sortingOrder=order;
            return line;
        }

        void LateUpdate()
        {
            if(game.State!=RoundState.Playing)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);
            float speed=Mathf.Clamp(game.snakeSpeed,60,900);
            float previous=x;
            x-=speed*dt;
            phase+=(5.5f+speed*.012f)*dt;

            var player=game.Player;
            float before=previous-player.PreviousX,after=x-player.PositionX;
            if(player.Height<60 && !player.Invulnerable && Mathf.Min(before,after)<=BiteReach && Mathf.Max(before,after)>=-BiteReach)
            {
                player.Hurt();
                game.HitSnake();
                Destroy(gameObject);
                return;
            }
            if(x<-BodyLength*100-30){Destroy(gameObject);return;}

            transform.position=DateRushArt.World(new Vector2(x,Floor));
            DrawSlither();
        }

        void DrawSlither()
        {
            for(int i=0;i<points.Length;i++)
            {
                float progress=i/(float)(points.Length-1);
                float wave=(Mathf.Sin(progress*9.6f-phase)*.12f+Mathf.Sin(progress*18f-phase*1.4f)*.022f)*(1-.42f*progress);
                points[i]=new Vector3(progress*BodyLength,wave,0);
            }
            outline.SetPositions(points);
            body.SetPositions(points);
            head.localPosition=points[0]+new Vector3(-.08f,.026f,0);
            head.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(points[2].y-points[0].y,points[2].x)*Mathf.Rad2Deg*.55f);
            tongue.enabled=Mathf.Repeat(phase*.092f,1)<.15f;
            for(int i=0;i<markings.Length;i++)
            {
                int point=3+i*2;
                markings[i].localPosition=points[point]+new Vector3(0,.028f,0);
                float size=Mathf.Lerp(.12f,.065f,i/(float)(markings.Length-1));
                markings[i].localScale=new Vector3(size,size*.74f,1);
            }
        }
    }
}
