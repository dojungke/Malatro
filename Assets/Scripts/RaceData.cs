using System;
using System.Collections.Generic;
using UnityEngine;

namespace Malatro
{
    public enum TrackSurface
    {
        Dirt,
        Turf
    }

    public enum RaceLeague
    {
        G3,
        G2,
        G1
    }

    public enum RaceTurnDirection
    {
        Left = 1,
        Right = -1
    }

    [Serializable]
    public sealed class RaceCornerSection
    {
        [Min(0)] public int StartDistanceMeters = 400;
        [Min(50)] public int LengthMeters = 250;
        public RaceTurnDirection Direction = RaceTurnDirection.Left;
        [Range(0.25f, 1.5f)] public float VisualStrength = 1f;

        public int EndDistanceMeters => StartDistanceMeters + LengthMeters;

        public float GetPhase(float distanceMeters)
        {
            return Mathf.InverseLerp(StartDistanceMeters, EndDistanceMeters, distanceMeters);
        }
    }

    [CreateAssetMenu(fileName = "RaceData", menuName = "Malatro/Race Data")]
    public sealed class RaceData : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string EnglishName;
        public string KoreanName;

        [Header("Fixed Race Properties")]
        [Min(400)] public int TotalDistanceMeters = 1600;
        [Min(2)] public int EntrantCount = 6;
        public TrackSurface Surface = TrackSurface.Turf;
        public RaceLeague League = RaceLeague.G3;

        [Header("Course Sections")]
        public List<RaceCornerSection> Corners = new List<RaceCornerSection>();

        public float SimulationLength => TotalDistanceMeters / 8f;

        public RaceCornerSection GetCornerAt(float distanceMeters)
        {
            if (Corners == null)
            {
                return null;
            }

            foreach (var corner in Corners)
            {
                if (corner != null && distanceMeters >= corner.StartDistanceMeters && distanceMeters <= corner.EndDistanceMeters)
                {
                    return corner;
                }
            }

            return null;
        }

        public string GetName(bool korean)
        {
            return korean && !string.IsNullOrWhiteSpace(KoreanName)
                ? KoreanName
                : EnglishName;
        }

        public string GetSurfaceName(bool korean)
        {
            return Surface switch
            {
                TrackSurface.Dirt => korean ? "더트" : "Dirt",
                TrackSurface.Turf => korean ? "잔디" : "Turf",
                _ => Surface.ToString()
            };
        }
    }
}
