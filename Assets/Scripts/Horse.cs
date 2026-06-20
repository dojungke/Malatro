using System;
using System.Collections.Generic;
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
        public float PreviousDistance;
        public float CurrentSpeed;
        public float LaneOffset;
        public float TargetLaneOffset;
        public float PreferredLaneOffset;
        public float LateralVelocity;
        public bool IsPassing;
        public float TemporarySpeed;
        public float TemporaryAcceleration;
        public float TimedSpeedBonus;
        public float TimedAccelerationBonus;
        public float TimedBoostTimer;
        public float RelicSpeedBonus;
        public float RelicAccelerationBonus;
        public float RelicStaminaBonus;
        public float RelicMagicBonus;
        public float Fatigue;
        public float Mana;
        public float FinishTime;
        public float SkillCooldown;
        public float SkillEffectTimer;
        public float StunTimer;
        public float TimeStopTimer;
        public float SpeedMultiplier = 1f;
        public float SpeedMultiplierTimer;
        public Horse RideTarget;
        public float RideTimer;
        public float RideEndJumpDistance;
        public float RideTargetSpeedMultiplier = 1f;
        public float FreeRideSpeedMultiplier = 1f;
        public bool Finished;
        public string SkillMessage;
        public HorseSkillData Skill;
        public Color Color;
        public GameObject Visual;
        public SpriteRenderer Renderer;
        public Sprite[] RunFrames;
        public Texture2D RunSheet;
        public bool Manifested;

        private float animationClock;
        private int animationFrame;
        private Sprite[] baseRunFrames;
        private Sprite baseStaticSprite;
        private Color baseRendererColor = Color.white;

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
            PreviousDistance = 0f;
            CurrentSpeed = 0f;
            LaneOffset = 0f;
            TargetLaneOffset = 0f;
            PreferredLaneOffset = 0f;
            LateralVelocity = 0f;
            IsPassing = false;
            TemporarySpeed = 0f;
            TemporaryAcceleration = 0f;
            TimedSpeedBonus = 0f;
            TimedAccelerationBonus = 0f;
            TimedBoostTimer = 0f;
            RelicSpeedBonus = 0f;
            RelicAccelerationBonus = 0f;
            RelicStaminaBonus = 0f;
            RelicMagicBonus = 0f;
            Fatigue = 0f;
            Mana = startingMana;
            FinishTime = 0f;
            SkillCooldown = 0f;
            SkillEffectTimer = 0f;
            StunTimer = 0f;
            TimeStopTimer = 0f;
            SpeedMultiplier = 1f;
            SpeedMultiplierTimer = 0f;
            RideTarget = null;
            RideTimer = 0f;
            RideEndJumpDistance = 0f;
            RideTargetSpeedMultiplier = 1f;
            FreeRideSpeedMultiplier = 1f;
            Finished = false;
            Manifested = false;
            SkillMessage = string.Empty;
            animationClock = 0f;
            animationFrame = 0;

            if (baseRunFrames != null && baseRunFrames.Length > 0)
            {
                RunFrames = baseRunFrames;
            }

            if (Renderer != null && RunFrames != null && RunFrames.Length > 0)
            {
                Renderer.color = baseRendererColor;
                Renderer.sprite = RunFrames[0];
            }
            else if (Renderer != null)
            {
                Renderer.color = baseRendererColor;
                Renderer.sprite = baseStaticSprite;
            }
        }

        public void Finish(float finishTime)
        {
            Finished = true;
            FinishTime = finishTime;
            CurrentSpeed = 0f;
            SkillEffectTimer = 0f;
            SkillMessage = string.Empty;
            StunTimer = 0f;
            TimeStopTimer = 0f;
            SpeedMultiplier = 1f;
            SpeedMultiplierTimer = 0f;
            TimedSpeedBonus = 0f;
            TimedAccelerationBonus = 0f;
            TimedBoostTimer = 0f;
        }

        public void SetRunFrames(SpriteRenderer renderer, Sprite firstFrame, Sprite secondFrame)
        {
            Renderer = renderer;
            RunFrames = new[] { firstFrame, secondFrame };
            baseRunFrames = RunFrames;
            RunSheet = firstFrame.texture;
            baseRendererColor = Color.white;
            Renderer.sprite = firstFrame;
        }

        public void SetStaticVisual(SpriteRenderer renderer, Sprite sprite, Color color)
        {
            Renderer = renderer;
            RunFrames = null;
            baseRunFrames = null;
            baseStaticSprite = sprite;
            baseRendererColor = color;
            Renderer.sprite = sprite;
            Renderer.color = color;
        }

        public void Manifest(Texture2D manifestedRunSheet)
        {
            Manifested = true;
            if (Renderer == null || manifestedRunSheet == null)
            {
                return;
            }

            var pixelsPerUnit = manifestedRunSheet.height / 0.9f;
            RunFrames = new[]
            {
                CreateFrameSprite(manifestedRunSheet, false, pixelsPerUnit),
                CreateFrameSprite(manifestedRunSheet, true, pixelsPerUnit)
            };
            animationFrame = 0;
            animationClock = 0f;
            Renderer.color = Color.white;
            Renderer.sprite = RunFrames[0];
        }

        private static Sprite CreateFrameSprite(Texture2D texture, bool secondFrame, float pixelsPerUnit)
        {
            var isTwoFrameSheet = texture.width >= texture.height * 1.9f;
            var frameWidth = isTwoFrameSheet ? texture.width * 0.5f : texture.width;
            var x = isTwoFrameSheet && secondFrame ? frameWidth : 0f;
            return Sprite.Create(
                texture,
                new Rect(x, 0f, frameWidth, texture.height),
                new Vector2(0.5f, 0.42f),
                Mathf.Max(1f, pixelsPerUnit));
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
            SpeedMultiplierTimer = Mathf.Max(0f, SpeedMultiplierTimer - deltaTime);
            if (SpeedMultiplierTimer <= 0f)
            {
                SpeedMultiplier = 1f;
            }
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

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || Data == null || Data.Tags == null)
            {
                return false;
            }

            foreach (var horseTag in Data.Tags)
            {
                if (string.Equals(horseTag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyTag(IEnumerable<string> tags)
        {
            if (tags == null)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetSkillName() => Skill != null ? Skill.EnglishName : "Skill";
    }
}
