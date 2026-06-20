using System.Collections.Generic;
using UnityEngine;

namespace Malatro
{
    [CreateAssetMenu(fileName = "RelicDatabase", menuName = "Malatro/Relic Database")]
    public sealed class RelicDatabase : ScriptableObject
    {
        public const string ResourcePath = "RelicDatabase";

        public List<RelicData> Relics = new List<RelicData>();

        public static RelicDatabase LoadOrCreateRuntimeDefaults()
        {
            var database = Resources.Load<RelicDatabase>(ResourcePath);
            return database != null ? database : CreateRuntimeDefaults();
        }

        public static RelicDatabase CreateRuntimeDefaults()
        {
            var database = CreateInstance<RelicDatabase>();
            database.Relics.Add(CreateRelic(
                "blank-ticket", "Blank Ticket", "빈 마권",
                "Increase ticket count by 1.", "마권 수량이 1개 증가합니다.",
                RelicRarity.Rare, RelicEffectType.ExtraTicket, 50, new Color(0.28f, 0.72f, 1f)));
            database.Relics.Add(CreateRelic(
                "winner-takes-all", "Winner Takes All", "승자 독식",
                "Win ticket payout is increased by 50%.", "단승식 획득 골드가 50% 증가합니다.",
                RelicRarity.Common, RelicEffectType.WinPayoutBonus, 30, new Color(0.72f, 0.76f, 0.72f)));
            database.Relics.Add(CreateRelic(
                "two-horse-carriage", "Two-Horse Carriage", "쌍두마차",
                "Exacta ticket payout is increased by 50%.", "쌍승식 획득 골드가 50% 증가합니다.",
                RelicRarity.Common, RelicEffectType.ExactaPayoutBonus, 30, new Color(0.72f, 0.76f, 0.72f)));
            database.Relics.Add(CreateRelic(
                "proof", "Proof", "증명",
                "The horse with the highest odds gains 5 Speed.", "배율이 가장 높은 말의 속도가 5 증가합니다.",
                RelicRarity.Epic, RelicEffectType.HighestOddsSpeed, 80, new Color(0.78f, 0.36f, 1f)));
            database.Relics.Add(CreateRelic(
                "prophet", "Prophet", "예언가",
                "Reward gold multiplier increases with successful tickets: 2x, 3x, 4x, and so on.", "적중한 마권 개수에 따라 보상 골드가 2배, 3배, 4배로 증가합니다.",
                RelicRarity.Legendary, RelicEffectType.ProphetReward, 120, new Color(1f, 0.68f, 0.16f)));
            database.Relics.Add(CreateRelic(
                "top-gun", "Top Gun", "탑건",
                "Reward gold multiplier increases with ticket types used: 2x, 3x, 4x, and so on.", "사용한 마권 종류 수에 따라 보상 골드가 2배, 3배, 4배로 증가합니다.",
                RelicRarity.Legendary, RelicEffectType.TicketTypeVarietyReward, 120, new Color(0.2f, 0.82f, 0.94f)));
            database.Relics.Add(CreateRelic(
                "bronze-medal", "Bronze Medal", "동메달",
                "Place ticket base gold is increased by 30.", "연승식 기본 골드가 30 증가합니다.",
                RelicRarity.Common, RelicEffectType.PlaceBaseGoldBonus, 30, new Color(0.72f, 0.42f, 0.2f)));
            database.Relics.Add(CreateRelic(
                "gold-medal", "Gold Medal", "금메달",
                "Win ticket base gold is increased by 30.", "단승식 기본 골드가 30 증가합니다.",
                RelicRarity.Common, RelicEffectType.WinBaseGoldBonus, 30, new Color(1f, 0.78f, 0.18f)));
            database.Relics.Add(CreateRelic(
                "silver-medal", "Silver Medal", "은메달",
                "Exacta and Quinella ticket base gold is increased by 30.", "쌍승식과 복승식 기본 골드가 30 증가합니다.",
                RelicRarity.Common, RelicEffectType.CombinationBaseGoldBonus, 30, new Color(0.75f, 0.82f, 0.88f)));
            database.Relics.Add(CreateRelic(
                "mana-disruptor", "Mana Disruptor", "마나 교란 장치",
                "Horses with the Mage tag lose 3 Speed.", "마법사 태그를 가진 말의 속도가 3 감소합니다.",
                RelicRarity.Rare, RelicEffectType.MageTagSpeedPenalty, 50, new Color(0.36f, 0.46f, 0.96f)));
            database.Relics.Add(CreateRelic(
                "rusty-dagger", "Rusty Dagger", "녹슨 단검",
                "Horses with the Assassin tag lose 3 Acceleration.", "암살자 태그를 가진 말의 가속도가 3 감소합니다.",
                RelicRarity.Rare, RelicEffectType.AssassinTagAccelerationPenalty, 50, new Color(0.7f, 0.32f, 0.18f)));
            database.Relics.Add(CreateRelic(
                "broken-sword", "Broken Sword", "부러진 검",
                "Horses with the Knight tag lose 3 Speed.", "기사 태그를 가진 말의 속도가 3 감소합니다.",
                RelicRarity.Rare, RelicEffectType.KnightTagSpeedPenalty, 50, new Color(0.55f, 0.58f, 0.62f)));
            database.Relics.Add(CreateRelic(
                "small-mana-stone", "Small Mana Stone", "작은 마석",
                "Horses with the Mage tag gain 2 Magic.", "마법사 태그를 가진 말의 마력이 2 증가합니다.",
                RelicRarity.Common, RelicEffectType.MageTagMagicBonus, 30, new Color(0.28f, 0.72f, 1f)));
            database.Relics.Add(CreateRelic(
                "subsidy", "Subsidy", "지원금",
                "Horses with the Kingdom tag gain 2 Stamina.", "왕국인 태그를 가진 말의 스테미나가 2 증가합니다.",
                RelicRarity.Common, RelicEffectType.KingdomTagStaminaBonus, 30, new Color(0.46f, 0.78f, 0.36f)));
            return database;
        }

        private static RelicData CreateRelic(
            string id,
            string englishName,
            string koreanName,
            string englishDescription,
            string koreanDescription,
            RelicRarity rarity,
            RelicEffectType effectType,
            int price,
            Color color)
        {
            var relic = CreateInstance<RelicData>();
            relic.Id = id;
            relic.EnglishName = englishName;
            relic.KoreanName = koreanName;
            relic.EnglishDescription = englishDescription;
            relic.KoreanDescription = koreanDescription;
            relic.Rarity = rarity;
            relic.EffectType = effectType;
            relic.Price = price;
            relic.Color = color;
            return relic;
        }
    }
}
