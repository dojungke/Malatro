using UnityEngine;

namespace Malatro
{
    public enum RelicRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum RelicEffectType
    {
        ExtraTicket,
        WinPayoutBonus,
        ExactaPayoutBonus,
        HighestOddsSpeed,
        ProphetReward
    }

    [CreateAssetMenu(fileName = "RelicData", menuName = "Malatro/Relic Data")]
    public sealed class RelicData : ScriptableObject
    {
        public string Id;
        public string EnglishName;
        public string KoreanName;
        [TextArea] public string EnglishDescription;
        [TextArea] public string KoreanDescription;
        public RelicRarity Rarity;
        public RelicEffectType EffectType;
        [Min(0)] public int Price;
        public Color Color = Color.white;
        public Sprite Icon;

        public int SellPrice => Mathf.Max(1, Price / 2);

        public string GetName(bool korean)
        {
            return korean && !string.IsNullOrWhiteSpace(KoreanName) ? KoreanName : EnglishName;
        }

        public string GetDescription(bool korean)
        {
            return korean && !string.IsNullOrWhiteSpace(KoreanDescription) ? KoreanDescription : EnglishDescription;
        }
    }
}
