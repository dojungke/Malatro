using System.Collections.Generic;
using UnityEngine;

namespace Malatro
{
    public enum HorseSkillEffectType
    {
        StandardBoost,
        LateCharge,
        KnightStrike,
        TimeStop,
        Sniper,
        Transfer,
        StarStair,
        AreaStun
    }

    [CreateAssetMenu(fileName = "HorseSkillData", menuName = "Malatro/Horse Skill Data")]
    // 말 스킬의 수치와 발동 규칙을 에셋으로 분리해 인스펙터에서 조정할 수 있게 한다.
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
        [Min(0f)] public float RetainedMana;

        [Header("Effects")]
        public HorseSkillEffectType EffectType;
        public float SpeedBoost;
        public float AccelerationBoost;
        public float FatigueRecovery;
        [Range(0f, 1f)] public float LateChargeThreshold = 0.62f;
        public float LateChargeSpeedBoost;
        [Min(0f)] public float ChargeDistanceMeters;
        [Min(0f)] public float StunDuration;
        [Min(0f)] public float TimeStopDuration;
        [Min(0f)] public float BonusPerHorseAhead = 0.25f;
        [Min(0f)] public float AreaRadiusMeters;

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

        public bool CanActivate(Horse horse, IReadOnlyList<Horse> field)
        {
            if (horse == null)
            {
                return false;
            }

            if (field == null)
            {
                return true;
            }

            if (EffectType == HorseSkillEffectType.Transfer)
            {
                foreach (var target in field)
                {
                    if (target != null
                        && target != horse
                        && !target.Finished
                        && target.Distance > horse.Distance)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (EffectType == HorseSkillEffectType.Sniper)
            {
                Horse leader = null;
                foreach (var target in field)
                {
                    if (target == null || target.Finished)
                    {
                        continue;
                    }

                    if (leader == null
                        || target.Distance > leader.Distance
                        || (Mathf.Approximately(target.Distance, leader.Distance) && target.Lane < leader.Lane))
                    {
                        leader = target;
                    }
                }

                return leader != null && leader != horse;
            }

            return true;
        }

        public float Activate(Horse horse, float trackLength, IReadOnlyList<Horse> field)
        {
            if (horse == null)
            {
                return 0f;
            }

            if (EffectType != HorseSkillEffectType.StarStair)
            {
                horse.TemporarySpeed += SpeedBoost;
                horse.TemporaryAcceleration += AccelerationBoost;
                horse.Fatigue = Mathf.Max(0f, horse.Fatigue - FatigueRecovery);
            }

            if (EffectType == HorseSkillEffectType.LateCharge)
            {
                // 후반 추입은 정해진 트랙 진행률을 넘었을 때만 추가 속도를 부여한다.
                horse.TemporarySpeed += horse.Distance >= trackLength * LateChargeThreshold
                    ? LateChargeSpeedBoost
                    : 0f;
            }
            else if (EffectType == HorseSkillEffectType.KnightStrike)
            {
                var startDistance = horse.Distance;
                var chargeDistance = ChargeDistanceMeters / 16f;
                var endDistance = Mathf.Min(trackLength, startDistance + chargeDistance);
                horse.Distance = endDistance;

                if (field != null)
                {
                    foreach (var target in field)
                    {
                        if (target == null || target == horse || target.Finished)
                        {
                            continue;
                        }

                        if (target.Distance >= startDistance && target.Distance <= endDistance)
                        {
                            target.StunTimer = Mathf.Max(target.StunTimer, StunDuration);
                            target.CurrentSpeed = 0f;
                        }
                    }
                }
            }
            else if (EffectType == HorseSkillEffectType.TimeStop && field != null)
            {
                foreach (var target in field)
                {
                    if (target == null || target == horse || target.Finished)
                    {
                        continue;
                    }

                    target.TimeStopTimer = Mathf.Max(target.TimeStopTimer, TimeStopDuration);
                    target.CurrentSpeed = 0f;
                }
            }
            else if (EffectType == HorseSkillEffectType.Sniper && field != null)
            {
                Horse leader = null;
                foreach (var target in field)
                {
                    if (target == null || target.Finished)
                    {
                        continue;
                    }

                    if (leader == null
                        || target.Distance > leader.Distance
                        || (Mathf.Approximately(target.Distance, leader.Distance) && target.Lane < leader.Lane))
                    {
                        leader = target;
                    }
                }

                if (leader != null && leader != horse)
                {
                    leader.StunTimer = Mathf.Max(leader.StunTimer, StunDuration);
                    leader.CurrentSpeed = 0f;
                }
            }
            else if (EffectType == HorseSkillEffectType.Transfer && field != null)
            {
                Horse nearestAhead = null;
                foreach (var target in field)
                {
                    if (target == null
                        || target == horse
                        || target.Finished
                        || target.Distance <= horse.Distance)
                    {
                        continue;
                    }

                    if (nearestAhead == null
                        || target.Distance < nearestAhead.Distance
                        || (Mathf.Approximately(target.Distance, nearestAhead.Distance)
                            && target.Lane < nearestAhead.Lane))
                    {
                        nearestAhead = target;
                    }
                }

                if (nearestAhead != null)
                {
                    var originalDistance = horse.Distance;
                    horse.Distance = nearestAhead.Distance;
                    nearestAhead.Distance = originalDistance;
                }
            }
            else if (EffectType == HorseSkillEffectType.StarStair && field != null)
            {
                var horsesAhead = 0;
                foreach (var target in field)
                {
                    if (target != null
                        && target != horse
                        && !target.Finished
                        && target.Distance > horse.Distance)
                    {
                        horsesAhead++;
                    }
                }

                var multiplier = 1f + horsesAhead * BonusPerHorseAhead;
                horse.TimedSpeedBonus = SpeedBoost * multiplier;
                horse.TimedAccelerationBonus = AccelerationBoost * multiplier;
                horse.TimedBoostTimer = EffectDuration;
            }
            else if (EffectType == HorseSkillEffectType.AreaStun && field != null)
            {
                var radius = AreaRadiusMeters / 16f;
                foreach (var target in field)
                {
                    if (target == null || target == horse || target.Finished)
                    {
                        continue;
                    }

                    if (Mathf.Abs(target.Distance - horse.Distance) <= radius)
                    {
                        target.StunTimer = Mathf.Max(target.StunTimer, StunDuration);
                        target.CurrentSpeed = 0f;
                    }
                }
            }

            horse.SkillCooldown = Cooldown;
            horse.SkillEffectTimer = EffectDuration;
            horse.SkillMessage = EnglishName;
            return Mathf.Clamp(RetainedMana, 0f, Mathf.Max(1f, ManaCost));
        }
    }
}
