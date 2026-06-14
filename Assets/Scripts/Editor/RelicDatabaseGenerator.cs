#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Malatro.Editor
{
    // 기본 유물 에셋과 Resources 데이터베이스가 항상 존재하도록 에디터에서 자동 복구한다.
    public static class RelicDatabaseGenerator
    {
        private const string DataFolder = "Assets/GameData/Relics";
        private const string DatabasePath = "Assets/Resources/RelicDatabase.asset";

        [MenuItem("Malatro/Database/Create or Repair Default Relic Database")]
        public static void CreateDefaultDatabaseIfMissing()
        {
            var probe = ScriptableObject.CreateInstance<RelicData>();
            var relicScriptAvailable = probe != null && MonoScript.FromScriptableObject(probe) != null;
            if (probe != null)
            {
                Object.DestroyImmediate(probe);
            }

            if (!relicScriptAvailable)
            {
                Debug.LogWarning("RelicData script is unavailable. Relic database repair was skipped.");
                return;
            }

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

                relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);
                if (relic != null && !database.Relics.Contains(relic))
                {
                    database.Relics.Add(relic);
                }
                if (relic != null)
                {
                    EditorUtility.SetDirty(relic);
                }
            }

            SyncRelicDatabase(database);
            EditorUtility.SetDirty(database);
            Object.DestroyImmediate(defaults);
            AssetDatabase.SaveAssets();
        }

        private static void SyncRelicDatabase(RelicDatabase database)
        {
            database.Relics.Clear();
            var guids = AssetDatabase.FindAssets("t:RelicData", new[] { DataFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);
                if (relic != null)
                {
                    database.Relics.Add(relic);
                }
            }

            database.Relics.Sort((left, right) =>
                string.Compare(left.Id, right.Id, System.StringComparison.Ordinal));
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
