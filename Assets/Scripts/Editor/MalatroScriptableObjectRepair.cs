#if UNITY_EDITOR
using UnityEditor;

namespace Malatro.Editor
{
    public static class MalatroScriptableObjectRepair
    {
        [MenuItem("Malatro/Database/Repair All Scriptable Objects")]
        public static void RepairAll()
        {
            AssetDatabase.ImportAsset(
                "Assets/GameData",
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            AssetDatabase.ImportAsset(
                "Assets/Resources/HorseDatabase.asset",
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                "Assets/Resources/RelicDatabase.asset",
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                "Assets/Resources/RaceDatabase.asset",
                ImportAssetOptions.ForceUpdate);

            HorseDatabaseGenerator.CreateDefaultDatabaseIfMissing();
            RelicDatabaseGenerator.CreateDefaultDatabaseIfMissing();
            RaceDatabaseGenerator.CreateDefaultDatabaseIfMissing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif
