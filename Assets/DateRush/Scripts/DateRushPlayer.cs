using UnityEngine;
using UnityEngine.InputSystem;
namespace DateRush
{
    public sealed class DateRushPlayer : MonoBehaviour
    {
        const float OriginalGripReach = 30f;
        const float AnimatedGripReach = 64f;
        const float PlateCenterFromGrip = 60f;
        static readonly Vector2 SampleDateSize = new Vector2(28,20);
        static readonly Vector2 AnimatedBodySize = new Vector2(225,225);
        static readonly Vector2 OriginalBodySize = new Vector2(190,216);
        sealed class DustPuff
        {
            public SpriteRenderer renderer;
            public Vector2 position,velocity;
            public float age,duration;
        }
        [Header("Movement in design pixels per second")]
        public float runSpeed=730,jumpSpeed=720,gravity=2000,dashSpeed=1550;
        public Vector2 BasketPosition {get;private set;}
        public Vector2 PreviousBasketPosition {get;private set;}
        public float Height {get;private set;}
        public bool IsSprinting {get;private set;}
        internal float PositionX=>x;
        internal float PreviousX {get;private set;}
        public bool Grounded=>Height<=.01f;
        public bool Invulnerable=>hurtTime>0;
        DateRushGame game;
        Transform figure,plate,shadow;
        SpriteRenderer body,plateRenderer,dashArcs;
        readonly SpriteRenderer[] sampleDates=new SpriteRenderer[3];
        readonly DustPuff[] dust=new DustPuff[12];
        Sprite[] animatedFrames;
        Sprite idle;
        readonly Sprite[] run=new Sprite[4];
        bool moving;
        float x=800,desiredX=800,velocityY,walk,idleTime,feedback,hurtTime,dashTime,dustTimer;
        int facing=1,dustIndex;
        float GripReach=>animatedFrames!=null?AnimatedGripReach:OriginalGripReach;
        public void Initialise(DateRushGame owner)
        {
            game=owner;
            animatedFrames=DateRushArt.LoadFrames("CatcherAnimation",4,2);
            idle=animatedFrames!=null?animatedFrames[0]:DateRushArt.Load("Catcher");
            if(animatedFrames==null)
                for(int i=0;i<run.Length;i++)run[i]=DateRushArt.Load("Run"+i);
            shadow=DateRushArt.Sprite("Ground shadow",DateRushArt.Shape("oval"),new Vector2(800,801),new Vector2(150,24),19,transform,new Color(.15f,.07f,.02f,.28f)).transform;
            body=DateRushArt.Sprite("Emirati boy - kandura and ghutra",idle,new Vector2(800,698),animatedFrames!=null?AnimatedBodySize:OriginalBodySize,21,transform);figure=body.transform;
            plate=new GameObject("Catching plate grip").transform;
            plate.SetParent(transform,false);
            var plateCenter=new Vector2(800+GripReach+PlateCenterFromGrip,713);
            plate.position=DateRushArt.World(plateCenter);
            plateRenderer=DateRushArt.Sprite("Steel catching plate",DateRushArt.Shape("plate"),plateCenter,new Vector2(140,28),19,plate);
            for(int i=0;i<sampleDates.Length;i++)
            {
                var position=plateCenter+new Vector2((i-1)*25,i==1?-20:-17);
                sampleDates[i]=game.CreateDateSprite("Target date on plate",game.Target,position,SampleDateSize,20,plate);
                sampleDates[i].transform.localRotation=Quaternion.Euler(0,0,10-i*10);
            }
            var collider=gameObject.AddComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(.7f,1.9f);collider.offset=new Vector2(0,0);
            dashArcs=DateRushArt.Sprite("Fast foot motion",DateRushArt.Shape("dash arcs"),new Vector2(800,787),new Vector2(68,45),20,transform,new Color(1,.52f,.25f,.75f));
            var dustRoot=new GameObject("Running dust").transform;
            dustRoot.SetParent(transform.parent,false);
            for(int i=0;i<dust.Length;i++)
            {
                var renderer=DateRushArt.Sprite("Dust puff",DateRushArt.Shape("dust"),new Vector2(800,801),new Vector2(25,19),20,dustRoot);
                dust[i]=new DustPuff{renderer=renderer};
            }
            ResetPlayer();
        }
        public void ResetPlayer()
        {
            x=desiredX=PreviousX=800;Height=velocityY=dashTime=hurtTime=walk=idleTime=feedback=dustTimer=0;
            facing=1;moving=IsSprinting=false;
            foreach(var puff in dust){puff.duration=0;puff.renderer.enabled=false;}
            RefreshPose(false);
            PreviousBasketPosition=BasketPosition;
        }
        void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,.05f);
            if(game.State==RoundState.Menu)
            {
                idleTime+=dt;moving=IsSprinting=false;
                RefreshPose(false);
                UpdateDust(dt,false);
                return;
            }
            if(game.State!=RoundState.Playing)return;
            PreviousBasketPosition=BasketPosition;
            PreviousX=x;
            var k=Keyboard.current;float axis=0;
            bool sprintHeld=false;
            if(k!=null)
            {
                if(k.aKey.isPressed || k.leftArrowKey.isPressed)axis--;
                if(k.dKey.isPressed || k.rightArrowKey.isPressed)axis++;
                if(k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)Jump();
                bool shiftHeld=k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
                bool shiftPressed=k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame;
                sprintHeld=axis!=0 && shiftHeld;
                if(axis==0 && shiftPressed)Dash();
            }
            if(axis!=0)dashTime=0;
            IsSprinting=sprintHeld || dashTime>0;
            float speed=IsSprinting?dashSpeed:runSpeed;
            if(axis!=0)
            {
                facing=axis>0?1:-1;
                desiredX=x+facing*speed*dt;
            }
            else if(IsSprinting)desiredX=x+facing*speed*dt;
            else
            {
                if(Mouse.current!=null && Mouse.current.leftButton.isPressed)Pointer(Mouse.current.position.ReadValue());
                if(Touchscreen.current!=null && Touchscreen.current.primaryTouch.press.isPressed)Pointer(Touchscreen.current.primaryTouch.position.ReadValue());
            }
            dashTime=Mathf.Max(0,dashTime-dt);
            desiredX=Mathf.Clamp(desiredX,110,1490);
            x=Mathf.MoveTowards(x,desiredX,speed*dt);
            moving=Mathf.Abs(x-PreviousX)>.1f;
            bool fastRun=axis!=0 || IsSprinting;
            if(moving){facing=x>PreviousX?1:-1;walk+=dt*(IsSprinting?39:fastRun?28:16);idleTime=0;}
            else {walk=Mathf.MoveTowards(walk,Mathf.Round(walk/Mathf.PI)*Mathf.PI,dt*5);idleTime+=dt;}
            velocityY-=gravity*dt;Height=Mathf.Max(0,Height+velocityY*dt);if(Grounded)velocityY=0;
            hurtTime=Mathf.Max(0,hurtTime-dt);feedback=Mathf.Max(0,feedback-dt);
            RefreshPose(fastRun);
            UpdateDust(dt,fastRun && moving && Grounded);
        }
        void Pointer(Vector2 screen)
        {
            if(!game.GameCamera.pixelRect.Contains(screen))return;
            Vector2 point=DateRushArt.Logical(game.GameCamera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,10)));
            if(point.y>525 && point.y<810)desiredX=point.x-(GripReach+PlateCenterFromGrip)*facing;
        }
        void RefreshPose(bool fastRun)
        {
            transform.position=DateRushArt.World(new Vector2(x,705-Height));
            float bob=Grounded?(moving?Mathf.Abs(Mathf.Sin(walk))*4:Mathf.Sin(idleTime*2.8f)*1.5f):0;
            int runFrame=(int)(walk*.65f)%4;
            Sprite pose;
            if(animatedFrames!=null)
                pose=animatedFrames[!Grounded?5:moving?4+runFrame:IdleFrame()];
            else
                pose=!Grounded?run[1]:moving?run[runFrame]:idle;
            if(pose!=null && body.sprite!=pose)
                DateRushArt.SetSprite(body,pose,animatedFrames!=null?AnimatedBodySize:OriginalBodySize);

            bool runningPose=moving || !Grounded;
            float frameOffset=animatedFrames!=null && runningPose?6:0;
            figure.position=DateRushArt.World(new Vector2(x,698+frameOffset-Height-bob));body.flipX=facing<0;
            float tilt=fastRun && moving?-facing*(IsSprinting?9:5):Grounded?Mathf.Sin(walk)*2:-facing*7;
            figure.rotation=Quaternion.Euler(0,0,tilt);
            float handY=animatedFrames==null?-.10f:runningPose?-.06f:-.13f;
            Vector3 hand=figure.position+figure.rotation*new Vector3(GripReach*facing/100f,handY,0);
            BasketPosition=DateRushArt.Logical(hand)+new Vector2(PlateCenterFromGrip*facing,-5);
            plate.position=DateRushArt.World(BasketPosition+new Vector2(0,11));
            plate.rotation=Quaternion.Euler(0,0,feedback>0?Mathf.Sin(feedback*32)*5:0);
            plateRenderer.color=feedback>0?new Color(1,1,.7f):Color.white;
            shadow.position=DateRushArt.World(new Vector2(x,801));shadow.localScale=new Vector3((150-Height*.22f)/128,24f/128,1);
            body.color=new Color(1,1,1,hurtTime>0 && Mathf.Sin(hurtTime*35)>0?.45f:1);
            dashArcs.enabled=fastRun && moving && Grounded;
            dashArcs.color=new Color(1,.52f,.25f,IsSprinting?.75f:.42f);
            dashArcs.transform.position=DateRushArt.World(new Vector2(x-10*facing,784));
            dashArcs.transform.rotation=Quaternion.Euler(0,0,-walk*90*facing);
            // Collider follows the character; catches use the plate's rim for fast fruit.
        }
        int IdleFrame()
        {
            if(feedback>0)return 3;
            float phase=Mathf.Repeat(idleTime,4.8f);
            if((phase>1.02f && phase<1.15f) || (phase>4.05f && phase<4.18f))return 1;
            if(phase>2.1f && phase<2.85f)return 2;
            return phase>3.05f && phase<3.62f?3:0;
        }
        void UpdateDust(float dt,bool kickingDust)
        {
            foreach(var puff in dust)
            {
                if(!puff.renderer.enabled)continue;
                puff.age+=dt;
                float progress=Mathf.Clamp01(puff.age/puff.duration);
                puff.position+=puff.velocity*dt;
                puff.renderer.transform.position=DateRushArt.World(puff.position);
                float size=Mathf.Lerp(18,61,progress);
                puff.renderer.transform.localScale=new Vector3(size/128,size*.68f/128,1);
                puff.renderer.color=new Color(.86f,.72f,.58f,.67f*(1-progress));
                if(progress>=1)puff.renderer.enabled=false;
            }
            if(!kickingDust){dustTimer=0;return;}
            dustTimer-=dt;
            if(dustTimer>0)return;
            dustTimer=IsSprinting?.032f:.06f;
            var next=dust[dustIndex++%dust.Length];
            float variation=(dustIndex%4)/3f;
            next.age=0;next.duration=.31f+.12f*variation;
            next.position=new Vector2(x-(25+variation*13)*facing,797-variation*10);
            next.velocity=new Vector2(-(49+variation*43)*facing,-43-variation*29);
            next.renderer.transform.position=DateRushArt.World(next.position);
            next.renderer.transform.localScale=new Vector3(18f/128,12f/128,1);
            next.renderer.color=new Color(.86f,.72f,.58f,.67f);
            next.renderer.enabled=true;
        }
        public void Jump(){if(game.State==RoundState.Playing && Grounded)velocityY=jumpSpeed;}
        internal void ShowTargetDates(int variety)
        {
            var sprite=game.GetDateSprite(variety);
            foreach(var date in sampleDates)DateRushArt.SetSprite(date,sprite,SampleDateSize);
        }
        public void Dash(){if(game.State==RoundState.Playing)dashTime=.2f;}
        public void CatchFeedback(bool correct){feedback=.3f;}
        public void Hurt(){hurtTime=1.5f;}
    }
}

