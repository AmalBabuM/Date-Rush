using UnityEngine;
namespace DateRush
{
    public sealed class DateRushDrop : MonoBehaviour
    {
        public int Variety {get;private set;}
        public bool Obstacle {get;private set;}
        public Vector2 Position {get;private set;}
        DateRushGame game;
        float velocity,rotation;
        SpriteRenderer sprite,glow;
        MeshRenderer nameTag;
        public void Initialise(DateRushGame owner,int variety,Vector2 position,bool obstacle)
        {
            game=owner;Variety=variety;Position=position;Obstacle=obstacle;transform.position=DateRushArt.World(position);
            glow=DateRushArt.Sprite("Target glow",DateRushArt.Shape("ring"),position,new Vector2(obstacle?94:66,obstacle?94:55),25,transform,obstacle?DateRushArt.Coral:DateRushArt.Gold);
            sprite=game.CreateDateSprite("Date fruit",variety,position,obstacle?new Vector2(81,67):new Vector2(53,39),26,transform);
            if(!obstacle)
            {
                var tag=new GameObject("Falling date name",typeof(TextMesh));tag.transform.SetParent(transform,false);
                var label=tag.GetComponent<TextMesh>();label.text=DateCatalog.All[variety].name;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=48;label.characterSize=.034f;label.anchor=TextAnchor.UpperCenter;label.color=DateRushArt.Cream;
                nameTag=label.GetComponent<MeshRenderer>();nameTag.sharedMaterial=label.font.material;nameTag.sortingOrder=30;
                tag.transform.position=DateRushArt.World(position+new Vector2(0,26));
            }
            RefreshTargetMarker();
            var hitbox=gameObject.AddComponent<CircleCollider2D>();hitbox.isTrigger=true;hitbox.radius=obstacle?.35f:.23f;
            MoveVisuals(position);
        }
        void Update()
        {
            if(game.State!=RoundState.Playing)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);Vector2 previous=Position;
            if(Obstacle)
            {
                Position+=new Vector2(-360*dt,0);rotation+=dt*280;
                if(Mathf.Abs(Position.x-DateRushArt.Logical(game.Player.transform.position).x)<59 && game.Player.Height<60 && !game.Player.Invulnerable)
                {game.Player.Hurt();game.HitObstacle();Remove();return;}
                if(Position.x < -100){Remove();return;}
            }
            else
            {
                velocity+=440*dt;Position+=new Vector2(0,velocity*dt);rotation+=dt*65;
                Vector2 basket=game.Player.BasketPosition,oldBasket=game.Player.PreviousBasketPosition;
                if(CatchRules.CrossedMovingBasket(previous,Position,oldBasket,basket))
                {game.CatchDate(this);Remove();return;}
                if(Position.y>810){game.MissDate(Variety,Position);Remove();return;}
            }
            MoveVisuals(previous);
            sprite.transform.rotation=Quaternion.Euler(0,0,rotation);
            glow.color=Obstacle?DateRushArt.Coral:new Color(1,.76f,.35f,.55f+Mathf.Sin(Time.time*8)*.2f);
        }
        void LateUpdate()=>RefreshTargetMarker();
        void RefreshTargetMarker()
        {
            bool target=game.IsTargetDate(Variety);
            glow.enabled=Obstacle || target;
            if(nameTag)nameTag.enabled=target;
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

