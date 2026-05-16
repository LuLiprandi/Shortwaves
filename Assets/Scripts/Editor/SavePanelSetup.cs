using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot editor utility: builds the SaveSlot sub-hierarchy inside the
/// MainMenu JournalPanel and wires the MainMenuSavePanel component.
/// Run via Tools > Shortwaves > Setup Save Panel.
/// </summary>
public static class SavePanelSetup
{
    [MenuItem("Tools/Shortwaves/Setup Save Panel")]
    private static void Run()
    {
        // ── Load MainMenu scene ───────────────────────────────────────────────
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ── Find JournalPanel ─────────────────────────────────────────────────
        var journalPanel = FindInScene("JournalPanel");
        if (journalPanel == null)
        {
            Debug.LogError("[SavePanelSetup] JournalPanel introuvable dans MainMenu.unity.");
            return;
        }

        // ── Remove any existing SavePanel child to avoid duplicates ───────────
        var existing = journalPanel.transform.Find("SavePanel");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
            Debug.Log("[SavePanelSetup] Ancien SavePanel supprimé.");
        }

        // ── Create SavePanel root ─────────────────────────────────────────────
        var savePanelGo = new GameObject("SavePanel");
        savePanelGo.transform.SetParent(journalPanel.transform, false);

        var savePanelRect = savePanelGo.AddComponent<RectTransform>();
        // Full-stretch inside JournalPanel with insets
        savePanelRect.anchorMin = Vector2.zero;
        savePanelRect.anchorMax = Vector2.one;
        savePanelRect.offsetMin = new Vector2(20f, 20f);
        savePanelRect.offsetMax = new Vector2(-20f, -20f);

        var savePanel = savePanelGo.AddComponent<MainMenuSavePanel>();

        // ── Create SlotContainer (scroll viewport content) ────────────────────
        // ScrollView wrapper
        var scrollGo   = new GameObject("ScrollView");
        scrollGo.transform.SetParent(savePanelGo.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var scrollComponent = scrollGo.AddComponent<ScrollRect>();
        scrollComponent.horizontal = false;
        scrollComponent.vertical   = true;

        // Viewport
        var viewportGo   = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // transparent, required for Mask
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;

        scrollComponent.viewport = viewportRect;

        // Content (SlotContainer)
        var contentGo   = new GameObject("SlotContainer");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot     = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(8, 8, 8, 8);

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        scrollComponent.content = contentRect;

        // ── Wire slotContainer reference ──────────────────────────────────────
        var so = new SerializedObject(savePanel);
        so.FindProperty("slotContainer").objectReferenceValue = contentGo.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── Save scene ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SavePanelSetup] ✓ SavePanel créé et sauvegardé dans MainMenu.unity.");
    }

    private static GameObject FindInScene(string name)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.name == name) return go;
        return null;
    }
}
