#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Malatro.Editor
{
    [InitializeOnLoad]
    public static class RaceDatabaseGenerator
    {
        private const string DataFolder = "Assets/GameData/Races";
        private const string DatabasePath = "Assets/Resources/RaceDatabase.asset";

        static RaceDatabaseGenerator()
        {
            EditorApplication.delayCall += CreateDefaultDatabaseIfMissing;
        }

        [MenuItem("Malatro/Database/Create or Repair Default Race Database")]
        public static void CreateDefaultDatabaseIfMissing()
        {
            EnsureFolder("Assets/GameData");
            EnsureFolder(DataFolder);
            EnsureFolder("Assets/Resources");

            var database = AssetDatabase.LoadAssetAtPath<RaceDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<RaceDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            if (database.Races == null)
            {
                database.Races = new System.Collections.Generic.List<RaceData>();
            }

            var defaults = RaceDatabase.CreateRuntimeDefaults();
            foreach (var source in defaults.Races)
            {
                var path = $"{DataFolder}/{source.Id}.asset";
                var race = AssetDatabase.LoadAssetAtPath<RaceData>(path);
                if (race == null)
                {
                    race = ScriptableObject.CreateInstance<RaceData>();
                    Copy(source, race);
                    AssetDatabase.CreateAsset(race, path);
                }

                if (!database.Races.Contains(race))
                {
                    database.Races.Add(race);
                }
                EditorUtility.SetDirty(race);
            }

            database.Races.RemoveAll(race => race == null);
            EditorUtility.SetDirty(database);
            Object.DestroyImmediate(defaults);
            AssetDatabase.SaveAssets();
        }

        private static void Copy(RaceData source, RaceData target)
        {
            target.Id = source.Id;
            target.EnglishName = source.EnglishName;
            target.KoreanName = source.KoreanName;
            target.TotalDistanceMeters = source.TotalDistanceMeters;
            target.Surface = source.Surface;
            target.League = source.League;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }
    }
}
#endif
