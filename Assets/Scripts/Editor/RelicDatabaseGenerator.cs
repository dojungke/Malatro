#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Malatro.Editor
{
    [InitializeOnLoad]
    public static class RelicDatabaseGenerator
    {
        private const string DataFolder = "Assets/GameData/Relics";
        private const string DatabasePath = "Assets/Resources/RelicDatabase.asset";

        static RelicDatabaseGenerator()
        {
            EditorApplication.delayCall += CreateDefaultDatabaseIfMissing;
        }

        [MenuItem("Malatro/Database/Create or Repair Default Relic Database")]
        public static void CreateDefaultDatabaseIfMissing()
        {
            EnsureFolder("Assets/GameData");
            EnsureFolder(DataFolder);
            EnsureFolder("Assets/Resources");

            var database = AssetDatabase.LoadAssetAtPath<RelicDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<RelicDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            if (database.Relics == null)
            {
                database.Relics = new System.Collections.Generic.List<RelicData>();
            }

            var defaults = RelicDatabase.CreateRuntimeDefaults();
            foreach (var source in defaults.Relics)
            {
                var path = $"{DataFolder}/{source.Id}.asset";
                var relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);
                if (relic == null)
                {
                    relic = ScriptableObject.CreateInstance<RelicData>();
                    Copy(source, relic);
                    AssetDatabase.CreateAsset(relic, path);
                }

                if (!database.Relics.Contains(relic))
                {
                    database.Relics.Add(relic);
                }
                EditorUtility.SetDirty(relic);
            }

            database.Relics.RemoveAll(relic => relic == null);
            EditorUtility.SetDirty(database);
            Object.DestroyImmediate(defaults);
            AssetDatabase.SaveAssets();
        }

        private static void Copy(RelicData source, RelicData target)
        {
            target.Id = source.Id;
            target.EnglishName = source.EnglishName;
            target.KoreanName = source.KoreanName;
            target.EnglishDescription = source.EnglishDescription;
            target.KoreanDescription = source.KoreanDescription;
            target.Rarity = source.Rarity;
            target.EffectType = source.EffectType;
            target.Price = source.Price;
            target.Color = source.Color;
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
