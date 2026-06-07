using UnityEngine;

namespace Malatro
{
    // 한 경주에 참가하는 말의 고정 능력치와 실시간 상태를 함께 보관한다.
    public sealed class Horse
    {
        public HorseData Data;
        public string Name;
        public int Lane;
        public int Speed;
        public int Acceleration;
        public int Stamina;
        public int Magic;
        public float WinOdds;
        public float LastOddsDelta;
        public float Distance;
        public float CurrentSpeed;
        public float TemporarySpeed;
        public float TemporaryAcceleration;
        public float TimedSpeedBonus;
        public float TimedAccelerationBonus;
        public float TimedBoostTimer;
        public float RelicSpeedBonus;
        public float Fatigue;
        public float Mana;
        public float FinishTime;
        public float SkillCooldown;
        public float SkillEffectTimer;
        public float StunTimer;
        public float TimeStopTimer;
        public bool Finished;
        public string SkillMessage;
        public HorseSkillData Skill;
        public Color Color;
        public GameObject Visual;
        public SpriteRenderer Renderer;
        public Sprite[] RunFrames;
        public Texture2D RunSheet;

        private float animationClock;
        private int animationFrame;

        public int AnimationFrame => animationFrame;
        public float SkillEffectAmount => Skill == null
            ? 0f
            : Mathf.Clamp01(SkillEffectTimer / Mathf.Max(0.1f, Skill.EffectDuration));

        public Horse(
            HorseData data,
            string name,
            int lane,
            int speed,
            int acceleration,
            int stamina,
            int magic,
            float winOdds,
            HorseSkillData skill,
            Color color)
        {
            Data = data;
            Name = name;
            Lane = lane;
            Speed = speed;
            Acceleration = acceleration;
            Stamina = stamina;
            Magic = magic;
            WinOdds = winOdds;
            Skill = skill;
            Color = color;
        }

        public void ResetForRace(float startingMana)
        {
            // 영구 능력치와 배당은 유지하고 이번 경주에서만 쓰는 상태를 초기화한다.
            Distance = 0f;
            CurrentSpeed = 0f;
            TemporarySpeed = 0f;
            TemporaryAcceleration = 0f;
            TimedSpeedBonus = 0f;
            TimedAccelerationBonus = 0f;
            TimedBoostTimer = 0f;
            RelicSpeedBonus = 0f;
            Fatigue = 0f;
            Mana = startingMana;
            FinishTime = 0f;
            SkillCooldown = 0f;
            SkillEffectTimer = 0f;
            StunTimer = 0f;
            TimeStopTimer = 0f;
            Finished = false;
            SkillMessage = string.Empty;
            animationClock = 0f;
            animationFrame = 0;

            if (Renderer != null && RunFrames != null && RunFrames.Length > 0)
            {
                Renderer.sprite = RunFrames[0];
            }
        }

        public void SetRunFrames(SpriteRenderer renderer, Sprite firstFrame, Sprite secondFrame)
        {
            Renderer = renderer;
            RunFrames = new[] { firstFrame, secondFrame };
            RunSheet = firstFrame.texture;
            Renderer.sprite = firstFrame;
        }

        public void AnimateRun(float deltaTime)
        {
            if (Renderer == null || RunFrames == null || RunFrames.Length < 2)
            {
                return;
            }

            // 현재 속도가 빠를수록 두 프레임을 더 짧은 간격으로 교체한다.
            var frameDuration = Mathf.Lerp(0.18f, 0.09f, Mathf.InverseLerp(2f, 15f, CurrentSpeed));
            animationClock += deltaTime;
            if (animationClock < frameDuration)
            {
                return;
            }

            animationClock -= frameDuration;
            animationFrame = 1 - animationFrame;
            Renderer.sprite = RunFrames[animationFrame];
        }

        public void TickSkillEffect(float deltaTime)
        {
            SkillEffectTimer = Mathf.Max(0f, SkillEffectTimer - deltaTime);
            StunTimer = Mathf.Max(0f, StunTimer - deltaTime);
            TimedBoostTimer = Mathf.Max(0f, TimedBoostTimer - deltaTime);
            if (TimedBoostTimer <= 0f)
            {
                TimedSpeedBonus = 0f;
                TimedAccelerationBonus = 0f;
            }
            if (SkillEffectTimer <= 0f)
            {
                SkillMessage = string.Empty;
            }
        }

        public void TickTimeStop(float deltaTime)
        {
            TimeStopTimer = Mathf.Max(0f, TimeStopTimer - deltaTime);
        }

        public string GetStatLine()
        {
            return $"SPD {Speed}  ACC {Acceleration}  STA {Stamina}  MAG {Magic}  {GetSkillName()}";
        }

        public string GetSkillName() => Skill != null ? Skill.EnglishName : "Skill";
    }
}
