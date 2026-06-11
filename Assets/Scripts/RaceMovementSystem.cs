using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Malatro
{
    public static class RaceMovementSystem
    {
        private const float MetersPerSimulationUnit = 8f;
        private const float PassingTriggerMeters = 3f;
        private const float PassingClearanceMeters = 4f;
        private const float PassingLaneClearance = 0.18f;
        private const float LaneChangeSmoothTime = 0.38f;
        private const float LaneChangeMaxSpeed = 0.48f;
        private const float LaneCollisionWidth = 0.14f;
        private const float CornerOutsideSpeedPenalty = 0.3f;

        public static void Initialize(IReadOnlyList<Horse> field)
        {
            var laneDivisor = Mathf.Max(1, field.Count - 1);
            foreach (var horse in field)
            {
                var gateOffset = horse.Lane / (float)laneDivisor;
                var preferenceSeed = Mathf.Repeat(
                    horse.Lane * 0.271f
                    + horse.Speed * 0.037f
                    + horse.Acceleration * 0.019f,
                    1f);
                horse.LaneOffset = gateOffset;
                horse.TargetLaneOffset = gateOffset;
                horse.PreferredLaneOffset = Mathf.Clamp(
                    0.08f + preferenceSeed * 0.3f + gateOffset * 0.08f,
                    0.05f,
                    0.48f);
                horse.LateralVelocity = 0f;
            }
        }

        public static void Update(
            IReadOnlyList<Horse> field,
            RaceData race,
            float raceClock,
            float deltaTime)
        {
            foreach (var horse in field)
            {
                if (horse.Finished || horse.TimeStopTimer > 0f || horse.StunTimer > 0f)
                {
                    continue;
                }

                var release = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 2.8f, raceClock));
                var gateOffset = horse.Lane / (float)Mathf.Max(1, field.Count - 1);
                var cruisingLine = Mathf.Lerp(gateOffset, horse.PreferredLaneOffset, release);
                var corner = race?.GetCornerAt(horse.Distance * MetersPerSimulationUnit);
                if (corner != null)
                {
                    var insidePull = Mathf.Clamp01(0.32f * corner.VisualStrength);
                    cruisingLine = Mathf.Lerp(cruisingLine, 0.04f, insidePull);
                }

                var wander = Mathf.Sin(raceClock * 0.72f + horse.Lane * 1.91f)
                    * 0.025f
                    * release
                    * (corner == null ? 1f : 0.35f);
                cruisingLine = Mathf.Clamp(cruisingLine + wander, 0.03f, 0.97f);

                var blocker = FindBlockingHorse(horse, field);
                if (blocker != null && horse.CurrentSpeed >= blocker.CurrentSpeed - 0.35f)
                {
                    horse.IsPassing = true;
                    horse.TargetLaneOffset = FindPassingLine(horse, blocker, cruisingLine, field);
                }
                else
                {
                    horse.TargetLaneOffset = ApplyNearbyHorseAvoidance(horse, cruisingLine, field);
                    horse.IsPassing = Mathf.Abs(horse.TargetLaneOffset - cruisingLine) > 0.08f;
                }

                horse.LaneOffset = Mathf.SmoothDamp(
                    horse.LaneOffset,
                    horse.TargetLaneOffset,
                    ref horse.LateralVelocity,
                    LaneChangeSmoothTime,
                    LaneChangeMaxSpeed,
                    deltaTime);
                horse.LaneOffset = Mathf.Clamp(horse.LaneOffset, 0.02f, 0.98f);
            }
        }

        public static float GetCornerLanePenalty(Horse horse, RaceData race)
        {
            var corner = race?.GetCornerAt(horse.Distance * MetersPerSimulationUnit);
            return corner == null
                ? 0f
                : horse.LaneOffset * CornerOutsideSpeedPenalty * corner.VisualStrength;
        }

        public static float GetTrafficSpeedLimit(
            Horse horse,
            IReadOnlyList<Horse> field,
            float unrestrictedSpeed)
        {
            var blocker = FindBlockingHorse(horse, field);
            if (blocker == null)
            {
                return unrestrictedSpeed;
            }

            var gapMeters = Mathf.Clamp(
                (blocker.Distance - horse.Distance) * MetersPerSimulationUnit,
                0f,
                PassingTriggerMeters);
            return Mathf.Lerp(
                Mathf.Max(1.5f, blocker.CurrentSpeed * 0.9f),
                blocker.CurrentSpeed + 0.5f,
                gapMeters / PassingTriggerMeters);
        }

        private static float FindPassingLine(
            Horse horse,
            Horse blocker,
            float cruisingLine,
            IReadOnlyList<Horse> field)
        {
            var outside = Mathf.Clamp(blocker.LaneOffset + PassingLaneClearance, 0.03f, 0.97f);
            var inside = Mathf.Clamp(blocker.LaneOffset - PassingLaneClearance, 0.03f, 0.97f);
            var outsideClear = IsLaneClear(horse, outside, field);
            var insideClear = IsLaneClear(horse, inside, field);

            if (outsideClear && insideClear)
            {
                var outsideCost = Mathf.Abs(outside - cruisingLine);
                var insideCost = Mathf.Abs(inside - cruisingLine) + 0.035f;
                return outsideCost <= insideCost ? outside : inside;
            }

            if (outsideClear)
            {
                return outside;
            }

            if (insideClear)
            {
                return inside;
            }

            return ApplyNearbyHorseAvoidance(horse, cruisingLine, field);
        }

        private static float ApplyNearbyHorseAvoidance(
            Horse horse,
            float desiredLine,
            IReadOnlyList<Horse> field)
        {
            var avoidance = 0f;
            foreach (var other in field)
            {
                if (other == horse || other.Finished)
                {
                    continue;
                }

                var longitudinalGap = Mathf.Abs(other.Distance - horse.Distance) * MetersPerSimulationUnit;
                var lateralGap = horse.LaneOffset - other.LaneOffset;
                if (longitudinalGap > 5f || Mathf.Abs(lateralGap) > 0.22f)
                {
                    continue;
                }

                var direction = Mathf.Abs(lateralGap) > 0.01f
                    ? Mathf.Sign(lateralGap)
                    : horse.Lane < other.Lane ? -1f : 1f;
                avoidance += direction
                    * (1f - longitudinalGap / 5f)
                    * (1f - Mathf.Abs(lateralGap) / 0.22f)
                    * 0.16f;
            }

            return Mathf.Clamp(desiredLine + avoidance, 0.03f, 0.97f);
        }

        private static Horse FindBlockingHorse(Horse horse, IReadOnlyList<Horse> field)
        {
            return field
                .Where(other => other != horse
                    && !other.Finished
                    && other.Distance > horse.Distance
                    && (other.Distance - horse.Distance) * MetersPerSimulationUnit <= PassingTriggerMeters
                    && Mathf.Abs(other.LaneOffset - horse.LaneOffset) <= LaneCollisionWidth)
                .OrderBy(other => other.Distance - horse.Distance)
                .FirstOrDefault();
        }

        private static bool IsLaneClear(
            Horse horse,
            float targetLane,
            IReadOnlyList<Horse> field)
        {
            return field.All(other => other == horse
                || Mathf.Abs(other.Distance - horse.Distance) * MetersPerSimulationUnit > PassingClearanceMeters
                || Mathf.Abs(other.LaneOffset - targetLane) > LaneCollisionWidth);
        }
    }
}
