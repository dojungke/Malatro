using System.Collections.Generic;
using UnityEngine;

namespace Malatro
{
    [CreateAssetMenu(fileName = "RaceDatabase", menuName = "Malatro/Race Database")]
    public sealed class RaceDatabase : ScriptableObject
    {
        public const string ResourcePath = "RaceDatabase";

        public List<RaceData> Races = new List<RaceData>();

        public static RaceDatabase LoadOrCreateRuntimeDefaults()
        {
            var database = Resources.Load<RaceDatabase>(ResourcePath);
            return database != null ? database : CreateRuntimeDefaults();
        }

        public static RaceDatabase CreateRuntimeDefaults()
        {
            var database = CreateInstance<RaceDatabase>();
            database.Races.Add(CreateRace("spring-dash", "Spring Dash", "봄바람 단거리", 1200, TrackSurface.Turf, RaceLeague.G3));
            database.Races.Add(CreateRace("brown-dirt-cup", "Brown Dirt Cup", "브라운 더트 컵", 1600, TrackSurface.Dirt, RaceLeague.G3));
            database.Races.Add(CreateRace("moonlight-mile", "Moonlight Mile", "달빛 마일", 1600, TrackSurface.Turf, RaceLeague.G2));
            database.Races.Add(CreateRace("iron-sand-stakes", "Iron Sand Stakes", "철모래 스테이크스", 2000, TrackSurface.Dirt, RaceLeague.G2));
            database.Races.Add(CreateRace("royal-turf-crown", "Royal Turf Crown", "로열 터프 크라운", 2000, TrackSurface.Turf, RaceLeague.G1));
            database.Races.Add(CreateRace("grand-dirt-prix", "Grand Dirt Prix", "그랜드 더트 프리", 1600, TrackSurface.Dirt, RaceLeague.G1));
            return database;
        }

        private static RaceData CreateRace(
            string id,
            string englishName,
            string koreanName,
            int distanceMeters,
            TrackSurface surface,
            RaceLeague league)
        {
            var race = CreateInstance<RaceData>();
            race.Id = id;
            race.EnglishName = englishName;
            race.KoreanName = koreanName;
            race.TotalDistanceMeters = distanceMeters;
            race.Surface = surface;
            race.League = league;
            return race;
        }
    }
}
