#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Malatro.Editor
{
    public static class RaceDatabaseGenerator
    {
        private const string DataFolder = "Assets/GameData/Races";
        private const string DatabasePath = "Assets/Resources/RaceDatabase.asset";

        [MenuItem("Malatro/Database/Create Empty Race Database")]
        public static void CreateDefaultDatabaseIfMissing()
        {
            var probe = ScriptableObject.CreateInstance<RaceDatabase>();
            var databaseScriptAvailable = probe != null && MonoScript.FromScriptableObject(probe) != null;
            if (probe != null)
            {
                Object.DestroyImmediate(probe);
            }

            if (!databaseScriptAvailable)
            {
                Debug.LogWarning("RaceDatabase script is unavailable. Race database repair was skipped.");
                return;
            }

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

            database.Races.Clear();
            var guids = AssetDatabase.FindAssets("t:RaceData", new[] { DataFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var race = AssetDatabase.LoadAssetAtPath<RaceData>(path);
                if (race != null)
                {
                    database.Races.Add(race);
                }
            }

            database.Races.Sort((left, right) =>
                string.Compare(left.Id, right.Id, System.StringComparison.Ordinal));
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
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
