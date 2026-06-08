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
            return database != null ? database : CreateEmptyRuntimeDatabase();
        }

        public static RaceDatabase CreateEmptyRuntimeDatabase()
        {
            return CreateInstance<RaceDatabase>();
        }
    }
}
