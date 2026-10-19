namespace RainyDayBakery
{
    public static class BakeryRules
    {
        public const float ReadySeconds = 6f;
        public const float ToastySeconds = 10f;
        public static string Quality(float seconds)
        {
            return seconds < ReadySeconds ? "Soft" : seconds <= ToastySeconds ? "Golden" : "Toasty";
        }
        public static int Reward(int recipe, int order, string quality)
        {
            return 8 + (recipe == order ? 5 : 0) + (quality == "Golden" ? 4 : 0);
        }
    }
}
