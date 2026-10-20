using System;
using System.Collections.Generic;

namespace RainyDayBakery
{
    public enum Sky { Rain, Fog, Sun }
    public enum WorkStage { Empty, Mixing, Proofing, Dough, Baking, Ready }
    [Serializable] public sealed class PantryItem { public string id; public int count; public PantryItem(string key, int value) { id = key; count = value; } }
    [Serializable] public sealed class BakerySave
    {
        public int version = 2, day = 1, shells, served, friendship, story;
        public int sky, selectedRecipe, batchRecipe, batchSky, kneads;
        public WorkStage stage;
        public float proofSeconds, bakeSeconds;
        public string quality = "";
        public List<PantryItem> inventory = new List<PantryItem>();
        public List<string> discovered = new List<string>();
        public List<int> baked = new List<int>();
    }
    public sealed class Recipe
    {
        public readonly string name, description;
        public readonly string[] ingredients;
        public readonly int unlock, seconds;
        public Recipe(string n, string d, int u, int s, params string[] items)
        { name = n; description = d; unlock = u; seconds = s; ingredients = items; }
    }
    public sealed class BakeryModel
    {
        public static readonly string[] ItemIds = { "flour", "egg", "butter", "milk", "honey", "cinnamon", "blueberry", "mushroom", "herb", "fruit" };
        public static readonly string[] ItemNames = { "Tepung", "Telur", "Mentega", "Susu", "Madu", "Kayu manis", "Blueberry", "Jamur liar", "Herba", "Buah musim" };
        public static readonly Recipe[] Recipes = {
            new Recipe("Baguette", "Renyah, ringan, teman pagi cerah.", 0, 8, "flour", "butter"),
            new Recipe("Croissant", "Lipatan mentega untuk teman di dermaga.", 1, 9, "flour", "butter", "egg"),
            new Recipe("Cinnamon Roll", "Aroma kayu manis menghangatkan hari hujan.", 0, 8, "flour", "milk", "cinnamon"),
            new Recipe("Sourdough", "Roti fermentasi dengan madu pesisir.", 2, 10, "flour", "honey"),
            new Recipe("Pretzel", "Simpul kecil, hadiah untuk tetangga.", 3, 8, "flour", "butter", "herb"),
            new Recipe("Melonpan", "Buah musiman di bawah kulit renyah.", 4, 9, "flour", "egg", "fruit"),
            new Recipe("Cream Puff", "Krim susu dan blueberry yang lembut.", 5, 7, "flour", "egg", "milk", "blueberry"),
            new Recipe("Roti Rempah Pesisir", "Jamur kabut dan herba untuk Pak Aruna.", 0, 9, "flour", "mushroom", "herb")
        };
        public BakerySave data;
        public BakeryModel(BakerySave saved = null)
        {
            data = saved ?? new BakerySave();
            if (data.inventory == null) data.inventory = new List<PantryItem>();
            if (data.discovered == null) data.discovered = new List<string>();
            if (data.baked == null) data.baked = new List<int>();
            data.sky = Clamp(data.sky, 0, 2); data.selectedRecipe = Clamp(data.selectedRecipe, 0, 7);
            data.batchRecipe = Clamp(data.batchRecipe, 0, 7); data.batchSky = Clamp(data.batchSky, 0, 2);
            data.day = Math.Max(1, data.day); data.kneads = Clamp(data.kneads, 0, 3);
            data.proofSeconds = Math.Max(0, data.proofSeconds); data.bakeSeconds = Math.Max(0, data.bakeSeconds);
            if (!Enum.IsDefined(typeof(WorkStage), data.stage)) data.stage = WorkStage.Empty;
        }
        static int Clamp(int value, int low, int high) { return Math.Max(low, Math.Min(high, value)); }
        public static string ItemName(string id) { int i = Array.IndexOf(ItemIds, id); return i < 0 ? id : ItemNames[i]; }
        public int Count(string id) { var item = data.inventory.Find(x => x.id == id); return item == null ? 0 : item.count; }
        public void Add(string id, int amount)
        {
            var item = data.inventory.Find(x => x.id == id);
            if (item == null) { item = new PantryItem(id, 0); data.inventory.Add(item); }
            item.count = Math.Max(0, item.count + amount);
            if (amount > 0 && !data.discovered.Contains(id)) data.discovered.Add(id);
        }
        public bool Unlocked(int index) { return index >= 0 && index < Recipes.Length && data.served >= Recipes[index].unlock; }
        public bool CanMix(int index)
        {
            if (!Unlocked(index) || data.stage != WorkStage.Empty) return false;
            foreach (string ingredient in Recipes[index].ingredients) if (Count(ingredient) < 1) return false;
            return true;
        }
        public bool Mix(int index)
        {
            if (!CanMix(index)) return false;
            foreach (string ingredient in Recipes[index].ingredients) Add(ingredient, -1);
            data.batchRecipe = index; data.batchSky = data.sky; data.kneads = 0;
            data.stage = WorkStage.Mixing; data.quality = ""; return true;
        }
        public bool Knead()
        {
            if (data.stage != WorkStage.Mixing) return false;
            if (++data.kneads == 3) { data.stage = WorkStage.Proofing; data.proofSeconds = data.batchSky == (int)Sky.Rain ? 12 : 6; }
            return true;
        }
        public void Tick(float dt)
        {
            if (dt <= 0) return;
            if (data.stage == WorkStage.Proofing)
            {
                data.proofSeconds = Math.Max(0, data.proofSeconds - dt);
                if (data.proofSeconds == 0) data.stage = WorkStage.Dough;
            }
            if (data.stage == WorkStage.Baking) data.bakeSeconds += dt;
        }
        public bool PutInOven()
        { if (data.stage != WorkStage.Dough) return false; data.stage = WorkStage.Baking; data.bakeSeconds = 0; return true; }
        public float GoldenStart { get { return Recipes[data.batchRecipe].seconds + (data.batchSky == (int)Sky.Fog ? 1 : 0); } }
        public string TakeOut()
        {
            if (data.stage != WorkStage.Baking) return "";
            data.quality = data.bakeSeconds < GoldenStart ? "Lembut" : data.bakeSeconds <= GoldenStart + 4 ? "Keemasan" : "Terlalu matang";
            data.stage = WorkStage.Ready;
            if (!data.baked.Contains(data.batchRecipe)) data.baked.Add(data.batchRecipe);
            return data.quality;
        }
        public int PreferredRecipe { get { return data.sky == 0 ? 2 : data.sky == 1 ? 7 : 0; } }
        public int Serve(bool aruna)
        {
            if (data.stage != WorkStage.Ready) return 0;
            int reward = 8 + (data.batchRecipe == PreferredRecipe ? 5 : 0) + (data.quality == "Keemasan" ? 4 : 0);
            data.shells += reward; data.served++; data.friendship++;
            if (aruna && data.sky == (int)Sky.Fog && data.batchRecipe == 7 && data.story == 1) data.story = 2;
            data.stage = WorkStage.Empty; return reward;
        }
        public bool TalkAruna()
        {
            if (data.sky == (int)Sky.Fog && data.story == 0) { data.story = 1; return true; }
            return false;
        }
        public string NextGoal
        {
            get
            {
                if (data.story == 0) return "Temui Pak Aruna di mercusuar saat kabut.";
                if (data.story == 1) return "Bawa Roti Rempah Pesisir ke Pak Aruna saat kabut.";
                if (data.baked.Count < 8) return "Cerita Aruna selesai. Coba kedelapan resep di jurnal.";
                if (data.discovered.Count < ItemIds.Length) return "Lengkapi koleksi semua bahan pesisir.";
                return "Bakery menjadi rumah kedua desa. Nikmati harimu!";
            }
        }
    }
}
