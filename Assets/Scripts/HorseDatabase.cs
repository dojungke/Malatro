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
                data.Tags = new List<string>();
                data.TurfAptitude = TrackAptitudeGrade.C;
                data.DirtAptitude = TrackAptitudeGrade.C;
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
                    SetSkill(skill, "Black Goat Slash", "흑염소참", 0f, 0f, 0f, 0f, new Color(0.38f, 0.12f, 0.48f));
                    skill.EnglishDescription = "Stun all opponents within 50 meters ahead and behind for 1 second. Activates only when a target is nearby.";
                    skill.KoreanDescription = "전후방 50미터 이내의 모든 상대를 1초 동안 기절시킵니다. 범위 안에 말이 있을 때만 발동합니다.";
                    skill.EffectType = HorseSkillEffectType.AreaStun;
                    skill.AreaRadiusMeters = 50f;
                    skill.StunDuration = 1f;
                    break;
                case "wind-step":
                    SetSkill(skill, "Knight's Strike", "기사의 일격", 0f, 0f, 0f, 0f, new Color(0.95f, 0.82f, 0.25f));
                    skill.EnglishDescription = "Charge 50 meters and stun horses in the path for 0.5 seconds.";
                    skill.KoreanDescription = "50미터 돌진하며 경로상의 말을 0.5초 동안 기절시킵니다.";
                    skill.EffectType = HorseSkillEffectType.KnightStrike;
                    skill.ChargeDistanceMeters = 50f;
                    skill.StunDuration = 0.5f;
                    break;
                case "second-wind":
                    SetSkill(skill, "Sniper", "저격", 0f, 0f, 0f, 0f, new Color(0.92f, 0.2f, 0.28f));
                    skill.EnglishDescription = "Stun the leader for 1 second. Does not activate while leading.";
                    skill.KoreanDescription = "선두 말을 1초 동안 기절시킵니다. 자신이 선두일 때는 사용하지 않습니다.";
                    skill.EffectType = HorseSkillEffectType.Sniper;
                    skill.StunDuration = 1f;
                    break;
                case "mana-surge":
                    SetSkill(skill, "Transfer", "환승", 0f, 0f, 0f, 0f, new Color(0.18f, 0.82f, 1f));
                    skill.EnglishDescription = "Swap positions with the nearest horse ahead. Does not activate while leading.";
                    skill.KoreanDescription = "자신 앞의 가장 가까운 말과 위치를 바꿉니다. 앞에 말이 없으면 사용하지 않습니다.";
                    skill.EffectType = HorseSkillEffectType.Transfer;
                    break;
                case "iron-rhythm":
                    SetSkill(skill, "My World", "내 세계", 0f, 0f, 0f, 0f, new Color(0.48f, 0.34f, 1f));
                    skill.EnglishDescription = "Stop time for everything except yourself for 5 seconds.";
                    skill.KoreanDescription = "자신을 제외한 모든 것의 시간을 5초 동안 멈춥니다.";
                    skill.EffectType = HorseSkillEffectType.TimeStop;
                    skill.EffectDuration = 5f;
                    skill.TimeStopDuration = 5f;
                    break;
                case "howl":
                    SetSkill(skill, "Howl", "울부짖기", 0f, 0f, 0f, 0f, new Color(0.5f, 0.72f, 1f));
                    skill.EnglishDescription = "Reduce the speed of other horses within 100 meters by 30% for 3 seconds.";
                    skill.KoreanDescription = "반경 100미터 내의 다른 말의 속도를 3초 동안 30% 감소시킵니다.";
                    skill.EffectType = HorseSkillEffectType.AreaSlow;
                    skill.EffectDuration = 1.4f;
                    skill.AreaRadiusMeters = 100f;
                    skill.SpeedReductionRatio = 0.3f;
                    skill.DebuffDuration = 3f;
                    break;
                case "leap":
                    SetSkill(skill, "Leap", "튀어오르기", 0f, 0f, 0f, 0f, new Color(0.2f, 0.85f, 0.9f));
                    skill.EnglishDescription = "Leap 100 meters forward.";
                    skill.KoreanDescription = "100미터 앞으로 튀어오릅니다.";
                    skill.EffectType = HorseSkillEffectType.Leap;
                    skill.EffectDuration = 1.1f;
                    skill.ChargeDistanceMeters = 100f;
                    break;
                case "trip-up":
                    SetSkill(skill, "Trip Up", "넘어트리기", 0f, 0f, 0f, 0f, new Color(0.58f, 0.5f, 0.42f));
                    skill.EnglishDescription = "When mana is full, stun a horse that overtakes this horse for 1 second.";
                    skill.KoreanDescription = "마나가 가득 찼을 때 이 말을 추월하는 말을 1초 동안 기절시킵니다.";
                    skill.EffectType = HorseSkillEffectType.OvertakeTrip;
                    skill.EffectDuration = 1.1f;
                    skill.StunDuration = 1f;
                    break;
                case "late-kick":
                    SetSkill(skill, "Late Kick", "추입", 0f, 0f, 0f, 0f, new Color(0.3f, 0.72f, 1f));
                    skill.EnglishDescription = "Available in the final 25% of the race. Gain Speed equal to 50% of Magic.";
                    skill.KoreanDescription = "결승점 전 마지막 25% 구간부터 사용할 수 있으며, 마력의 50%만큼 속도가 상승합니다.";
                    skill.EffectType = HorseSkillEffectType.LateCharge;
                    skill.LateChargeThreshold = 0.75f;
                    skill.MagicSpeedRatio = 0.5f;
                    skill.Cooldown = 999f;
                    break;
                default:
                    SetSkill(skill, "Star Ladder", "별사다리", 4f, 4f, 0f, 0f, new Color(1f, 0.35f, 0.72f));
                    skill.EnglishDescription = "Gain 4 Speed and Acceleration for 3 seconds. The effect increases by 25% for each horse ahead.";
                    skill.KoreanDescription = "3초 동안 속도와 가속도가 4 증가하며, 앞에 있는 말 한 마리당 효과가 25% 증가합니다.";
                    skill.EffectType = HorseSkillEffectType.StarStair;
                    skill.EffectDuration = 3f;
                    skill.BonusPerHorseAhead = 0.25f;
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
