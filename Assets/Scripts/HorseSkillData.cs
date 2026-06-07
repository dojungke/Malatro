using UnityEngine;

namespace Malatro
{
    public enum HorseSkillEffectType
    {
        StandardBoost,
        LateCharge
    }

    [CreateAssetMenu(fileName = "HorseSkillData", menuName = "Malatro/Horse Skill Data")]
    public sealed class HorseSkillData : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string EnglishName;
        public string KoreanName;
        [TextArea] public string EnglishDescription;
        [TextArea] public string KoreanDescription;

        [Header("Activation")]
        [Min(1f)] public float ManaCost = 100f;
        [Min(0f)] public float Cooldown = 1.2f;
        [Min(0.1f)] public float EffectDuration = 1.1f;
        [Range(0f, 100f)] public float RetainedMana;

        [Header("Effects")]
        public HorseSkillEffectType EffectType;
        public float SpeedBoost;
        public float AccelerationBoost;
        public float FatigueRecovery;
        [Range(0f, 1f)] public float LateChargeThreshold = 0.62f;
        public float LateChargeSpeedBoost;

        [Header("Visual")]
        public Color EffectColor = Color.white;
        public Sprite Icon;

        public string GetName(bool korean)
        {
            return korean && !string.IsNullOrWhiteSpace(KoreanName) ? KoreanName : EnglishName;
        }

        public string GetDescription(bool korean)
        {
            return korean && !string.IsNullOrWhiteSpace(KoreanDescription) ? KoreanDescription : EnglishDescription;
        }

        public float Activate(Horse horse, float trackLength)
        {
            if (horse == null)
            {
                return 0f;
            }

            horse.TemporarySpeed += SpeedBoost;
            horse.TemporaryAcceleration += AccelerationBoost;
            horse.Fatigue = Mathf.Max(0f, horse.Fatigue - FatigueRecovery);

            if (EffectType == HorseSkillEffectType.LateCharge)
            {
                horse.TemporarySpeed += horse.Distance >= trackLength * LateChargeThreshold
                    ? LateChargeSpeedBoost
                    : 0f;
            }

            horse.SkillCooldown = Cooldown;
            horse.SkillEffectTimer = EffectDuration;
            horse.SkillMessage = EnglishName;
            return RetainedMana;
        }
    }
}
