using System.Collections.Generic;
using UnityEngine;

namespace Malatro
{
    [CreateAssetMenu(fileName = "HorseDatabase", menuName = "Malatro/Horse Database")]
    public sealed class HorseDatabase : ScriptableObject
    {
        public const string ResourcePath = "HorseDatabase";

        [Tooltip("List order determines the lane and navigation order.")]
        public List<HorseData> Horses = new List<HorseData>();

        public static HorseDatabase LoadOrCreateRuntimeDefaults()
        {
            var database = Resources.Load<HorseDatabase>(ResourcePath);
            return database != null ? database : CreateRuntimeDefaults();
        }

        public static HorseDatabase CreateRuntimeDefaults()
        {
            var database = CreateInstance<HorseDatabase>();
            var definitions = new[]
            {
                new DefaultHorse("midnight-mint", "Midnight Mint", "미드나이트 민트", "Mint", "민트", "Horses/leaf-run", "late-charge"),
                new DefaultHorse("lucky-stirrup", "Lucky Stirrup", "럭키 스터럽", "Lucky", "럭키", "Horses/purple-run", "wind-step"),
                new DefaultHorse("velvet-thunder", "Velvet Thunder", "벨벳 썬더", "Velvet", "벨벳", "Horses/witch-run", "second-wind"),
                new DefaultHorse("pocket-comet", "Pocket Comet", "포켓 코멧", "Comet", "코멧", "Horses/blue-cat-run", "mana-surge"),
                new DefaultHorse("dust-sonata", "Dust Sonata", "더스트 소나타", "Sonata", "소나타", "Horses/blond-cat-run", "iron-rhythm"),
                new DefaultHorse("iron-clover", "Iron Clover", "아이언 클로버", "Clover", "클로버", "Horses/ram-run", "lightning-start")
            };

            for (var i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                var data = CreateInstance<HorseData>();
                data.Id = definition.Id;
                data.EnglishName = definition.EnglishName;
                data.KoreanName = definition.KoreanName;
                data.EnglishShortName = definition.EnglishShortName;
                data.KoreanShortName = definition.KoreanShortName;
                data.RunSheet = Resources.Load<Texture2D>(definition.ResourcePath);
                data.SkillData = CreateRuntimeSkill(definition.SkillId);
                data.UiColor = Color.HSVToRGB(i / (float)definitions.Length, 0.75f, 0.95f);
                data.OpeningOddsRange = new Vector2(1.8f, 7.5f);
                database.Horses.Add(data);
            }

            return database;
        }

        private readonly struct DefaultHorse
        {
            public readonly string Id;
            public readonly string EnglishName;
            public readonly string KoreanName;
            public readonly string EnglishShortName;
            public readonly string KoreanShortName;
            public readonly string ResourcePath;
            public readonly string SkillId;

            public DefaultHorse(
                string id,
                string englishName,
                string koreanName,
                string englishShortName,
                string koreanShortName,
                string resourcePath,
                string skillId)
            {
                Id = id;
                EnglishName = englishName;
                KoreanName = koreanName;
                EnglishShortName = englishShortName;
                KoreanShortName = koreanShortName;
                ResourcePath = resourcePath;
                SkillId = skillId;
            }
        }

        public static HorseSkillData CreateRuntimeSkill(string id)
        {
            var skill = CreateInstance<HorseSkillData>();
            skill.Id = id;
            switch (id)
            {
                case "lightning-start":
                    SetSkill(skill, "Lightning Start", "번개 출발", 1.5f, 7f, 0f, 0f, new Color(1f, 0.86f, 0.18f));
                    break;
                case "wind-step":
                    SetSkill(skill, "Wind Step", "바람 걸음", 4.2f, 0f, 0f, 0f, new Color(0.2f, 0.9f, 0.88f));
                    break;
                case "second-wind":
                    SetSkill(skill, "Second Wind", "두 번째 숨", 1.2f, 0f, 7f, 0f, new Color(0.3f, 1f, 0.5f));
                    break;
                case "mana-surge":
                    SetSkill(skill, "Mana Surge", "마나 폭주", 0f, 2.5f, 0f, 35f, new Color(0.72f, 0.36f, 1f));
                    break;
                case "iron-rhythm":
                    SetSkill(skill, "Iron Rhythm", "강철 리듬", 0f, 3.5f, 4f, 0f, new Color(1f, 0.52f, 0.2f));
                    break;
                default:
                    SetSkill(skill, "Late Charge", "막판 추입", 2f, 0f, 0f, 0f, new Color(1f, 0.24f, 0.3f));
                    skill.EffectType = HorseSkillEffectType.LateCharge;
                    skill.LateChargeSpeedBoost = 4.5f;
                    break;
            }
            return skill;
        }

        private static void SetSkill(HorseSkillData skill, string english, string korean, float speed, float acceleration, float fatigue, float retainedMana, Color color)
        {
            skill.EnglishName = english;
            skill.KoreanName = korean;
            skill.SpeedBoost = speed;
            skill.AccelerationBoost = acceleration;
            skill.FatigueRecovery = fatigue;
            skill.RetainedMana = retainedMana;
            skill.EffectColor = color;
        }
    }
}
