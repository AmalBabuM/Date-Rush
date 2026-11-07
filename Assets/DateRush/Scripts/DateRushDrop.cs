using UnityEngine;
namespace DateRush
{
    public sealed class DateRushDrop : MonoBehaviour
    {
        public int Variety {get;private set;}
        public Vector2 Position {get;private set;}
        DateRushGame game;
        float velocity,rotation,groundAge;
        bool landed;
        SpriteRenderer sprite,face,glow,fallTrail;
        Vector3 fruitScale,faceScale;
        MeshRenderer nameTag;
        public void Initialise(DateRushGame owner,int variety,Vector2 position)
        {
            game=owner;Variety=variety;Position=position;transform.position=DateRushArt.World(position);
            glow=DateRushArt.Sprite("Target glow",DateRushArt.Shape("ring"),position,new Vector2(66,55),25,transform,DateRushArt.Gold);
            sprite=game.CreateDateSprite("Date fruit",variety,position,new Vector2(53,39),26,transform);
            face=DateRushArt.Sprite("Date face",DateRushArt.Shape("date smile"),position,DateRushArt.DateFaceSize,27,transform);
            fruitScale=sprite.transform.localScale;
            faceScale=face.transform.localScale;
            fallTrail=DateRushArt.Sprite("White wind trail",DateRushArt.Shape("fall trail"),position,new Vector2(46,1),23,transform,new Color(1,1,1,0));
            var tag=new GameObject("Falling date name",typeof(TextMesh));tag.transform.SetParent(transform,false);
            var label=tag.GetComponent<TextMesh>();label.text=DateCatalog.All[variety].name;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=48;label.characterSize=.034f;label.anchor=TextAnchor.UpperCenter;label.color=DateRushArt.Cream;
            nameTag=label.GetComponent<MeshRenderer>();nameTag.sharedMaterial=label.font.material;nameTag.sortingOrder=30;
            tag.transform.position=DateRushArt.World(position+new Vector2(0,26));
            RefreshTargetMarker();
            var hitbox=gameObject.AddComponent<CircleCollider2D>();hitbox.isTrigger=true;hitbox.radius=.23f;
        }
        void Update()
        {
            if(game.State!=RoundState.Playing)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);
            if(landed){AnimateGroundReaction(dt);return;}
            Vector2 previous=Position;
            velocity+=440*dt;Position+=new Vector2(0,velocity*dt);rotation+=dt*65;
            Vector2 basket=game.Player.BasketPosition,oldBasket=game.Player.PreviousBasketPosition;
            if(CatchRules.CrossedMovingBasket(previous,Position,oldBasket,basket))
            {game.CatchDate(this);Remove();return;}
            if(Position.y>=810)
            {
                Position=new Vector2(Position.x,810);
                MoveVisuals(previous);
                Land();
                return;
            }
            MoveVisuals(previous);
            RotateFruit(rotation);
            face.transform.localScale=faceScale*(1+.055f*Mathf.Sin(rotation*.16f));
            UpdateFallTrail();
            glow.color=new Color(1,.76f,.35f,.55f+Mathf.Sin(Time.time*8)*.2f);
        }
        void UpdateFallTrail()
        {
            float speedFactor=Mathf.Clamp01(velocity/260f);
            float length=Mathf.Min(132,velocity*.28f);
            float pulse=Mathf.Sin(rotation*.14f);
            var trailTransform=fallTrail.transform;
            trailTransform.localScale=new Vector3((46+3*pulse)/fallTrail.sprite.rect.width,length/fallTrail.sprite.rect.height,1);
            trailTransform.localPosition=new Vector3(0,(length*.5f-5)/100f,0);
            fallTrail.color=new Color(1,1,1,speedFactor*(.64f+.12f*pulse));
        }
        void LateUpdate()=>RefreshTargetMarker();
        void RefreshTargetMarker()
        {
            bool target=!landed && game.IsTargetDate(Variety);
            glow.enabled=target;
            if(nameTag)nameTag.enabled=target;
        }
        void Land()
        {
            landed=true;
            game.MissDate(Variety,Position);
            fallTrail.enabled=false;
            DateRushArt.SetSprite(face,DateRushArt.Shape("date angry"),DateRushArt.DateFaceSize);
            AnimateGroundReaction(0);
        }
        void AnimateGroundReaction(float dt)
        {
            groundAge+=dt;
            if(groundAge>=.56f){Remove();return;}

            float impact=Mathf.Exp(-9*groundAge);
            var squash=new Vector3(1+.18f*impact,1-.29f*impact,1);
            sprite.transform.localScale=Vector3.Scale(fruitScale,squash);
            face.transform.localScale=Vector3.Scale(faceScale,squash);
            RotateFruit(Mathf.Sin(groundAge*34)*11*impact);
        }
        void RotateFruit(float degrees)
        {
            var pose=Quaternion.Euler(0,0,degrees);
            sprite.transform.localRotation=face.transform.localRotation=pose;
        }
        void MoveVisuals(Vector2 previous)
        {
            Vector3 delta=DateRushArt.World(Position)-DateRushArt.World(previous);
            // Child art is positioned in world space at spawn; translate the root by the delta.
            transform.position+=delta;
        }
        void Remove(){game.Drops.Remove(this);Destroy(gameObject);}
    }
}

