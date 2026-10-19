using System;
using UnityEngine;

namespace RainyDayBakery
{
    // A self-contained, resolution-independent prototype. No imported art or scene wiring required.
    public sealed class RainyDayGame : MonoBehaviour
    {
        enum Screen { Menu, Play, Pause, Summary }
        enum Batch { Empty, Ingredient, Dough, Baking, Bread }
        Screen screen;
        Batch batch;
        int weather, recipe, kneads, served, coins, totalCoins, days;
        float bakeTime, ambience;
        string quality = "", message = "";
        Vector2 mira = new Vector2(410, 460), destination;
        int pending = -1;
        bool moving;
        Texture2D pixel;
        GUIStyle text, title, small, button;
        readonly string[] skies = { "RAIN", "FOG", "SUN" };
        readonly string[] ingredients = { "rain berries", "wild rosemary", "sun lemons" };
        readonly string[] recipes = { "Berry cloud bun", "Rosemary hearth loaf", "Lemon morning roll" };
        readonly string[] hints = { "Rain brings sweet berries to the garden.", "Fog wakes the rosemary near the sea.", "Sunshine ripens the little lemon tree." };
        readonly Vector2[] stops = { new Vector2(730, 505), new Vector2(210, 365), new Vector2(405, 365), new Vector2(568, 495), new Vector2(735, 330) };
        readonly Color ink = Hex("343e42"), cream = Hex("fff3d7"), teal = Hex("386d69"), peach = Hex("e9ad84");
        int Order { get { return served % 3; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindObjectOfType<RainyDayGame>() == null)
                new GameObject("Rainy Day Bakery").AddComponent<RainyDayGame>();
        }
        void Awake()
        {
            Application.targetFrameRate = 60;
            totalCoins = PlayerPrefs.GetInt("RainyBakery.Coins", 0);
            days = PlayerPrefs.GetInt("RainyBakery.Days", 0);
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            if (Camera.main == null) new GameObject("Main Camera", typeof(Camera)).tag = "MainCamera";
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = ink;
        }
        void OnDestroy() { if (pixel != null) Destroy(pixel); }
        void StartDay()
        {
            batch = Batch.Empty; weather = 0; served = coins = kneads = 0;
            bakeTime = 0; quality = ""; moving = false; pending = -1;
            mira = new Vector2(410, 460); screen = Screen.Play;
            message = "Nami: Welcome, Mira! Pak Aruna would love a berry bun. Visit the garden to begin.";
        }
        void Update()
        {
            ambience += Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (screen == Screen.Play) screen = Screen.Pause;
                else if (screen == Screen.Pause) screen = Screen.Play;
            }
            if (screen != Screen.Play) return;
            if (batch == Batch.Baking) bakeTime += Time.deltaTime;
            Vector2 input = new Vector2((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0) - (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0));
            if (input.sqrMagnitude > 0)
            {
                moving = false; pending = -1; mira += input.normalized * 210 * Time.deltaTime;
            }
            else if (moving)
            {
                mira = Vector2.MoveTowards(mira, destination, 260 * Time.deltaTime);
                if (Vector2.Distance(mira, destination) < 2) { moving = false; int action = pending; pending = -1; Interact(action); }
            }
            mira.x = Mathf.Clamp(mira.x, 100, 800); mira.y = Mathf.Clamp(mira.y, 310, 565);
            if (Input.GetKeyDown(KeyCode.E))
            {
                int nearest = 0;
                for (int i = 1; i < stops.Length; i++)
                    if (Vector2.Distance(mira, stops[i]) < Vector2.Distance(mira, stops[nearest])) nearest = i;
                if (Vector2.Distance(mira, stops[nearest]) < 100) Interact(nearest);
                else message = "Walk closer to a station, or click its button to walk there.";
            }
        }
        void Visit(int station) { destination = stops[station]; pending = station; moving = true; }
        void Interact(int station)
        {
            if (station == 0)
            {
                if (batch != Batch.Empty) { message = "Finish the batch in your basket first. Nothing goes to waste here."; return; }
                recipe = weather; batch = Batch.Ingredient; kneads = 0;
                message = "Gathered " + ingredients[recipe] + ". Flour is on the house! Take them to the mixing table.";
            }
            else if (station == 1)
            {
                if (batch != Batch.Ingredient) { message = "Gather an ingredient in the garden first, then knead it here."; return; }
                kneads++;
                if (kneads >= 3) { batch = Batch.Dough; message = "Lovely dough! Take your " + recipes[recipe].ToLowerInvariant() + " to the oven."; }
                else message = "Fold, press, breathe. Knead " + kneads + "/3. Click the table again.";
            }
            else if (station == 2)
            {
                if (batch == Batch.Dough) { batch = Batch.Baking; bakeTime = 0; message = "The oven is warm. Take the bread out in the golden window (6–10 seconds)."; }
                else if (batch == Batch.Baking)
                {
                    quality = BakeryRules.Quality(bakeTime); batch = Batch.Bread;
                    message = quality + " and made with love. Serve your bread at the counter.";
                }
                else message = "The oven needs kneaded dough. Gather, then knead at the mixing table.";
            }
            else if (station == 3)
            {
                if (batch != Batch.Bread) { message = "Pak Aruna: I'll enjoy the view while you bake. No rush!"; return; }
                int reward = BakeryRules.Reward(recipe, Order, quality);
                bool match = recipe == Order;
                coins += reward; served++; batch = Batch.Empty;
                message = "Pak Aruna: " + (match ? "Just what I hoped for!" : "A lovely surprise. Thank you!") + " +" + reward + " shells.";
                if (served == 3)
                {
                    totalCoins += coins; days++;
                    PlayerPrefs.SetInt("RainyBakery.Coins", totalCoins); PlayerPrefs.SetInt("RainyBakery.Days", days); PlayerPrefs.Save();
                    screen = Screen.Summary;
                }
                else message += " Next request: " + recipes[Order] + ". Try " + skies[Order].ToLowerInvariant() + " at the weather dial.";
            }
            else if (station == 4) message = "Nami: " + hints[weather] + " Aruna wants " + recipes[Order].ToLowerInvariant() + ". Change the sky any time; your gathered ingredient stays the same.";
        }
        static Color Hex(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }
        void Styles()
        {
            if (text != null) return;
            text = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
            text.normal.textColor = ink;
            small = new GUIStyle(text) { fontSize = 14 };
            title = new GUIStyle(text) { fontSize = 42, fontStyle = FontStyle.Bold };
            button = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, fontSize = 17, fontStyle = FontStyle.Bold };
        }
        void Box(float x, float y, float w, float h, Color color)
        { GUI.color = color; GUI.DrawTexture(new Rect(x, y, w, h), pixel); GUI.color = Color.white; }
        void Label(string value, float x, float y, float w, float h, GUIStyle style = null)
        { GUI.Label(new Rect(x, y, w, h), value, style ?? text); }
        bool Button(string value, float x, float y, float w, float h, bool active = false)
        {
            Rect rect = new Rect(x, y, w, h);
            bool hover = rect.Contains(Event.current.mousePosition);
            Box(x, y + 3, w, h, Hex("294e4c"));
            Box(x, y, w, h, active ? peach : hover ? Hex("e6d6ac") : cream);
            return GUI.Button(rect, value, button);
        }
        void OnGUI()
        {
            Styles();
            float scale = Mathf.Min(UnityEngine.Screen.width / 1280f, UnityEngine.Screen.height / 800f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((UnityEngine.Screen.width - 1280 * scale) / 2, (UnityEngine.Screen.height - 800 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            Box(0, 0, 1280, 800, Hex("e9dfc7"));
            DrawWorld();
            if (screen == Screen.Menu) { DrawMenu(); return; }
            DrawHUD();
            if (screen == Screen.Pause || screen == Screen.Summary) DrawModal();
        }
        void DrawWorld()
        {
            Color sky = weather == 2 ? Hex("b9d9cc") : weather == 1 ? Hex("b8c7c5") : Hex("99b6be");
            Box(0, 0, 890, 800, sky);
            // Coastal horizon, ripples, and distant islands.
            Box(0, 145, 890, 135, Hex("719e9c"));
            for (int i = 0; i < 20; i++) Box((i * 123 + ambience * 8) % 890, 161 + i % 5 * 22, 34, 2, Hex("b2cfbf"));
            Box(605, 123, 148, 25, Hex("76968e")); Box(658, 72, 26, 61, cream); Box(653, 64, 36, 12, teal); Box(665, 81, 12, 15, peach);
            Box(40, 256, 810, 357, Hex("a7b58d"));
            Box(85, 307, 735, 270, Hex("d9c8a7"));
            for (int i = 0; i < 9; i++) Box(95, 325 + i * 29, 710, 1, Hex("cbb996"));
            // Open-front cottage bakery.
            Box(102, 170, 500, 151, Hex("f2d5a3"));
            Box(85, 160, 535, 30, Hex("9b6154")); Box(103, 140, 500, 20, Hex("b27661"));
            Box(122, 205, 105, 75, teal); Box(131, 212, 87, 60, sky); Box(172, 210, 6, 64, cream); Box(128, 238, 94, 5, cream);
            Box(477, 205, 105, 75, teal); Box(486, 212, 87, 60, sky); Box(527, 210, 6, 64, cream); Box(483, 238, 94, 5, cream);
            Label("RAINY DAY", 253, 200, 225, 42, new GUIStyle(title) { fontSize = 30 });
            Label("B A K E R Y", 270, 243, 200, 28, small);
            // Mixing table and oven.
            Box(147, 307, 124, 40, Hex("a97954")); Box(155, 347, 10, 28, teal); Box(250, 347, 10, 28, teal);
            Box(183, 299, 53, 20, cream); Box(192, 293, 35, 9, peach);
            Box(353, 287, 106, 68, Hex("7b7770")); Box(369, 303, 74, 39, ink);
            Box(382, 317, 48, 19, batch == Batch.Baking ? Hex("eeaf62") : Hex("b27854"));
            // Counter, garden, weather marker.
            Box(498, 437, 132, 38, Hex("9c7056")); Box(493, 428, 142, 12, cream);
            Bread(533, 415); Bread(578, 415);
            Box(668, 405, 130, 64, Hex("88694e"));
            for (int i = 0; i < 5; i++)
            {
                Box(679 + i * 23, 391 + i % 2 * 8, 14, 49, Hex("65876a"));
                Box(675 + i * 23, 383 + i % 2 * 8, 23, 18, weather == 0 ? Hex("bd7180") : weather == 1 ? Hex("b5b995") : Hex("efcf75"));
            }
            Person(735, 305, Hex("90a6a2"), "Nami");
            Person(564, 552, Hex("d29e6f"), "Pak Aruna");
            Person(mira.x, mira.y, Hex("b77767"), "Mira");
            if (batch != Batch.Empty) { Box(mira.x + 17, mira.y - 28, 24, 17, cream); if (batch == Batch.Bread) Bread(mira.x + 19, mira.y - 34); }
            if (weather == 0)
                for (int i = 0; i < 65; i++) Box((i * 137) % 875, (i * 71 + ambience * 190) % 610, 1, 11, new Color(.83f, .94f, .95f, .45f));
            if (weather == 1)
                for (int i = 0; i < 6; i++) Box((i * 172 + ambience * 13) % 1000 - 130, 150 + i * 69, 260, 30, new Color(.92f, .95f, .91f, .19f));
            if (screen == Screen.Play)
            {
                if (Button("Gather", 675, 470, 120, 30)) Visit(0);
                if (Button(batch == Batch.Ingredient ? "Knead " + kneads + "/3" : "Mix dough", 150, 371, 123, 30)) Visit(1);
                if (Button(batch == Batch.Baking ? "Take out" : "Oven", 347, 371, 120, 30)) Visit(2);
                if (Button("Serve", 504, 480, 120, 30)) Visit(3);
                if (Button("Ask Nami", 677, 338, 120, 30)) Visit(4);
            }
            Box(30, 628, 830, 141, cream);
            Label("A LITTLE COASTAL KINDNESS", 52, 644, 780, 24, small);
            Label(screen == Screen.Menu ? "A warm oven. A changeable sky. A place to belong." : message, 52, 678, 780, 80);
        }
        void Bread(float x, float y)
        { Box(x, y + 4, 29, 14, Hex("cb8d50")); Box(x + 4, y, 21, 17, Hex("e9b76a")); Box(x + 8, y + 3, 3, 7, cream); Box(x + 17, y + 3, 3, 7, cream); }
        void Person(float x, float y, Color coat, string name)
        {
            Box(x - 17, y + 4, 35, 7, new Color(0, 0, 0, .13f));
            Box(x - 10, y - 21, 21, 26, coat); Box(x - 7, y - 17, 14, 17, cream);
            Box(x - 10, y - 43, 21, 23, Hex("e7ba92")); Box(x - 12, y - 46, 25, 9, Hex("685548"));
            Box(x - 5, y - 34, 3, 3, ink); Box(x + 4, y - 34, 3, 3, ink);
            Label(name, x - 43, y + 11, 90, 24, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }
        void DrawMenu()
        {
            Box(890, 0, 390, 800, teal);
            Box(923, 64, 290, 3, peach);
            GUI.contentColor = cream;
            var whiteTitle = new GUIStyle(title); whiteTitle.normal.textColor = cream;
            var whiteText = new GUIStyle(text); whiteText.normal.textColor = cream;
            Label("Rainy Day\nBakery", 924, 107, 340, 130, whiteTitle);
            Label("Small joys, fresh from the oven.", 926, 260, 300, 65, whiteText);
            Label("Become Mira. Change the sky, gather something good, and bake for your seaside neighbors.", 926, 349, 300, 120, whiteText);
            GUI.contentColor = Color.white;
            if (Button(days > 0 ? "Open another day" : "Open the bakery", 925, 515, 300, 55)) StartDay();
            Label("3 orders • no rush • no game over", 925, 594, 310, 30, new GUIStyle(small) { normal = { textColor = cream } });
            Label("Days shared: " + days + "\nShells earned: " + totalCoins, 925, 662, 300, 65, whiteText);
        }
        void DrawHUD()
        {
            Box(890, 0, 390, 800, cream);
            Label("DAY " + (days + 1).ToString("00"), 920, 25, 240, 26, small);
            Label("The slow life", 918, 57, 330, 55, new GUIStyle(title) { fontSize = 34 });
            Label(served + " / 3 orders shared     " + coins + " shells", 920, 118, 330, 30);
            Box(920, 163, 330, 2, peach);
            Label("01 / CHOOSE THE SKY", 920, 186, 330, 25, small);
            for (int i = 0; i < 3; i++)
                if (Button(skies[i], 920 + i * 113, 223, 104, 39, weather == i) && screen == Screen.Play)
                { weather = i; message = "Nami: " + hints[weather]; }
            Label(hints[weather], 920, 280, 330, 55, small);
            Label("02 / TODAY'S REQUEST", 920, 352, 330, 25, small);
            Label(recipes[Order], 920, 387, 330, 60, new GUIStyle(text) { fontSize = 25, fontStyle = FontStyle.Bold });
            Label("Pak Aruna • lighthouse keeper", 920, 450, 330, 25, small);
            Box(920, 490, 330, 2, peach);
            Label("03 / YOUR BASKET", 920, 512, 330, 25, small);
            string status = batch == Batch.Empty ? "Empty — visit the garden" : batch == Batch.Ingredient ? ingredients[recipe] + " • knead " + kneads + "/3" : batch == Batch.Dough ? "Dough ready for the oven" : batch == Batch.Baking ? "Baking • " + bakeTime.ToString("0.0") + " seconds" : quality + " " + recipes[recipe];
            Label(status, 920, 548, 330, 62);
            if (batch == Batch.Baking)
            {
                Box(920, 615, 330, 15, Hex("d8c6a8")); Box(1052, 615, 88, 15, Hex("91b89b"));
                Box(920 + Mathf.Clamp01(bakeTime / 15) * 326, 610, 4, 25, ink);
                Label("Soft          Golden (6–10s)          Toasty", 920, 642, 330, 30, small);
            }
            else Label("Gather → knead ×3 → bake → serve", 920, 622, 330, 40, small);
            Label("Click stations to walk & interact.\nOr WASD / arrows + E. Esc pauses.", 920, 698, 335, 52, small);
            if (screen == Screen.Play && Button("II", 808, 26, 49, 38)) screen = Screen.Pause;
        }
        void DrawModal()
        {
            Box(0, 0, 1280, 800, new Color(.12f, .20f, .21f, .72f));
            Box(355, 183, 570, 435, cream);
            bool summary = screen == Screen.Summary;
            Label(summary ? "A day well baked." : "A quiet moment.", 393, 223, 500, 65, title);
            Label(summary ? "Three warm gifts for the village.\nYou earned " + coins + " shells today.\nYour " + totalCoins + " lifetime shells are saved." : "The oven can wait.\nTake all the time you need.", 395, 315, 485, 125);
            if (Button(summary ? "Bake another day" : "Back to the bakery", 395, 468, 490, 48))
            { if (summary) StartDay(); else screen = Screen.Play; }
            if (Button(summary ? "Main menu" : "End day (discard this day)", 395, 539, 490, 40)) { screen = Screen.Menu; moving = false; }
        }
    }
}
