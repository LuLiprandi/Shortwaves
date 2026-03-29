using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ColbyO.VNTG.CRT;

/// <summary>Editor utility to apply CRT settings overrides to a Volume Profile.</summary>
public static class CRTSettingsApplier
{
    private const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";

    [MenuItem("Tools/Apply CRT Settings (No VHS)")]
    public static void ApplyCRTSettings()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            Debug.LogError("CRTSettingsApplier: SampleSceneProfile not found at " + ProfilePath);
            return;
        }

        if (!profile.TryGet(out CRTSettings settings))
        {
            settings = profile.Add<CRTSettings>(overrides: true);
            Debug.Log("CRTSettingsApplier: CRTSettings component added.");
        }

        settings.active = true;

        // Disable CRT entirely — PSX Effect handles pixelation
        Override(settings.Enabled, false);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("CRTSettingsApplier: CRT settings applied successfully (VHS disabled).");
    }

    private static void Override<T>(VolumeParameter<T> param, T value)
    {
        param.overrideState = true;
        param.value = value;
    }
}
