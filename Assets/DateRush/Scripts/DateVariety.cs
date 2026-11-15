using System;
using UnityEngine;

namespace DateRush
{
    [Serializable]
    public sealed class DateVariety
    {
        public string name;
        public string description;
        public Color color;
        public DateVariety(string name, string description, Color color)
        { this.name = name; this.description = description; this.color = color; }
    }

    public static class DateCatalog
    {
        public static readonly DateVariety[] All = {
            new DateVariety("Khalas", "A prized Al-Ahsa cultivar, Khalas became a signature Gulf market date and is widely grown in UAE oases.", new Color(.68f,.29f,.08f)),
            new DateVariety("Fardh", "A major Abu Dhabi cultivar, especially in Al Ain, Fardh is valued as a commercial crop and eaten fresh or dried.", new Color(.35f,.13f,.19f)),
            new DateVariety("Khenaizi", "Khenaizi remains part of the UAE's oasis harvest and is celebrated among traditional varieties at local date festivals.", new Color(.43f,.12f,.10f)),
            new DateVariety("Lulu", "Lulu is grown in Al Ain's historic oases, where date palms have supported food, livelihoods and community life for generations.", new Color(.48f,.24f,.10f)),
            new DateVariety("Sukkary", "From Saudi Arabia's Qassim region, Sukkary is a premium variety grown across the Kingdom and enjoyed fresh or fully ripe.", new Color(.84f,.47f,.16f)),
            new DateVariety("Ajwa", "Cultivated around Medina, Ajwa is one of Saudi Arabia's most historic varieties and a treasured gift for visitors.", new Color(.19f,.12f,.21f))
        };
    }

    // Pure scoring rules, shared by gameplay and editor verification.
    public static class CatchRules
    {
        public const int Goal = 15;
        public const float Duration = 90f;
        public static int Points(bool match, int combo) => match ? 100 + Mathf.Min(Mathf.Max(combo - 1, 0), 4) * 25 : -25;
        public static bool CrossedMovingBasket(Vector2 previous, Vector2 current, Vector2 oldBasket, Vector2 basket)
        {
            float before=previous.y-oldBasket.y, after=current.y-basket.y;
            if(before>=0 || after<0 || after<=before)return false;
            float t=-before/(after-before);
            return Mathf.Abs(Mathf.Lerp(previous.x,current.x,t)-Mathf.Lerp(oldBasket.x,basket.x,t))<=70;
        }
        public static bool CrossedBasket(float previousY, float currentY, float x, float basketX)
            => previousY < 715 && currentY >= 715 && Mathf.Abs(x - basketX) <= 74;
    }
}

