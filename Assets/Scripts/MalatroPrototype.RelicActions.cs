namespace Malatro
{
    public sealed partial class MalatroPrototype
    {
        private void BuyRelic(RelicData relic)
        {
            if (relic == null || relicInventory.Contains(relic))
            {
                return;
            }

            if (relicInventory.IsFull)
            {
                SetLog("relic_full");
                return;
            }

            if (gold < relic.Price)
            {
                SetLog("relic_need_gold", relic.Price);
                return;
            }

            gold -= relic.Price;
            relicInventory.Add(relic);
            EnsureTicketCount();
            SetLog("relic_bought", relic.GetName(language == UiLanguage.Korean));
        }

        private void SellRelic(RelicData relic)
        {
            if (!relicInventory.Remove(relic))
            {
                return;
            }

            gold += relic.SellPrice;
            EnsureTicketCount();
            SetLog("relic_sold", relic.GetName(language == UiLanguage.Korean), relic.SellPrice);
        }

        private string GetRarityName(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => L("rarity_common"),
                RelicRarity.Rare => L("rarity_rare"),
                RelicRarity.Epic => L("rarity_epic"),
                RelicRarity.Legendary => L("rarity_legendary"),
                _ => rarity.ToString()
            };
        }
    }
}
