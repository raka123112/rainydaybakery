using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RainyDayBakery
{
    public sealed class RainyDayGame : MonoBehaviour
    {
        public BakeryModel model { get; private set; }
        public BakeryWorld world { get; private set; }
        public BakeryUI ui { get; private set; }
        public bool playing { get; private set; }
        public bool paused { get; private set; }
        public bool muted { get; private set; }
        public string SaveStatus { get; private set; }
        float autosave, refresh;
        readonly float[] harvestedAt = new float[8];
        string savePath;
        AudioSource ambience, chime;
        AudioClip note;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        { if (FindObjectOfType<RainyDayGame>() == null) new GameObject("Rainy Day Bakery · Game").AddComponent<RainyDayGame>(); }
        void Start()
        {
            Application.targetFrameRate = 60;
            savePath = Path.Combine(Application.persistentDataPath,"rainy-day-v2.json");
            model = new BakeryModel(Load());
            for(int i=0;i<harvestedAt.Length;i++)harvestedAt[i]=-100;
            if(FindObjectOfType<EventSystem>()==null)new GameObject("UI Event System",typeof(EventSystem),typeof(StandaloneInputModule));
            world = gameObject.AddComponent<BakeryWorld>(); world.Initialize(); world.arrived = Interact;
            world.SetWeather((Sky)model.data.sky,model.data.story);
            ui = gameObject.AddComponent<BakeryUI>(); ui.Initialize(this); ui.ShowMenu();world.SetWeather(Sky.Sun,model.data.story);
            SetupAudio();
            Say(1,"Selamat datang, Mira. Ambil bahan di pantry, lalu buka Buku Resep. Kita mulai dari satu roti hangat.");
        }
        BakerySave Load()
        {
            foreach(string path in new[]{savePath,savePath+".bak"})
            {
                if(!File.Exists(path))continue;
                try
                {
                    string json=File.ReadAllText(path);
                    if(!json.Contains("\"version\""))throw new FormatException("Missing save version");
                    var data=JsonUtility.FromJson<BakerySave>(json);
                    if(data==null||data.version!=2)throw new FormatException("Unsupported save version");
                    SaveStatus=path.EndsWith(".bak")?"Cadangan progres dipulihkan":"Progres dimuat"; return data;
                }
                catch(Exception ex) when(ex is IOException || ex is ArgumentException || ex is FormatException)
                { Debug.LogWarning("Cannot load bakery save: "+ex.Message); SaveStatus="Save bermasalah; mencoba cadangan"; }
            }
            return new BakerySave {shells=PlayerPrefs.GetInt("RainyBakery.Coins",0),day=PlayerPrefs.GetInt("RainyBakery.Days",0)+1};
        }
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary=savePath+".tmp";
                File.WriteAllText(temporary,JsonUtility.ToJson(model.data,true));
                if(File.Exists(savePath))File.Replace(temporary,savePath,savePath+".bak");else File.Move(temporary,savePath);
                SaveStatus="Progres tersimpan";
            }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
            { SaveStatus="Gagal menyimpan: periksa izin folder";Debug.LogError(ex); }
        }
        public void Begin()
        { playing=true;paused=false;ui.HideModal();world.CancelWalk();world.SetWeather((Sky)model.data.sky,model.data.story);ui.Refresh();Say(1,model.NextGoal); }
        public void Pause()
        { if(!playing)return;paused=!paused;if(paused){Save();ui.ShowPause();}else ui.HideModal(); }
        public void MainMenu()
        { Save();playing=false;paused=false;world.CancelWalk();world.SwitchArea(false);world.SetWeather(Sky.Sun,model.data.story);ui.ShowMenu(); }
        public void SetWeather(int weather)
        {
            model.data.sky=weather;world.CancelWalk();world.SetWeather((Sky)weather,model.data.story);Save();ui.Refresh();
            Say(1,weather==0?"Hujan menyuburkan herba dan blueberry. Warga berteduh di bakery; fermentasi menjadi 12 detik.":weather==1?"Kabut membuka jamur liar di pesisir. Pak Aruna menunggu di mercusuar; bawakan aroma rempah.":"Cerah membuka ladang buah dan dermaga. Pak Aruna berjalan ke dermaga, mencari baguette renyah.");
        }
        public void SelectRecipe(int index)
        {
            if(!model.Unlocked(index))return;model.data.selectedRecipe=index;ui.HideModal();Save();ui.Refresh();
            Say(0,"Resep dipilih: "+BakeryModel.Recipes[index].name+". Siapkan bahan, lalu gunakan meja adonan.");
        }
        public void Visit(int index)
        {
            if(!playing||paused||ui.ModalOpen)return;
            if(!world.interior&&index==2&&model.data.sky!=2){Say(1,"Ladang masih basah. Pilih cuaca Cerah agar jalurnya terbuka.");return;}
            if(!world.interior&&index==5&&model.data.sky!=2){Say(1,"Dermaga licin. Kita kembali ke sana saat Cerah.");return;}
            world.WalkTo(index);
        }
        public void Interact(int index)
        {
            if(!playing||paused||ui.ModalOpen)return;
            if(world.interior) Inside(index); else Outside(index);
            Save();ui.Refresh();
        }
        bool Harvest(int index)
        {
            if(Time.time-harvestedAt[index]<6){Say(1,"Biarkan tanaman pulih sebentar (6 detik). Kamu boleh menjelajah dulu.");return false;}
            harvestedAt[index]=Time.time;return true;
        }
        void Outside(int index)
        {
            switch(index)
            {
                case 0:world.SwitchArea(true);world.SetWeather((Sky)model.data.sky,model.data.story);Say(0,"Rumah yang hangat. Pantry di kiri, oven di belakang, etalase di kanan.");break;
                case 1:
                    if(Harvest(index)){model.Add("herb",model.data.sky==0?3:1);if(model.data.sky==0)model.Add("blueberry",2);Say(0,model.data.sky==0?"Herba +3, blueberry +2. Hujan membuat kebun hidup.":"Herba +1. Saat hujan, kebun ini memberi lebih banyak.");}break;
                case 2:
                    if(model.data.sky!=2){Say(1,"Ladang terbuka saat Cerah.");break;}
                    if(Harvest(index)){model.Add("fruit",2);model.Add("honey",1);Say(0,"Buah musim +2, madu +1. Sinar matahari terasa manis.");}break;
                case 3:
                    if(model.data.sky!=1){Say(1,"Jamur liar baru muncul ketika kabut turun.");break;}
                    if(Harvest(index)){model.Add("mushroom",2);Say(0,"Jamur liar +2. Cukup untuk membuat roti hangat bagi Aruna.");}break;
                case 4:
                    if(model.data.sky!=1){Say(1,"Aruna ada di bakery saat Hujan dan di dermaga saat Cerah. Temui dia di sini saat Kabut.");break;}
                    Aruna();break;
                case 5:
                    if(model.data.sky!=2){Say(1,"Dermaga kembali terbuka saat Cerah.");break;}
                    if(model.data.stage==WorkStage.Ready){Aruna();break;}
                    if(model.data.story>=2&&Harvest(index)){model.Add("honey",2);Say(2,"Kunci gudang pesisir ini milikmu. Ambil madu dari kiriman nelayan: +2.");}
                    else Aruna();break;
                case 6:
                    if(model.data.stage!=WorkStage.Empty){Say(1,"Selesaikan adonan atau sajikan rotimu dulu sebelum beristirahat.");break;}
                    model.data.day++;Say(0,"Hari baru, angin baru. Semua bahan, resep, dan persahabatan tetap bersamamu.");Ping();break;
                case 7:Hint();break;
            }
        }
        void Inside(int index)
        {
            switch(index)
            {
                case 0:world.SwitchArea(false);world.SetWeather((Sky)model.data.sky,model.data.story);Say(0,"Mari melihat kabar desa dan mencari bahan musiman.");break;
                case 1:
                    foreach(string id in new[]{"flour","egg","butter","milk","cinnamon"})model.Add(id,3);
                    Say(1,"Pantry terisi: tepung, telur, mentega, susu, kayu manis masing-masing +3. Bahan dasar selalu tersedia.");Ping();break;
                case 2:
                    if(model.data.stage==WorkStage.Empty)
                    {
                        if(model.Mix(model.data.selectedRecipe))Say(0,"Bahan siap. Klik meja tiga kali lagi untuk melipat dan menguleni.");
                        else {Say(1,"Bahan belum lengkap. Lihat kebutuhan dan lokasi bahan di Buku Resep.");ui.ShowRecipes();}
                    }
                    else if(model.Knead()){Say(0,model.data.stage==WorkStage.Proofing?"Adonan beristirahat. Cuaca saat mencampur menentukan waktu fermentasi.":"Lipat, tekan, tarik napas. Uleni "+model.data.kneads+" / 3.");Ping();}
                    else Say(1,"Adonanmu sedang diproses. Lihat status di panel kanan.");break;
                case 3:
                    if(model.PutInOven())Say(0,"Roti masuk oven. Angkat saat indikator berada di jendela keemasan.");
                    else if(model.data.stage==WorkStage.Baking){Say(0,model.TakeOut()+"! Bawa ke etalase, atau temui Aruna langsung di desa.");Ping();}
                    else Say(1,model.data.stage==WorkStage.Proofing?"Tunggu fermentasi selesai sebelum memasukkan adonan.":"Oven membutuhkan adonan yang sudah diuleni dan difermentasi.");break;
                case 4:Serve(false);break;
                case 5:Hint();break;
                case 6:if(model.data.sky==0)Aruna();else Say(1,"Aruna sedang di luar. Kamu tetap bisa melayani warga melalui etalase.");break;
            }
        }
        void Hint()
        {
            string destination=model.data.sky==0?"bakery":model.data.sky==1?"mercusuar":"dermaga";
            Say(1,"Aruna ada di "+destination+". Favorit cuaca ini: "+BakeryModel.Recipes[model.PreferredRecipe].name+". "+model.NextGoal);
        }
        void Aruna()
        {
            if(model.TalkAruna()){Say(2,"Kabut membuat cahaya mercusuar terasa jauh. Maukah kau membuat roti dari herba dan jamur pesisir? Aku akan membuka gudang madu di dermaga untukmu.");return;}
            if(model.data.stage==WorkStage.Ready){Serve(true);return;}
            Say(2,model.data.story>=2?"Sejak roti hangatmu datang, mercusuar tak lagi terasa sepi. Gudang madu di dermaga selalu terbuka untukmu saat cerah.":"Aku suka aroma rempah saat kabut, cinnamon roll saat hujan, dan baguette saat cerah. Tak perlu terburu-buru, Mira.");
        }
        void Serve(bool aruna)
        {
            int before=model.data.story;int unlockedBefore=UnlockedCount();int reward=model.Serve(aruna);
            if(reward==0){Say(aruna?2:1,"Kami menunggu dengan senang hati. Panggang roti dulu, lalu bawa ke sini.");return;}
            string thanks="Terima kasih! Bahkan roti yang tak sempurna punya kehangatannya. +"+reward+" kerang.";
            if(model.data.story>before)thanks="Roti ini menghangatkan malam di mercusuar. Terima kasih, Mira. Gudang madu dermaga kini terbuka! +"+reward+" kerang.";
            else if(UnlockedCount()>unlockedBefore)thanks+=" Resep baru terbuka di Buku Resep!";
            Say(aruna?2:1,thanks);Ping();
        }
        int UnlockedCount(){int n=0;for(int i=0;i<8;i++)if(model.Unlocked(i))n++;return n;}
        public void Say(int who,string message){if(ui!=null)ui.Say(who,message);}
        void Update()
        {
            if(model==null||ui==null)return;
            if(Input.GetKeyDown(KeyCode.Escape)) {if(ui.ModalOpen&&playing&&!paused)ui.HideModal();else Pause();}
            bool running=playing&&!paused&&!ui.ModalOpen;
            world.Simulate(Time.deltaTime,running);
            if(running)
            {
                WorkStage before=model.data.stage;model.Tick(Time.deltaTime);
                if(before==WorkStage.Proofing&&model.data.stage==WorkStage.Dough){Say(1,"Fermentasi selesai. Adonan siap masuk oven batu.");Ping();Save();}
                if((autosave+=Time.deltaTime)>=5){autosave=0;Save();}
            }
            if((refresh+=Time.unscaledDeltaTime)>.2f){refresh=0;ui.RefreshStatus();}
            if(ambience!=null)ambience.volume=muted?0:!world.interior&&model.data.sky==0?.09f:.025f;
        }
        void SetupAudio()
        {
            muted=PlayerPrefs.GetInt("RainyBakery.Muted",0)==1;
            ambience=gameObject.AddComponent<AudioSource>();chime=gameObject.AddComponent<AudioSource>();
            int rate=22050;float[] noise=new float[rate*4];var rng=new System.Random(29);float value=0;
            for(int i=0;i<noise.Length;i++){value=value*.94f+((float)rng.NextDouble()*2-1)*.06f;noise[i]=value;}
            var rainClip=AudioClip.Create("Soft coastal ambience",noise.Length,1,rate,false);rainClip.SetData(noise,0);ambience.clip=rainClip;ambience.loop=true;ambience.Play();
            float[] tone=new float[rate/3];for(int i=0;i<tone.Length;i++)tone[i]=Mathf.Sin(i*2*Mathf.PI*659.25f/rate)*Mathf.Exp(-i/(float)rate*13)*.15f;
            note=AudioClip.Create("Warm notification",tone.Length,1,rate,false);note.SetData(tone,0);
        }
        void Ping(){if(!muted&&chime!=null)chime.PlayOneShot(note);}
        public void ToggleSound(){muted=!muted;PlayerPrefs.SetInt("RainyBakery.Muted",muted?1:0);PlayerPrefs.Save();ui.Refresh();}
        void OnApplicationPause(bool value){if(value&&model!=null){Save();if(playing&&!paused){paused=true;ui.ShowPause();}}}
        void OnApplicationQuit(){if(model!=null)Save();}
    }
}
