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

        [MenuItem("Malatro/Database/Create Empty Race Database")]
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

            database.Races.RemoveAll(race => race == null);
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
