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
            new DateVariety("Khalas", "An Emirati favourite. Khalas dates have a golden-brown colour and a soft, caramel-like taste.", new Color(.68f,.29f,.08f)),
            new DateVariety("Fardh", "Fardh dates are known for their dark reddish-brown skin, firm texture and small seed.", new Color(.35f,.13f,.19f)),
            new DateVariety("Khenaizi", "Khenaizi is a date variety grown in the UAE. Its fruit turns deep red as it ripens.", new Color(.43f,.12f,.10f)),
            new DateVariety("Lulu", "Lulu means pearl in Arabic. These small, rounded dates are a familiar UAE variety.", new Color(.48f,.24f,.10f)),
            new DateVariety("Sukkary", "The name Sukkary comes from the Arabic word for sugar. Look for its golden colour and crinkled skin.", new Color(.84f,.47f,.16f)),
            new DateVariety("Ajwa", "Ajwa dates are grown around Madinah in Saudi Arabia. They are small, dark and softly textured.", new Color(.19f,.12f,.21f))
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

