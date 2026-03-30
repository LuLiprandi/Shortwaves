using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Disables the VNTG CRT renderer feature while the main menu is active,
/// then re-enables it when transitioning to another scene.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MainMenuCameraSetup : MonoBehaviour
{
    private const string CrtFeatureTypeName = "ColbyO.VNTG.CRT.CRTRendererFeature";

    private ScriptableRendererFeature _crtFeature;

    private void Awake()
    {
        _crtFeature = FindCrtFeature();
        if (_crtFeature != null)
            _crtFeature.SetActive(false);

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        RestoreCrt();
    }

    private void OnSceneUnloaded(Scene _)
    {
        RestoreCrt();
    }

    /// <summary>Re-enables the CRT feature so other scenes are unaffected.</summary>
    private void RestoreCrt()
    {
        if (_crtFeature != null)
            _crtFeature.SetActive(true);
    }

    /// <summary>Finds the CRTRendererFeature in the active URP pipeline via reflection.</summary>
    private static ScriptableRendererFeature FindCrtFeature()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null) return null;

        var field = typeof(UniversalRenderPipelineAsset).GetField(
            "m_RendererDataList",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (field == null) return null;

        var rendererDataList = field.GetValue(urpAsset) as ScriptableRendererData[];
        if (rendererDataList == null) return null;

        foreach (var data in rendererDataList)
        {
            if (data == null) continue;
            foreach (var feature in data.rendererFeatures)
            {
                if (feature != null && feature.GetType().FullName == CrtFeatureTypeName)
                    return feature;
            }
        }

        return null;
    }
}
