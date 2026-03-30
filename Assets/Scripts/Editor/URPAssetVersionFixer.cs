using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Fixes a URP asset that fails the build pre-process version check.
/// URP 17.0.4 expects k_LastVersion == 12. VNTG_RPAsset was serialized with 13
/// (from a newer URP), so we downgrade both version fields to 12.
/// </summary>
public static class URPAssetVersionFixer
{
    private const string AssetPath = "Assets/VNTG/Renderers/VNTG_RPAsset.asset";
    private const int TargetVersion = 12; // k_LastVersion in URP 17.0.4

    [MenuItem("Tools/Fix VNTG URP Asset Version")]
    public static void Fix()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
        if (asset == null)
        {
            Debug.LogError($"[URPAssetVersionFixer] Asset introuvable : {AssetPath}");
            return;
        }

        var so = new SerializedObject(asset);

        SetVersion(so, "k_AssetVersion");
        SetVersion(so, "k_AssetPreviousVersion");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[URPAssetVersionFixer] VNTG_RPAsset mis à jour à la version {TargetVersion}. Relance le build.");
    }

    private static void SetVersion(SerializedObject so, string fieldName)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[URPAssetVersionFixer] Champ '{fieldName}' introuvable — ignoré.");
            return;
        }
        Debug.Log($"[URPAssetVersionFixer] {fieldName} : {prop.intValue} → {TargetVersion}");
        prop.intValue = TargetVersion;
    }
}
