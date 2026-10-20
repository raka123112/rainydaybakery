using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace RainyDayBakery
{
    public sealed class BakeryWorld : MonoBehaviour
    {
        public Camera worldCamera;
        public bool interior;
        public Action<int> arrived;
        public Vector3 PlayerPosition { get { return player.transform.position; } }
        public readonly string[] VillageNames = { "Bakery", "Kebun herba", "Ladang buah", "Jamur pesisir", "Mercusuar", "Dermaga", "Sumur · istirahat", "Nami" };
        public readonly string[] InteriorNames = { "Keluar", "Pantry", "Meja adonan", "Oven batu", "Etalase · sajikan", "Nami", "Pak Aruna" };
        public string[] Names { get { return interior ? InteriorNames : VillageNames; } }
        readonly Vector2[] village = { new Vector2(.53f,.50f), new Vector2(.27f,.48f), new Vector2(.26f,.69f), new Vector2(.67f,.49f), new Vector2(.80f,.25f), new Vector2(.78f,.68f), new Vector2(.48f,.76f), new Vector2(.59f,.57f), new Vector2(.40f,.57f), new Vector2(.63f,.61f), new Vector2(.66f,.38f), new Vector2(.71f,.28f) };
        readonly int[,] villageEdges = { {0,8},{8,1},{1,2},{2,6},{6,8},{0,7},{7,9},{9,3},{3,10},{10,11},{11,4},{9,5},{6,9} };
        readonly Vector2[] room = { new Vector2(.50f,.84f), new Vector2(.30f,.30f), new Vector2(.30f,.58f), new Vector2(.59f,.37f), new Vector2(.66f,.63f), new Vector2(.75f,.39f), new Vector2(.70f,.73f), new Vector2(.49f,.65f), new Vector2(.47f,.49f), new Vector2(.40f,.30f) };
        readonly int[,] roomEdges = { {0,7},{7,2},{1,9},{9,8},{2,8},{8,3},{8,4},{4,5},{4,6},{7,4} };
        SpriteRenderer backdrop, player, nami, aruna;
        Sprite[,] characters = new Sprite[3,3];
        Sprite[] portraits = new Sprite[3];
        Light2D global, oven, lamp;
        ParticleSystem rain;
        SpriteRenderer fog, rays;
        readonly List<SpriteRenderer> ripples=new List<SpriteRenderer>();
        readonly Queue<Vector3> route = new Queue<Vector3>();
        int target = -1, facing;
        float walkClock;
        Sky sky;
        Material lit, unlit;
        public Sprite Portrait(int who) { return portraits[who]; }
        public void Initialize()
        {
            lit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            var cam = new GameObject("Isometric Camera", typeof(Camera), typeof(AudioListener));
            worldCamera = cam.GetComponent<Camera>(); cam.tag = "MainCamera";
            worldCamera.orthographic = true; worldCamera.orthographicSize = 8.7f;
            worldCamera.transform.position = new Vector3(0, 0, -20);
            worldCamera.backgroundColor = ColorHex("8aaca5");
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.rect = new Rect(0, .17f, .755f, .735f);
            worldCamera.transparencySortMode = TransparencySortMode.CustomAxis;
            worldCamera.transparencySortAxis = Vector3.up;
            cam.AddComponent<UniversalAdditionalCameraData>();
            var grid = new GameObject("Isometric Z As Y Grid", typeof(Grid)).GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY; grid.cellSize = new Vector3(1,.5f,1);
            var tiles = new GameObject("Environment Base Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
            tiles.transform.SetParent(grid.transform); tiles.GetComponent<TilemapRenderer>().sortingLayerName = "Environment_Base";
            backdrop = MakeSprite("Illustrated environment", null, "Background", 0);
            Texture2D sheet = Resources.Load<Texture2D>("Art/characters");
            if (sheet == null) throw new InvalidOperationException("Missing Art/characters texture. Reimport the project assets.");
            for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
            {
                Rect rect = new Rect(col * sheet.width / 3f, (2-row) * sheet.height / 3f, sheet.width / 3f, sheet.height / 3f);
                rect = Trim(sheet, rect);
                characters[row,col] = Sprite.Create(sheet, rect, new Vector2(.5f,.02f), rect.height / 1.9f);
                if (col == 0) portraits[row] = Sprite.Create(sheet, rect, new Vector2(.5f,.5f), 100);
            }
            player = MakeSprite("Mira", characters[0,0], "Characters", 0);
            nami = MakeSprite("Nami", characters[1,0], "Characters", 0);
            aruna = MakeSprite("Pak Aruna", characters[2,0], "Characters", 0);
            global = new GameObject("Weather · Global Light 2D").AddComponent<Light2D>(); global.lightType = Light2D.LightType.Global;
            oven = MakeLight("Oven · Amber", new Vector3(2.5f,2.3f,0), 4);
            lamp = MakeLight("Pendant · Amber", new Vector3(-3,2,0), 5);
            CreateEffects(); SwitchArea(false); SetWeather(Sky.Rain, 0);
        }
        static Rect Trim(Texture2D texture, Rect cell)
        {
            int left=(int)cell.x, bottom=(int)cell.y, width=(int)cell.width, height=(int)cell.height;
            Color[] pixels=texture.GetPixels(left,bottom,width,height);
            int minX=width,minY=height,maxX=0,maxY=0;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(pixels[y*width+x].a>.25f)
            { minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y); }
            if(minX>maxX)return cell;
            return new Rect(left+minX,bottom+minY,maxX-minX+1,maxY-minY+1);
        }
        SpriteRenderer MakeSprite(string name, Sprite sprite, string layer, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = lit;
            sr.sortingLayerName = layer; sr.sortingOrder = order; return sr;
        }
        Light2D MakeLight(string name, Vector3 position, float radius)
        {
            var l = new GameObject(name).AddComponent<Light2D>(); l.lightType = Light2D.LightType.Point;
            l.transform.position = position; l.color = ColorHex("FF9E3B"); l.intensity = 1.4f;
            l.pointLightOuterRadius = radius; l.pointLightInnerRadius = .5f; return l;
        }
        static Color ColorHex(string h) { Color c; ColorUtility.TryParseHtmlString("#"+h, out c); return c; }
        public Vector3 Position(int index) { return ToWorld((interior ? room : village)[index]); }
        static Vector3 ToWorld(Vector2 p) { return new Vector3((p.x-.5f)*24, (.5f-p.y)*16,0); }
        public void SwitchArea(bool inside)
        {
            interior = inside; route.Clear(); target = -1;
            var texture = Resources.Load<Texture2D>(inside ? "Art/interior" : "Art/village");
            if (texture == null) throw new InvalidOperationException("Missing environment artwork.");
            backdrop.sprite = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), new Vector2(.5f,.5f), texture.width/24f);
            backdrop.transform.localScale = new Vector3(1, 16f / (24f * texture.height / texture.width), 1);
            player.transform.position = Position(0);
            SetWeather(sky, 0);
        }
        public void SetWeather(Sky value, int story)
        {
            sky = value;
            if(!interior && !OnPath(player.transform.position))
            {
                int nearest=0;
                for(int i=0;i<village.Length;i++)
                    if((sky==Sky.Sun||(i!=2&&i!=5))&&(Position(i)-player.transform.position).sqrMagnitude<(Position(nearest)-player.transform.position).sqrMagnitude)nearest=i;
                player.transform.position=Position(nearest);
            }
            global.color = interior ? ColorHex("FFF2D4") : value == Sky.Rain ? ColorHex("4A607A") : value == Sky.Fog ? ColorHex("8C8882") : ColorHex("FFF2D4");
            global.intensity = interior ? .85f : value == Sky.Rain ? .65f : value == Sky.Fog ? .50f : 1.10f;
            oven.gameObject.SetActive(interior); lamp.gameObject.SetActive(interior);
            rain.gameObject.SetActive(!interior && sky == Sky.Rain);
            foreach(var ripple in ripples)ripple.gameObject.SetActive(!interior&&sky==Sky.Rain);
            fog.gameObject.SetActive(!interior && sky == Sky.Fog);
            rays.gameObject.SetActive(!interior && sky == Sky.Sun);
            nami.transform.position = Position(interior ? 5 : 7);
            aruna.gameObject.SetActive(interior ? sky == Sky.Rain : sky != Sky.Rain);
            aruna.transform.position = Position(interior ? 6 : sky == Sky.Fog ? 4 : 5) + new Vector3(.4f,.2f,0);
            Sort(nami); Sort(aruna);
        }
        void Sort(SpriteRenderer sprite) { sprite.sortingOrder = Mathf.RoundToInt(-sprite.transform.position.y * 100); }
        int Closest(Vector3 p)
        {
            int best = 0; var nodes = interior ? room : village;
            for (int i=1;i<nodes.Length;i++) if ((Position(i)-p).sqrMagnitude < (Position(best)-p).sqrMagnitude) best=i;
            return best;
        }
        public void WalkTo(int index)
        {
            int start = Closest(player.transform.position); int[,] edges = interior ? roomEdges : villageEdges;
            int count = (interior ? room : village).Length; int[] previous = new int[count];
            for (int i=0;i<count;i++) previous[i]=-1;
            var q = new Queue<int>(); q.Enqueue(start); previous[start]=start;
            while(q.Count>0)
            {
                int n=q.Dequeue(); if(n==index) break;
                for(int e=0;e<edges.GetLength(0);e++)
                { if(!EdgeOpen(edges[e,0],edges[e,1]))continue;int next=edges[e,0]==n?edges[e,1]:edges[e,1]==n?edges[e,0]:-1; if(next>=0&&previous[next]<0){previous[next]=n;q.Enqueue(next);} }
            }
            if(previous[index]<0)return;
            var reverse=new List<int>(); int current=index;
            while(current!=start){reverse.Add(current);current=previous[current];}
            reverse.Add(start); reverse.Reverse(); route.Clear(); foreach(int n in reverse)route.Enqueue(Position(n)); target=index;
        }
        public void CancelWalk() { route.Clear(); target=-1; }
        public void Simulate(float dt, bool enabled)
        {
            float effectTime = Time.unscaledTime;
            fog.color = new Color(.88f,.87f,.81f,.26f + Mathf.Sin(effectTime*.3f)*.05f);
            fog.transform.position = new Vector3(Mathf.Sin(effectTime*.1f)*2,0,-1);
            foreach(var ripple in ripples)
            {
                float phase=Mathf.Repeat(effectTime*.6f+ripple.transform.position.x,1);
                ripple.transform.localScale=new Vector3(phase*.9f,phase*.35f,1);
                ripple.color=new Color(.8f,.9f,.9f,(1-phase)*.35f);
            }
            if (!enabled) { if(rain.isPlaying)rain.Pause(); return; }
            if(rain.gameObject.activeSelf&&!rain.isPlaying)rain.Play();
            Vector2 input = new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),
                (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
            Vector3 old = player.transform.position;
            if(input.sqrMagnitude>0)
            {
                CancelWalk(); Vector3 candidate=old+(Vector3)(input.normalized*dt*3.2f);
                if(OnPath(candidate))player.transform.position=candidate;
            }
            else if(route.Count>0)
            {
                player.transform.position=Vector3.MoveTowards(old,route.Peek(),dt*3.8f);
                if(Vector3.Distance(player.transform.position,route.Peek())<.03f)route.Dequeue();
                if(route.Count==0&&target>=0){int action=target;target=-1;arrived?.Invoke(action);}
            }
            Vector3 delta=player.transform.position-old;
            if(delta.sqrMagnitude>.00001f)
            {
                facing = Mathf.Abs(delta.x)>Mathf.Abs(delta.y)*1.4f?1:delta.y>0?2:0;
                player.sprite=characters[0,facing]; player.flipX=facing==1&&delta.x<0;
                walkClock+=dt*11; player.transform.localScale=new Vector3(1,1+Mathf.Sin(walkClock)*.025f,1);
            }
            else player.transform.localScale=Vector3.one;
            Sort(player);
            if(Input.GetKeyDown(KeyCode.E))
            {
                int nearest=Closest(player.transform.position);
                if(nearest<Names.Length&&Vector3.Distance(Position(nearest),player.transform.position)<1.3f)arrived?.Invoke(nearest);
            }
        }
        bool EdgeOpen(int a,int b) {return interior||sky==Sky.Sun||(a!=2&&b!=2&&a!=5&&b!=5);}
        bool OnPath(Vector3 p)
        {
            int[,] edges=interior?roomEdges:villageEdges;
            for(int e=0;e<edges.GetLength(0);e++)
            {
                if(!EdgeOpen(edges[e,0],edges[e,1]))continue;
                Vector3 a=Position(edges[e,0]),b=Position(edges[e,1]),ab=b-a;
                Vector3 near=a+ab*Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude);
                if(Vector3.Distance(p,near)<.75f)return true;
            }
            return false;
        }
        void CreateEffects()
        {
            var texture=new Texture2D(64,64); var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            { float a=Mathf.Clamp01(1-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/32);pixels[y*64+x]=new Color(1,1,1,a*a); }
            texture.SetPixels(pixels);texture.Apply();
            var soft=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
            fog=MakeSprite("Screen-space fog veil",soft,"Foreground",20);fog.sharedMaterial=unlit;fog.transform.localScale=new Vector3(40,24,1);
            rays=MakeSprite("Golden light shaft",soft,"Foreground",19);rays.sharedMaterial=unlit;rays.color=new Color(1,.89f,.58f,.18f);
            rays.transform.position=new Vector3(-5,3,0);rays.transform.localScale=new Vector3(3,29,1);rays.transform.rotation=Quaternion.Euler(0,0,-32);
            rain=new GameObject("Rain · Particle System").AddComponent<ParticleSystem>(); rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main;main.startLifetime=2.5f;main.startSpeed=0;main.startSize=.06f;main.startColor=new Color(.8f,.9f,1,.4f);main.maxParticles=500;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=rain.emission;emission.rateOverTime=100;
            var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(25,1,1);
            var velocity=rain.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=-1.8f;velocity.y=-9;
            rain.transform.position=new Vector3(0,9,-2);
            var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=unlit;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=5;renderer.velocityScale=.1f;renderer.sortingLayerName="Foreground";
            rain.Play();
            var ringTexture=new Texture2D(64,64);var ringPixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {float radius=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));ringPixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(1-Mathf.Abs(radius-27)/2));}
            ringTexture.SetPixels(ringPixels);ringTexture.Apply();var ring=Sprite.Create(ringTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
            for(int i=0;i<16;i++){var sr=MakeSprite("Rain ripple",ring,"Environment_Decor",1);sr.sharedMaterial=unlit;sr.transform.position=new Vector3(-11+i*1.45f,-7.2f+(i%3)*.25f,0);ripples.Add(sr);}
        }
        void OnDestroy() { if(lit!=null)Destroy(lit);if(unlit!=null)Destroy(unlit); }
    }
}
