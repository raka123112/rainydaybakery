using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RainyDayBakery
{
    public sealed class BakeryUI : MonoBehaviour
    {
        RainyDayGame game;
        Canvas canvas;
        RectTransform root, hud, hotspots, modal;
        Font body, heading;
        Sprite paper, wood;
        Text day, status, goal, dialogue, speaker, location, save, sound;
        Image portrait, progress, golden;
        Text timer;
        readonly List<RectTransform> markers=new List<RectTransform>();
        readonly List<Button> skyButtons=new List<Button>();
        readonly List<Sprite> icons=new List<Sprite>();
        string lastArea="";
        public bool ModalOpen { get { return modal!=null; } }
        static Color C(string hex){Color c;ColorUtility.TryParseHtmlString("#"+hex,out c);return c;}
        readonly Color ink=new Color(.26f,.21f,.17f), cream=new Color(.98f,.94f,.84f), moss=new Color(.25f,.39f,.34f);
        public void Initialize(RainyDayGame controller)
        {
            game=controller;
            body=Resources.Load<Font>("Fonts/Body")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            heading=Resources.Load<Font>("Fonts/Heading")??body;
            paper=Frame(false);wood=Frame(true);
            canvas=new GameObject("Bakery UI · Screen Space",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root=canvas.GetComponent<RectTransform>();
            hud=Rect("HUD",root,0,0,1600,1000);hud.anchorMin=hud.anchorMax=new Vector2(.5f,.5f);hud.anchoredPosition=Vector2.zero;hud.pivot=new Vector2(.5f,.5f);
            Panel(hud,0,0,1600,88,wood);
            Label(hud,"RAINY DAY BAKERY",30,17,500,48,29,cream,true);
            day=Label(hud,"",575,20,550,42,21,cream);
            Button(hud,"Jeda",1457,19,113,46,()=>game.Pause());
            Panel(hud,1208,90,392,734,paper);
            location=Label(hud,"",1236,112,340,32,16,moss);
            Label(hud,"Ikuti langit.",1236,148,340,50,32,ink,true);
            string[] names={"Hujan","Kabut","Cerah"};
            for(int i=0;i<3;i++){int sky=i;skyButtons.Add(Button(hud,names[i],1236+i*110,217,102,45,()=>game.SetWeather(sky)));}
            goal=Label(hud,"",1236,285,334,94,19,ink);
            Label(hud,"DI MEJA KERJA",1236,399,334,26,14,moss);
            status=Label(hud,"",1236,437,334,96,21,ink,true);
            Panel(hud,1236,549,330,15,wood);
            golden=Panel(hud,1368,551,88,11,null);golden.color=C("91A878");
            progress=Panel(hud,1238,546,4,21,null);progress.color=C("DBAE5C");
            timer=Label(hud,"",1236,578,334,55,15,ink);
            Button(hud,"Buku resep",1236,649,330,49,()=>ShowRecipes());
            Button(hud,"Tas & jurnal",1236,712,330,49,()=>ShowInventory());
            var soundButton=Button(hud,"Suara",1236,777,155,34,()=>game.ToggleSound());sound=soundButton.GetComponentInChildren<Text>();
            Button(hud,"Bantuan",1404,777,162,34,()=>ShowHelp());
            Panel(hud,0,830,1600,170,wood);
            Panel(hud,17,841,1566,145,paper);
            portrait=Panel(hud,27,828,150,166,null);portrait.preserveAspect=true;portrait.color=Color.white;
            speaker=Label(hud,"NAMI",189,852,250,28,17,moss);
            dialogue=Label(hud,"",189,889,1030,82,21,ink);
            save=Label(hud,"",1252,861,310,90,14,moss);
            Label(hud,"Klik lokasi untuk berjalan · WASD / panah + E · Esc untuk jeda",28,797,1120,28,15,cream);
            hotspots=Rect("Location markers",root,0,0,0,0);hotspots.anchorMin=Vector2.zero;hotspots.anchorMax=Vector2.one;hotspots.pivot=new Vector2(.5f,.5f);hotspots.offsetMin=hotspots.offsetMax=Vector2.zero;
            hotspots.SetSiblingIndex(0);
            Texture2D atlas=Resources.Load<Texture2D>("Art/items");
            if(atlas!=null)for(int r=0;r<4;r++)for(int c=0;c<4;c++)icons.Add(Sprite.Create(atlas,new Rect(c*atlas.width/4f,(3-r)*atlas.height/4f,atlas.width/4f,atlas.height/4f),new Vector2(.5f,.5f),100));
            Refresh();
        }
        Sprite Frame(bool isWood)
        {
            int size=64;var t=new Texture2D(size,size);var px=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int edge=Mathf.Min(Mathf.Min(x,size-1-x),Mathf.Min(y,size-1-y));
                Color baseColor=isWood?C("99704C"):C("F7EBD1");
                float grain=(Mathf.Sin(y*.85f+Mathf.Sin(x*.1f)*2)+Mathf.Sin(x*12.3f+y*31.7f)*.35f)*(isWood?.035f:.008f);
                if(edge<2)baseColor=isWood?C("553D2D"):C("C0A580");else if(edge<5)baseColor=isWood?C("C29364"):C("E4D1AE");
                baseColor.r+=grain;baseColor.g+=grain;baseColor.b+=grain;
                if((x<7&&y<7&&Vector2.Distance(new Vector2(x,y),new Vector2(7,7))>7)||(x>56&&y<7&&Vector2.Distance(new Vector2(x,y),new Vector2(56,7))>7)||(x<7&&y>56&&Vector2.Distance(new Vector2(x,y),new Vector2(7,56))>7)||(x>56&&y>56&&Vector2.Distance(new Vector2(x,y),new Vector2(56,56))>7))baseColor.a=0;
                px[y*size+x]=baseColor;
            }
            t.SetPixels(px);t.Apply();return Sprite.Create(t,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(9,9,9,9));
        }
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);
            rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;
        }
        Image Panel(Transform parent,float x,float y,float w,float h,Sprite sprite)
        {var rt=Rect("Panel",parent,x,y,w,h);var image=rt.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=sprite!=null?Image.Type.Sliced:Image.Type.Simple;image.raycastTarget=false;return image;}
        Text Label(Transform parent,string value,float x,float y,float w,float h,int size,Color color,bool serif=false)
        {
            var rt=Rect(value.Length>25?"Text":value,parent,x,y,w,h);var t=rt.gameObject.AddComponent<Text>();t.font=serif?heading:body;t.fontSize=size;t.color=color;t.text=value;t.raycastTarget=false;t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        Button Button(Transform parent,string value,float x,float y,float w,float h,Action action)
        {
            var image=Panel(parent,x,y,w,h,paper);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=C("E8CB9D");colors.pressedColor=C("D4AE78");colors.disabledColor=C("B6ADA0");button.colors=colors;
            var t=Label(image.transform,value,8,4,w-16,h-8,17,ink);t.alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(()=>action());return button;
        }
        public void Say(int who,string message)
        {speaker.text=new[]{"MIRA · PEMBUAT ROTI","NAMI · PENGAMAT CUACA","PAK ARUNA · PENJAGA MERCUSUAR"}[who];dialogue.text=message;portrait.sprite=game.world.Portrait(who);}
        public void Refresh()
        {
            if(hud==null)return;
            hud.gameObject.SetActive(game.playing);hotspots.gameObject.SetActive(game.playing&&!ModalOpen);
            string area=game.world.interior?"BAKERY":"DESA PESISIR";
            if(area!=lastArea){lastArea=area;BuildMarkers();}
            for(int i=0;i<skyButtons.Count;i++){var colors=skyButtons[i].colors;colors.normalColor=game.model.data.sky==i?C("D5B47D"):Color.white;skyButtons[i].colors=colors;}
            sound.text=game.muted?"Suara: mati":"Suara: aktif";
            RefreshStatus();
        }
        public void RefreshStatus()
        {
            var d=game.model.data;var model=game.model;
            day.text="Hari "+d.day.ToString("00")+"     ·     "+d.shells+" kerang     ·     "+d.served+" roti dibagikan";
            location.text=(game.world.interior?"DI DALAM BAKERY":"DESA PESISIR")+"  /  "+new[]{"HUJAN","KABUT","CERAH"}[d.sky];
            goal.text=model.NextGoal;
            status.text=d.stage==WorkStage.Empty?"Resep pilihan\n"+BakeryModel.Recipes[d.selectedRecipe].name:BakeryModel.Recipes[d.batchRecipe].name+"\n"+StageName(d.stage);
            float ratio=0;golden.gameObject.SetActive(d.stage==WorkStage.Baking);
            if(d.stage==WorkStage.Baking)
            {
                float maximum=model.GoldenStart+7;ratio=d.bakeSeconds/maximum;
                var gr=golden.rectTransform;gr.anchoredPosition=new Vector2(1238+326*model.GoldenStart/maximum,-551);gr.sizeDelta=new Vector2(326*4/maximum,11);
                timer.text=d.bakeSeconds.ToString("0.0")+" dtk · Keemasan "+model.GoldenStart+"–"+(model.GoldenStart+4)+" dtk\nKlik Oven batu untuk mengangkat.";
            }
            else if(d.stage==WorkStage.Proofing){ratio=1-d.proofSeconds/(d.batchSky==0?12:6);timer.text="Fermentasi: "+Mathf.CeilToInt(d.proofSeconds)+" detik lagi.";}
            else if(d.stage==WorkStage.Mixing){ratio=d.kneads/3f;timer.text="Uleni "+d.kneads+" / 3 · Klik meja adonan.";}
            else timer.text=d.stage==WorkStage.Ready?d.quality+" · Siap disajikan.":d.stage==WorkStage.Dough?"Fermentasi selesai · Bawa ke oven.":"Pantry → resep → adonan → oven → sajikan";
            progress.rectTransform.anchoredPosition=new Vector2(1238+Mathf.Clamp01(ratio)*326,-546);
            save.text=(game.SaveStatus??"Progres otomatis")+"\n\nTanpa batas waktu.\nTak ada game over.";
        }
        static string StageName(WorkStage s){switch(s){case WorkStage.Mixing:return "Menguleni";case WorkStage.Proofing:return "Fermentasi";case WorkStage.Dough:return "Adonan siap";case WorkStage.Baking:return "Di dalam oven";default:return "Siap disajikan";}}
        void BuildMarkers()
        {
            foreach(var rt in markers)Destroy(rt.gameObject);markers.Clear();
            for(int i=0;i<game.world.Names.Length;i++)
            {
                int index=i;var b=Button(hotspots,game.world.Names[i],0,0,164,34,()=>game.Visit(index));
                b.GetComponentInChildren<Text>().fontSize=14;var rt=b.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);markers.Add(rt);
            }
        }
        void LateUpdate()
        {
            if(game==null)return;
            var corners=new Vector3[4];hud.GetWorldCorners(corners);float scale=canvas.scaleFactor;
            game.world.worldCamera.pixelRect=game.playing?new Rect(corners[0].x,corners[0].y+170*scale,1208*scale,742*scale):new Rect(0,0,Screen.width,Screen.height);
            for(int i=0;i<markers.Count;i++)
            {
                Vector3 screen=game.world.worldCamera.WorldToScreenPoint(game.world.Position(i));Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(hotspots,screen,null,out local);markers[i].anchoredPosition=local+new Vector2(0,-23);
                bool visible=!(game.world.interior&&i==6&&game.model.data.sky!=0);
                markers[i].gameObject.SetActive(visible);
            }
        }
        RectTransform Modal(string title,float width=980,float height=750)
        {
            HideModal();game.world.CancelWalk();
            modal=Rect("Modal",root,0,0,0,0);modal.anchorMin=Vector2.zero;modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=Vector2.zero;
            var shade=modal.gameObject.AddComponent<Image>();shade.color=new Color(.12f,.16f,.14f,.78f);shade.raycastTarget=true;
            var frame=Panel(modal,0,0,width,height,wood).rectTransform;frame.anchorMin=frame.anchorMax=new Vector2(.5f,.5f);frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;
            Panel(frame,14,14,width-28,height-28,paper);
            Label(frame,title,40,30,width-150,60,34,ink,true);
            hotspots.gameObject.SetActive(false);
            return frame;
        }
        public void HideModal()
        {if(modal!=null){Destroy(modal.gameObject);modal=null;}if(hotspots!=null)hotspots.gameObject.SetActive(game.playing);}
        public void ShowMenu()
        {
            var f=Modal("",640,690);f.anchorMin=f.anchorMax=new Vector2(.73f,.5f);
            modal.GetComponent<Image>().color=new Color(.12f,.16f,.14f,.18f);
            Label(f,"SEBUAH RUMAH DI PESISIR",52,55,530,30,16,moss);
            Label(f,"Rainy Day\nBakery",48,115,540,168,66,ink,true);
            Label(f,"Sedikit hujan. Sepotong roti.\nCerita yang tumbuh perlahan.",52,325,530,85,25,ink);
            Button(f,game.model.data.served>0||game.model.data.inventory.Count>0?"Lanjutkan harimu":"Buka pintu bakery",52,447,530,62,()=>game.Begin());
            Button(f,"Cara bermain",52,531,530,48,()=>ShowHelp());
            Label(f,"Mira, Nami, dan Pak Aruna menunggumu.\nUnity 2022.3 · Revised edition",52,609,530,55,16,moss);
            Refresh();
        }
        public void ShowPause()
        {
            var f=Modal("Tarik napas sebentar.",680,490);
            Label(f,"Oven dan fermentasi ikut beristirahat.\nProgresmu tersimpan otomatis.",42,117,590,90,23,ink);
            Button(f,"Kembali ke desa",42,245,596,56,()=>game.Pause());
            Button(f,"Simpan & menu utama",42,324,596,49,()=>game.MainMenu());
            Button(f,game.muted?"Aktifkan suara":"Matikan suara",42,393,596,43,()=>{game.ToggleSound();ShowPause();});
        }
        public void ShowHelp()
        {
            var f=Modal("Satu hari yang hangat",900,720);
            Label(f,"1   Klik Bakery, lalu Pantry untuk mengambil bahan dasar.\n2   Pilih resep di Buku Resep; kumpulkan bahan yang kurang.\n3   Klik Meja adonan untuk mencampur, lalu uleni tiga kali.\n4   Tunggu fermentasi: 12 detik saat hujan, 6 detik lainnya.\n5   Masukkan ke Oven batu, lalu angkat pada rentang keemasan.\n6   Sajikan di Etalase, atau bawakan langsung ke Pak Aruna.\n\nHujan: herba + blueberry, Aruna di bakery.\nKabut: jamur liar, Aruna di mercusuar.\nCerah: buah + madu, Aruna di dermaga.\n\nKlik penanda lokasi atau gunakan WASD / panah + E.\nEsc untuk jeda. Buku dan jurnal juga menghentikan oven.\nBahan yang sudah dicampur tidak berubah saat cuaca diganti.",42,111,816,501,21,ink);
            Button(f,"Mengerti",42,630,816,48,()=>{if(game.playing)HideModal();else ShowMenu();});
        }
        public void ShowRecipes()
        {
            var f=Modal("Buku resep Mira",1120,850);
            Button(f,"Tutup",939,35,136,43,()=>HideModal());
            Label(f,"Resep terbuka saat kamu berbagi roti. Setiap bahan terpakai satu per adonan.",42,99,1030,35,17,moss);
            for(int i=0;i<8;i++)
            {
                int index=i;int col=i%2,row=i/2;float x=40+col*540,y=151+row*160;
                Panel(f,x,y,520,143,wood);Panel(f,x+5,y+5,510,133,paper);
                if(icons.Count>i){var icon=Panel(f,x+9,y+12,91,93,null);icon.sprite=icons[i];icon.preserveAspect=true;}
                var recipe=BakeryModel.Recipes[i];bool unlocked=game.model.Unlocked(i);
                Label(f,recipe.name,x+105,y+12,394,32,23,ink,true);
                string needs="";foreach(string ingredient in recipe.ingredients)needs+=BakeryModel.ItemName(ingredient)+" "+game.model.Count(ingredient)+"/1  ";
                Label(f,needs,x+105,y+49,390,41,14,ink);
                var choose=Button(f,unlocked?(game.model.data.selectedRecipe==i?"Dipilih":"Pilih resep"):"Buka: "+recipe.unlock+" roti dibagikan",x+105,y+96,390,33,()=>game.SelectRecipe(index));choose.interactable=unlocked;
            }
        }
        public void ShowInventory()
        {
            var f=Modal("Tas & cerita pesisir",1040,820);Button(f,"Tutup",859,35,136,43,()=>HideModal());
            Label(f,"BAHAN TERKUMPUL",42,109,460,28,15,moss);
            for(int i=0;i<BakeryModel.ItemIds.Length;i++)
            {
                string id=BakeryModel.ItemIds[i];
                if(i<8&&icons.Count>i+8){var icon=Panel(f,48,150+i*43,42,37,null);icon.sprite=icons[i+8];icon.preserveAspect=true;}
                Label(f,BakeryModel.ItemNames[i],99,154+i*43,260,32,20,ink);
                Label(f,game.model.Count(id).ToString(),384,154+i*43,80,32,21,moss);
            }
            Label(f,"CATATAN DESA",546,109,440,28,15,moss);
            Label(f,game.model.NextGoal,546,154,430,105,23,ink,true);
            var d=game.model.data;
            Label(f,"Roti dibagikan: "+d.served+"\nResep pernah dipanggang: "+d.baked.Count+" / 8\nBahan ditemukan: "+d.discovered.Count+" / 10\nKehangatan warga: "+d.friendship+"\nGudang madu: "+(d.story>=2?"Terbuka":"Bantu Pak Aruna"),546,285,440,180,21,ink);
            Label(f,"LOKASI BAHAN\nPantry: tepung, telur, mentega, susu, kayu manis.\nKebun: herba; blueberry saat hujan.\nPesisir berkabut: jamur liar.\nLadang cerah: buah musim dan madu.",546,495,440,180,19,ink);
            Label(f,"Tidak ada batas waktu atau kehilangan progres saat roti kurang sempurna.\nIstirahat di sumur untuk memulai hari baru setelah adonan selesai.",42,698,950,70,18,moss);
        }
    }
}
