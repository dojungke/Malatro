using System.Collections.Generic;
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

    public enum TrackAptitudeGrade
    {
        S,
        A,
        B,
        C,
        D,
        E,
        F
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
        public List<string> Tags = new List<string>();

        [Header("Race Stats")]
        public IntStatRange Speed = new IntStatRange(7, 13);
        public IntStatRange Acceleration = new IntStatRange(5, 12);
        public IntStatRange Stamina = new IntStatRange(6, 13);
        public IntStatRange Magic = new IntStatRange(8, 18);
        public TrackAptitudeGrade TurfAptitude = TrackAptitudeGrade.C;
        public TrackAptitudeGrade DirtAptitude = TrackAptitudeGrade.C;
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

        public TrackAptitudeGrade GetAptitude(TrackSurface surface)
        {
            return surface == TrackSurface.Dirt ? DirtAptitude : TurfAptitude;
        }

        public float GetAptitudeMultiplier(TrackSurface surface)
        {
            return GetAptitudeMultiplier(GetAptitude(surface));
        }

        public static float GetAptitudeMultiplier(TrackAptitudeGrade grade)
        {
            return grade switch
            {
                TrackAptitudeGrade.S => 1.3f,
                TrackAptitudeGrade.A => 1.2f,
                TrackAptitudeGrade.B => 1.1f,
                TrackAptitudeGrade.C => 1f,
                TrackAptitudeGrade.D => 0.9f,
                TrackAptitudeGrade.E => 0.8f,
                TrackAptitudeGrade.F => 0.7f,
                _ => 1f
            };
        }
    }
}
