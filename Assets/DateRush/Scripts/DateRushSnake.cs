using UnityEngine;

namespace DateRush
{
    // Kept under the existing component name to preserve the scene and game references.
    public sealed class DateRushSnake : MonoBehaviour
    {
        const float Floor = 792f;
        const float HitReach = 47f;
        const float Radius = .46f;
        DateRushGame game;
        Transform bale;
        float x=1680f,roll;

        internal void Initialise(DateRushGame owner)
        {
            game=owner;
            transform.position=DateRushArt.World(new Vector2(x,Floor));
            bale=new GameObject("Rolling hay bale pivot").transform;
            bale.SetParent(transform,false);
            bale.localPosition=new Vector3(0,Radius,0);
            DateRushArt.Sprite("Big hay bale",DateRushArt.Shape("hay bale"),new Vector2(x,Floor-Radius*100),new Vector2(92,92),24,bale);
        }

        void LateUpdate()
        {
            if(game.State!=RoundState.Playing)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);
            float speed=Mathf.Clamp(game.snakeSpeed,60,900);
            float previous=x;
            x-=speed*dt;
            roll+=speed*dt/Radius;

            var player=game.Player;
            float before=previous-player.PreviousX,after=x-player.PositionX;
            if(player.Height<60 && !player.Invulnerable && Mathf.Min(before,after)<=HitReach && Mathf.Max(before,after)>=-HitReach)
            {
                player.Hurt();
                game.HitSnake();
                Destroy(gameObject);
                return;
            }
            if(x<-Radius*100-40){Destroy(gameObject);return;}

            float bounce=Mathf.Abs(Mathf.Sin(roll*2f))*.025f;
            transform.position=DateRushArt.World(new Vector2(x,Floor-bounce*100));
            bale.localRotation=Quaternion.Euler(0,0,-roll*Mathf.Rad2Deg);
        }
    }
}
