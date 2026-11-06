using UnityEngine;
using UnityEngine.InputSystem;
namespace DateRush
{
    public sealed class DateRushPlayer : MonoBehaviour
    {
        [Header("Movement in design pixels per second")]
        public float runSpeed=730,jumpSpeed=720,gravity=2000,dashSpeed=1550;
        public Vector2 BasketPosition {get;private set;}
        public Vector2 PreviousBasketPosition {get;private set;}
        public float Height {get;private set;}
        public float DashCooldown {get;private set;}
        public bool Grounded=>Height<=.01f;
        public bool Invulnerable=>hurtTime>0;
        public int TouchAxis {get;set;}
        DateRushGame game;
        Transform figure,basket,shadow;
        SpriteRenderer body,basketRenderer;
        Sprite idle;readonly Sprite[] run=new Sprite[4];bool moving;
        float x=800,desiredX=800,velocityY,walk,feedback,hurtTime,dashTime;
        int facing=1;
        public void Initialise(DateRushGame owner)
        {
            game=owner;
            shadow=DateRushArt.Sprite("Ground shadow",DateRushArt.Shape("oval"),new Vector2(800,801),new Vector2(150,24),19,transform,new Color(.15f,.07f,.02f,.28f)).transform;
            body=DateRushArt.Sprite("Emirati boy - kandura and ghutra",DateRushArt.Load("Catcher"),new Vector2(800,698),new Vector2(190,216),21,transform);figure=body.transform;
            basketRenderer=DateRushArt.Sprite("Woven catching basket",DateRushArt.Shape("basket"),new Vector2(865,735),new Vector2(140,81),24,transform);basket=basketRenderer.transform;
            var collider=gameObject.AddComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(.7f,1.9f);collider.offset=new Vector2(0,0);
            idle=DateRushArt.Load("Catcher");for(int i=0;i<4;i++)run[i]=DateRushArt.Load("Run"+i);
            ResetPlayer();
        }
        public void ResetPlayer()
        {x=desiredX=800;Height=velocityY=dashTime=DashCooldown=hurtTime=walk=feedback=0;facing=1;TouchAxis=0;moving=false;RefreshPose(0);PreviousBasketPosition=BasketPosition;}
        void Update()
        {
            if(game.State!=RoundState.Playing)return;
            PreviousBasketPosition=BasketPosition;
            float dt=Mathf.Min(Time.deltaTime,.05f);var k=Keyboard.current;float axis=TouchAxis;
            if(k!=null)
            {
                if(k.aKey.isPressed || k.leftArrowKey.isPressed)axis--;
                if(k.dKey.isPressed || k.rightArrowKey.isPressed)axis++;
                if(k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)Jump();
                if(k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame)Dash();
            }
            if(axis!=0){facing=axis>0?1:-1;desiredX=x+Mathf.Sign(axis)*runSpeed*dt;}
            else
            {
                if(Mouse.current!=null && Mouse.current.leftButton.isPressed)Pointer(Mouse.current.position.ReadValue());
                if(Touchscreen.current!=null && Touchscreen.current.primaryTouch.press.isPressed)Pointer(Touchscreen.current.primaryTouch.position.ReadValue());
            }
            if(dashTime>0){desiredX=x+facing*dashSpeed*dt;dashTime-=dt;}
            desiredX=Mathf.Clamp(desiredX,110,1490);
            float previous=x;x=Mathf.MoveTowards(x,desiredX,(dashTime>0?dashSpeed:runSpeed)*dt);
            moving=Mathf.Abs(x-previous)>.1f;
            if(moving){facing=x>previous?1:-1;walk+=dt*15;}
            else walk=Mathf.MoveTowards(walk,Mathf.Round(walk/Mathf.PI)*Mathf.PI,dt*5);
            velocityY-=gravity*dt;Height=Mathf.Max(0,Height+velocityY*dt);if(Grounded)velocityY=0;
            DashCooldown=Mathf.Max(0,DashCooldown-dt);hurtTime=Mathf.Max(0,hurtTime-dt);feedback=Mathf.Max(0,feedback-dt);
            RefreshPose(dt);
        }
        void Pointer(Vector2 screen)
        {
            if(!game.GameCamera.pixelRect.Contains(screen))return;
            Vector2 point=DateRushArt.Logical(game.GameCamera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,10)));
            if(point.y>525 && point.y<810)desiredX=point.x-65*facing;
        }
        void RefreshPose(float dt)
        {
            transform.position=DateRushArt.World(new Vector2(x,705-Height));
            float bob=Grounded?Mathf.Abs(Mathf.Sin(walk))*4:0;
            Sprite pose=!Grounded?run[1]:moving?run[(int)(walk*.65f)%4]:idle;
            if(pose!=null && body.sprite!=pose)
            {
                body.sprite=pose;body.sharedMaterial=DateRushArt.MaterialFor(pose);
                figure.localScale=new Vector3(190/pose.rect.width,216/pose.rect.height,1);
            }
            figure.position=DateRushArt.World(new Vector2(x,698-Height-bob));body.flipX=facing<0;
            figure.rotation=Quaternion.Euler(0,0,Grounded?Mathf.Sin(walk)*2:-facing*7);
            BasketPosition=new Vector2(x+65*facing,708-Height-bob);
            basket.position=DateRushArt.World(BasketPosition+new Vector2(0,28));
            basket.rotation=Quaternion.Euler(0,0,feedback>0?Mathf.Sin(feedback*32)*5:0);
            basketRenderer.color=feedback>0?new Color(1,1,.7f):Color.white;
            shadow.position=DateRushArt.World(new Vector2(x,801));shadow.localScale=new Vector3((150-Height*.22f)/128,24f/128,1);
            body.color=new Color(1,1,1,hurtTime>0 && Mathf.Sin(hurtTime*35)>0?.45f:1);
            // Collider follows the character; catches use a swept basket boundary for fast fruit.
        }
        public void Jump(){if(game.State==RoundState.Playing && Grounded)velocityY=jumpSpeed;}
        public void Dash(){if(game.State==RoundState.Playing && DashCooldown<=0){dashTime=.2f;DashCooldown=1.7f;}}
        public void CatchFeedback(bool correct){feedback=.3f;}
        public void Hurt(){hurtTime=1.5f;}
    }
}

