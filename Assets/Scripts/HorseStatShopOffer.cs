using System;

namespace Malatro
{
    public enum HorseStatType
    {
        Speed,
        Acceleration,
        Stamina,
        Magic
    }

    [Serializable]
    public sealed class HorseStatShopOffer
    {
        public HorseStatType Stat;
        public RelicRarity Rarity;
        public int Amount;
        public int Price;
        public bool RequiresHorseSelection;
        public Horse RandomTarget;
        public bool Purchased;
    }

    [Serializable]
    public sealed class ShopOfferEntry
    {
        public RelicData Relic;
        public HorseStatShopOffer StatOffer;

        public bool IsRelic => Relic != null;
    }
}
