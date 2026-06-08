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

    [CreateAssetMenu(fileName = "RaceData", menuName = "Malatro/Race Data")]
    public sealed class RaceData : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string EnglishName;
        public string KoreanName;

        [Header("Fixed Race Properties")]
        [Min(400)] public int TotalDistanceMeters = 1600;
        public TrackSurface Surface = TrackSurface.Turf;
        public RaceLeague League = RaceLeague.G3;

        public float SimulationLength => TotalDistanceMeters / 8f;

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
