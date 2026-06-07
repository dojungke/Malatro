using UnityEngine;
using UnityEngine.Serialization;

namespace Malatro
{
    [System.Serializable]
    public struct IntStatRange
    {
        [Min(0)] public int Min;
        [Min(0)] public int Max;

        public IntStatRange(int min, int max)
        {
            Min = min;
            Max = Mathf.Max(min, max);
        }

        public int Roll(System.Random random)
        {
            return random.Next(Min, Mathf.Max(Min, Max) + 1);
        }
    }

    [CreateAssetMenu(fileName = "HorseData", menuName = "Malatro/Horse Data")]
    public sealed class HorseData : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string EnglishName;
        public string KoreanName;
        public string EnglishShortName;
        public string KoreanShortName;

        [Header("Race Stats")]
        public IntStatRange Speed = new IntStatRange(7, 13);
        public IntStatRange Acceleration = new IntStatRange(5, 12);
        public IntStatRange Stamina = new IntStatRange(6, 13);
        public IntStatRange Magic = new IntStatRange(8, 18);
        public Vector2 OpeningOddsRange = new Vector2(1.8f, 7.5f);

        [Header("Visual")]
        public Texture2D RunSheet;
        public Color UiColor = Color.white;

        [Header("Skill")]
        public HorseSkillData SkillData;

        [FormerlySerializedAs("Skill")]
        [SerializeField, HideInInspector] private int legacySkill;

        public int LegacySkill => legacySkill;

        public string GetName(bool korean)
        {
            return korean && !string.IsNullOrWhiteSpace(KoreanName) ? KoreanName : EnglishName;
        }

        public string GetShortName(bool korean)
        {
            var localized = korean ? KoreanShortName : EnglishShortName;
            return string.IsNullOrWhiteSpace(localized) ? GetName(korean) : localized;
        }
    }
}
