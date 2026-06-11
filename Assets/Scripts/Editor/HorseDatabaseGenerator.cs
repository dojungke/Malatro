#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Malatro.Editor
{
    [InitializeOnLoad]
    // 프로젝트를 열 때 기본 말/스킬 에셋이 없으면 생성하고 기존 데이터를 현재 형식으로 보정한다.
    public static class HorseDatabaseGenerator
    {
        private const string DataFolder = "Assets/GameData/Horses";
        private const string SkillFolder = "Assets/GameData/Skills";
        private const string DatabasePath = "Assets/Resources/HorseDatabase.asset";

        static HorseDatabaseGenerator()
        {
            EditorApplication.delayCall += CreateDefaultDatabaseIfMissing;
        }

        [MenuItem("Malatro/Database/Create or Repair Default Horse Database")]
        public static void CreateDefaultDatabaseIfMissing()
        {
            EnsureFolder("Assets/GameData");
            EnsureFolder(DataFolder);
            EnsureFolder(SkillFolder);
            EnsureFolder("Assets/Resources");

            var defaults = new[]
            {
                new HorseSeed("midnight-mint", "Midnight Mint", "미드나이트 민트", "Mint", "민트", "leaf-run", "late-charge"),
                new HorseSeed("lucky-stirrup", "Lucky Stirrup", "럭키 스터럽", "Lucky", "럭키", "purple-run", "wind-step"),
                new HorseSeed("velvet-thunder", "Velvet Thunder", "벨벳 썬더", "Velvet", "벨벳", "witch-run", "second-wind"),
                new HorseSeed("pocket-comet", "Pocket Comet", "포켓 코멧", "Comet", "코멧", "blue-cat-run", "mana-surge"),
                new HorseSeed("dust-sonata", "Dust Sonata", "더스트 소나타", "Sonata", "소나타", "blond-cat-run", "iron-rhythm"),
                new HorseSeed("iron-clover", "Iron Clover", "아이언 클로버", "Clover", "클로버", "ram-run", "lightning-start")
            };

            var database = AssetDatabase.LoadAssetAtPath<HorseDatabase>(DatabasePath);
            if (database != null && database.Horses != null && database.Horses.Count > 0)
            {
                MigrateExistingHorseSkills(database);
                AssignCustomHorseSkills(database);
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                return;
            }

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<HorseDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            if (database.Horses == null)
            {
                database.Horses = new System.Collections.Generic.List<HorseData>();
            }
            for (var i = 0; i < defaults.Length; i++)
            {
                var seed = defaults[i];
                var assetPath = $"{DataFolder}/{seed.Id}.asset";
                var data = AssetDatabase.LoadAssetAtPath<HorseData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<HorseData>();
                    data.Id = seed.Id;
                    data.EnglishName = seed.EnglishName;
                    data.KoreanName = seed.KoreanName;
                    data.EnglishShortName = seed.EnglishShortName;
                    data.KoreanShortName = seed.KoreanShortName;
                    data.Speed = new IntStatRange(7, 13);
                    data.Acceleration = new IntStatRange(5, 12);
                    data.Stamina = new IntStatRange(6, 13);
                    data.Magic = new IntStatRange(8, 18);
                    data.TurfAptitude = TrackAptitudeGrade.C;
                    data.DirtAptitude = TrackAptitudeGrade.C;
                    data.OpeningOddsRange = new Vector2(1.8f, 7.5f);
                    data.UiColor = Color.HSVToRGB(i / (float)defaults.Length, 0.75f, 0.95f);
                    data.RunSheet = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Horses/{seed.TextureName}.png");
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                data.SkillData = LoadOrCreateSkill(seed.SkillId);
                if (!database.Horses.Contains(data))
                {
                    database.Horses.Add(data);
                }
                EditorUtility.SetDirty(data);
            }

            EditorUtility.SetDirty(database);
            MigrateExistingHorseSkills(database);
            AssignCustomHorseSkills(database);
            AssetDatabase.SaveAssets();
        }

        private static void AssignCustomHorseSkills(HorseDatabase database)
        {
            AssignCustomHorseSkill(database, "Assets/GameData/Horses/2Li.asset", "howl");
            AssignCustomHorseSkill(database, "Assets/GameData/Horses/Mackerel.asset", "leap");
            AssignCustomHorseSkill(database, "Assets/GameData/Horses/Rock.asset", "trip-up");
        }

        private static void AssignCustomHorseSkill(HorseDatabase database, string horsePath, string skillId)
        {
            var horse = AssetDatabase.LoadAssetAtPath<HorseData>(horsePath);
            if (horse == null)
            {
                return;
            }

            horse.SkillData = LoadOrCreateSkill(skillId);
            if (!database.Horses.Contains(horse))
            {
                database.Horses.Add(horse);
            }

            EditorUtility.SetDirty(horse);
        }

        private static void MigrateExistingHorseSkills(HorseDatabase database)
        {
            // 예전 정수형 스킬 값의 순서를 유지해 대응하는 HorseSkillData 에셋으로 옮긴다.
            var skillIds = new[]
            {
                "lightning-start",
                "wind-step",
                "second-wind",
                "mana-surge",
                "iron-rhythm",
                "late-charge"
            };

            foreach (var horse in database.Horses)
            {
                if (horse == null || horse.SkillData != null)
                {
                    continue;
                }

                var index = Mathf.Clamp(horse.LegacySkill, 0, skillIds.Length - 1);
                horse.SkillData = LoadOrCreateSkill(skillIds[index]);
                EditorUtility.SetDirty(horse);
            }
        }

        private static HorseSkillData LoadOrCreateSkill(string id)
        {
            var path = $"{SkillFolder}/{id}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<HorseSkillData>(path);
            if (skill != null)
            {
                return skill;
            }

            skill = HorseDatabase.CreateRuntimeSkill(id);
            AssetDatabase.CreateAsset(skill, path);
            return skill;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            // 중첩 경로의 부모 폴더부터 재귀적으로 만든다.
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }

        private readonly struct HorseSeed
        {
            public readonly string Id;
            public readonly string EnglishName;
            public readonly string KoreanName;
            public readonly string EnglishShortName;
            public readonly string KoreanShortName;
            public readonly string TextureName;
            public readonly string SkillId;

            public HorseSeed(
                string id,
                string englishName,
                string koreanName,
                string englishShortName,
                string koreanShortName,
                string textureName,
                string skillId)
            {
                Id = id;
                EnglishName = englishName;
                KoreanName = koreanName;
                EnglishShortName = englishShortName;
                KoreanShortName = koreanShortName;
                TextureName = textureName;
                SkillId = skillId;
            }
        }
    }
}
#endif
