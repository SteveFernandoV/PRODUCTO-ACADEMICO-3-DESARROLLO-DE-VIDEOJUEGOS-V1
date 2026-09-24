using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

public static class PA3SceneBuilder
{
    const string Root = "Assets/PA3";
    const string ScenePath = "Assets/Scenes/PA3_VerticalSlice.unity";
    const string MenuScenePath = "Assets/Scenes/PA3_MainMenu.unity";
    const string OldMotesPrefabPath = Root + "/Prefabs/EnergyMistVFX.prefab";
    const string MotesPrefabPath = Root + "/Prefabs/CrystalMotesVFX.prefab";
    const string NaturePackage = @"C:\Users\fer10\AppData\Roaming\Unity\Asset Store-5.x\Polytope Studio\3D ModelsEnvironments\Low Poly Environment - Nature Free - LOWPOLY MEDIEVAL FANTASY SERIES.unitypackage";
    const string SkyPackage = @"C:\Users\fer10\AppData\Roaming\Unity\Asset Store-5.x\rpgwhitelock\Textures MaterialsSkies\AllSky Free - 10 Sky Skybox Set.unitypackage";
    static readonly Color Ground = new Color(0.10f, 0.22f, 0.17f);
    static readonly Color Rock = new Color(0.23f, 0.28f, 0.34f);
    static readonly Color Accent = new Color(0.10f, 0.75f, 1f);
    static readonly List<GameObject> staticObjects = new List<GameObject>();

    [InitializeOnLoadMethod]
    static void QueueCrystalMotesAndPortalRepair()
    {
        EditorApplication.delayCall += () =>
        {
            const string key = "PA3_CrystalMotesPortalRepair_20260923";
            if (EditorPrefs.GetBool(key, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (RestorePortalAndPrepareCrystalMotesPrefabInternal()) EditorPrefs.SetBool(key, true);
        };
    }

    [MenuItem("PA3/Restore Exit Portal and Crystal Motes Prefab")]
    public static void RestorePortalAndPrepareCrystalMotesPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("PA3_PORTAL_REPAIR_SKIPPED: exit Play Mode before restoring the portal.");
            return;
        }

        bool success = RestorePortalAndPrepareCrystalMotesPrefabInternal();
        if (success) EditorPrefs.SetBool("PA3_CrystalMotesPortalRepair_20260923", true);
    }

    static bool RestorePortalAndPrepareCrystalMotesPrefabInternal()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(OldMotesPrefabPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(MotesPrefabPath) == null)
        {
            string moveError = AssetDatabase.MoveAsset(OldMotesPrefabPath, MotesPrefabPath);
            if (!string.IsNullOrEmpty(moveError))
            {
                Debug.LogError("PA3_MOTES_PREFAB_RENAME_FAILED: " + moveError);
                return false;
            }
        }

        Scene originalScene = SceneManager.GetActiveScene();
        Scene gameplayScene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !gameplayScene.IsValid() || !gameplayScene.isLoaded;
        if (openedHere)
        {
            gameplayScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            if (!gameplayScene.IsValid())
            {
                Debug.LogError("PA3_PORTAL_REPAIR_FAILED: could not open " + ScenePath);
                return false;
            }
        }

        bool saved = false;
        try
        {
            const float goalX = 30f;
            const float goalZ = 22f;
            GameObject portal = FindInScene(gameplayScene, "PA3_ExitPortal");
            if (portal == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/PA3_ExitPortal.prefab");
                if (prefab == null)
                {
                    Debug.LogError("PA3_PORTAL_PREFAB_MISSING: " + Root + "/Prefabs/PA3_ExitPortal.prefab");
                    return false;
                }
                portal = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gameplayScene);
            }
            portal.transform.SetPositionAndRotation(new Vector3(goalX, 0f, goalZ), Quaternion.Euler(0f, 180f, 0f));
            portal.SetActive(true);

            GameObject exit = FindInScene(gameplayScene, "Exit_Trigger_TEMPLATE");
            if (exit == null)
            {
                Debug.LogError("PA3_PORTAL_TRIGGER_MISSING: Exit_Trigger_TEMPLATE was not found in the gameplay scene.");
                return false;
            }
            exit.transform.SetPositionAndRotation(new Vector3(goalX, 2f, goalZ), Quaternion.identity);
            exit.transform.localScale = new Vector3(3f, 4f, 2.5f);
            var box = exit.GetComponent<BoxCollider>();
            if (box == null) box = exit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            if (exit.GetComponent<ExitTrigger>() == null) exit.AddComponent<ExitTrigger>();
            var renderer = exit.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            exit.SetActive(true);

            var sceneMotes = FindInScene(gameplayScene, "VFX_CrystalMotes");
            if (sceneMotes != null && sceneMotes.transform.parent != null && sceneMotes.transform.parent.name == "VFX")
            {
                var emptyVfxGroup = sceneMotes.transform.parent.gameObject;
                Object.DestroyImmediate(sceneMotes);
                if (emptyVfxGroup.transform.childCount == 0) Object.DestroyImmediate(emptyVfxGroup);
            }

            EditorSceneManager.MarkSceneDirty(gameplayScene);
            saved = EditorSceneManager.SaveScene(gameplayScene);
            if (saved)
            {
                AssetDatabase.Refresh();
                Debug.Log("PA3_PORTAL_RESTORED: portal prefab and active exit trigger saved in PA3_VerticalSlice at (30, 0, 22). Crystal Motes prefab is named CrystalMotesVFX.");
            }
        }
        finally
        {
            if (openedHere && gameplayScene.IsValid() && gameplayScene.isLoaded)
                EditorSceneManager.CloseScene(gameplayScene, true);
            if (originalScene.IsValid() && originalScene.isLoaded)
                SceneManager.SetActiveScene(originalScene);
        }

        return saved;
    }

    static GameObject FindInScene(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == objectName) return child.gameObject;
        return null;
    }

    [InitializeOnLoadMethod]
    static void QueuePortalPlacementForOpenPa3Scene()
    {
        EditorApplication.playModeStateChanged -= OnPortalEditorPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPortalEditorPlayModeChanged;
        EditorApplication.delayCall += () =>
        {
            TryFinishPa3SceneSetup();
        };
    }

    static void OnPortalEditorPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += TryFinishPa3SceneSetup;
    }

    static void TryFinishPa3SceneSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        CreateMainMenuSceneAndConfigureFlow();
        var scene = SceneManager.GetActiveScene();
        if (scene.path == ScenePath && GameObject.Find("PA3_ExitPortal") == null)
        {
            if (scene.isDirty) Debug.LogWarning("PA3_PORTAL_WAITING_FOR_USER_SAVE: save the gameplay scene, then use PA3/Place Exit Portal at Level Goal.");
            else PlaceExitPortalAtLevelGoal();
        }
    }

    [MenuItem("PA3/Create Main Menu Scene and Configure Flow")]
    public static void CreateMainMenuSceneAndConfigureFlow()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("PA3_MENU_SKIPPED: exit Play Mode before creating the menu scene."); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) == null)
        {
            Scene originalScene = SceneManager.GetActiveScene();
            Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(menuScene);
                BuildMainMenuScene();
                EditorSceneManager.SaveScene(menuScene, MenuScenePath);
            }
            finally
            {
                if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
                if (menuScene.IsValid() && menuScene.isLoaded) EditorSceneManager.CloseScene(menuScene, true);
            }
        }

        AssetDatabase.Refresh();
        if (AssetDatabase.AssetPathToGUID(MenuScenePath) == string.Empty || AssetDatabase.AssetPathToGUID(ScenePath) == string.Empty)
        {
            Debug.LogError("PA3_BUILD_SCENES_MISSING: save both PA3_MainMenu and PA3_VerticalSlice before updating Build Settings.");
            return;
        }
        ApplyPa3BuildSettings();
        AssetDatabase.DeleteAsset(Root + "/Prefabs/PA3_GameMenu.prefab");
        AssetDatabase.DeleteAsset("Assets/Scenes/menu.unity");
        Debug.Log("PA3_FLOW_CONFIGURED: PA3_MainMenu is the build entry; Jugar opens PA3_VerticalSlice; portal victory and guardian capture return to a fresh menu.");
    }

    static void ApplyPa3BuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }

    static void BuildMainMenuScene()
    {
        var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        var canvasObject = new GameObject("PA3_MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        HudElement("NightfallBackdrop", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.012f, 0.027f, 0.061f, 1f));
        HudElement("AuroraBand", canvasObject.transform, new Vector2(0f, 0.93f), Vector2.one, new Vector2(0f, 3f), Vector2.zero, new Color(0.12f, 0.76f, 0.88f, 0.75f));

        var main = CreateMenuPanel(canvasObject.transform, "MainMenuPanel", "ECOS DE AURELIA", "Una aventura de exploración y recolección.", new[] { "JUGAR", "CÓMO JUGAR", "SALIR" });
        var mainCard = main.panel.transform.Find("GlassCard");
        HudElement("CrestOuter", mainCard, new Vector2(0.5f, 0.97f), new Vector2(0.5f, 0.97f), new Vector2(44f, 44f), Vector2.zero, new Color(0.09f, 0.74f, 0.86f, 0.95f));
        HudElement("CrestCore", mainCard.Find("CrestOuter"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 18f), Vector2.zero, new Color(0.73f, 0.98f, 1f, 1f));
        var feedback = mainCard.Find("MenuSubtitle").GetComponent<Text>();

        var instructions = CreateMenuPanel(canvasObject.transform, "InstructionsPanel", "GUÍA DE EXPLORACIÓN", "Controles y objetivo", new[] { "VOLVER AL MENÚ" });
        var instructionCard = instructions.panel.transform.Find("GlassCard");
        instructions.buttons[0].GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -245f);
        CreateMenuText(instructionCard, "ControlsText", "W A S D  ·  Moverse\nESPACIO  ·  Saltar\n\nRecoge los 6 cristales de luz y llega al portal.\nEl Guardián te devolverá al menú si te alcanza.", 23, new Color(0.81f, 0.9f, 0.96f), new Vector2(0.5f, 0.49f), new Vector2(650f, 250f));

        var menuController = new GameObject("PA3_MainMenuController").AddComponent<PA3MenuController>();
        menuController.ConfigureMainMenu(main.panel, instructions.panel, main.buttons[0], main.buttons[1], instructions.buttons[0], main.buttons[2], feedback);
    }

    [MenuItem("PA3/Build Vertical Slice")]
    public static void Build() { BuildInternal(true); }

    [MenuItem("PA3/Build Vertical Slice", true)]
    static bool CanBuild() => AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null;

    [MenuItem("PA3/Refresh Prefabs Only")]
    public static void RefreshPrefabsOnly()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("PA3_PREFAB_REFRESH_SKIPPED: exit Play Mode first."); return; }
        EnsureFolders();
        var originalScene = SceneManager.GetActiveScene();
        var scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scratchScene);
            var materials = CreateMaterials();
            var root = new GameObject("PA3_PrefabAuthoring_Temporary");
            var player = CreatePlayer(root.transform, materials);
            BuildCollectibles(root.transform, materials);
            BuildEnemy(root.transform, player.transform, materials);
            BuildHazardAndExit(root.transform, player.transform, materials);
            CreateVfx(root.transform, materials);
            CreateNarrativeBeaconPrefab(root.transform, materials);
            CreatePortalPrefab(root.transform);
            CreateCameraRigPrefab();
            CreateLightingPrefabs(root.transform);
            AssetDatabase.SaveAssets();
            Debug.Log("PA3_PREFABS_REFRESHED_ONLY: player, enemy, camera rig, collectibles, triggers, beacon, VFX and directional-light prefabs updated; open scene was not saved or replaced.");
        }
        finally
        {
            if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
            if (scratchScene.IsValid() && scratchScene.isLoaded) EditorSceneManager.CloseScene(scratchScene, true);
        }
    }

    [InitializeOnLoadMethod]
    static void QueuePrefabRefreshOnce()
    {
        const string key = "PA3_PrefabKitRefresh_Final_20260922";
        if (EditorPrefs.GetBool(key, false)) return;
        EditorPrefs.SetBool(key, true);
        EditorApplication.delayCall += RefreshPrefabsOnly;
    }

    [MenuItem("PA3/Import downloaded Nature and Sky assets")]
    public static void ImportDownloadedAssets()
    {
        foreach (var package in new[] { NaturePackage, SkyPackage })
        {
            if (!File.Exists(package)) { Debug.LogError("PA3_ASSET_PACKAGE_NOT_FOUND: " + package); continue; }
            AssetDatabase.ImportPackage(package, false);
        }
        Debug.Log("PA3_ASSETS_IMPORTED: Nature and AllSky packages imported. Re-run Build Vertical Slice after import completes.");
    }

    [MenuItem("PA3/Create or Repair Crystal Collection HUD")]
    public static void CreateOrRepairCrystalHud()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("PA3_HUD_SKIPPED: exit Play Mode before editing the HUD."); return; }
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) { Debug.LogError("PA3_HUD_WRONG_SCENE: open PA3_VerticalSlice before creating the collection HUD."); return; }
        var manager = Object.FindAnyObjectByType<PA3GameManager>(FindObjectsInactive.Include);
        if (manager == null) { Debug.LogError("PA3_HUD_NO_MANAGER: PA3_GameManager was not found; no scene changes made."); return; }

        bool sceneWasAlreadyDirty = scene.isDirty;
        var hud = GameObject.Find("HUD_PA3");
        if (hud == null) hud = CreateCrystalHud();
        else RepairCrystalHudLayout(hud);
        var objective = hud.transform.Find("CollectionCard/ObjectiveText")?.GetComponent<Text>();
        var collectionTotal = hud.transform.Find("CollectionCard/CountValue")?.GetComponent<Text>();
        var status = hud.transform.Find("ObjectiveMessage/StatusText")?.GetComponent<Text>();
        var progress = hud.transform.Find("CollectionCard/ProgressTrack/CollectionProgressFill")?.GetComponent<Image>();
        if (objective == null || collectionTotal == null || status == null || progress == null)
        {
            Debug.LogError("PA3_HUD_INCOMPLETE: HUD_PA3 exists but its required controls are missing; existing object was left untouched.");
            return;
        }

        manager.objectiveText = objective; manager.collectionTotalText = collectionTotal; manager.statusText = status; manager.collectionProgress = progress;
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!sceneWasAlreadyDirty)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("PA3_HUD_CREATED_AND_SAVED: crystal counter, progress bar and collection objective connected to PA3_GameManager.");
        }
        else Debug.Log("PA3_HUD_CREATED_UNSAVED: HUD connected; scene already contained unsaved edits, so press Ctrl+S when ready.");
    }

    [MenuItem("PA3/Place Exit Portal at Level Goal")]
    public static void PlaceExitPortalAtLevelGoal()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("PA3_PORTAL_SKIPPED: exit Play Mode before placing the portal."); return; }
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) { Debug.LogError("PA3_PORTAL_WRONG_SCENE: open PA3_VerticalSlice first."); return; }

        const string portalPath = Root + "/Prefabs/PA3_ExitPortal.prefab";
        var portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(portalPath);
        if (portalPrefab == null) { Debug.LogError("PA3_PORTAL_PREFAB_MISSING: " + portalPath); return; }

        Vector3 goalPosition = new Vector3(30f, 0f, 22f);
        var portal = GameObject.Find("PA3_ExitPortal");
        if (portal == null)
        {
            portal = (GameObject)PrefabUtility.InstantiatePrefab(portalPrefab, scene);
            portal.transform.SetPositionAndRotation(goalPosition, Quaternion.Euler(0f, 180f, 0f));
        }

        var guardian = GameObject.Find("Guardian_Enemy_PREFAB_TEMPLATE");
        var guardianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/GuardianEnemy.prefab");
        if (guardian != null && guardianPrefab != null && PrefabUtility.GetCorrespondingObjectFromSource(guardian) == null)
        {
            Transform oldParent = guardian.transform.parent;
            Vector3 oldPosition = guardian.transform.position;
            Quaternion oldRotation = guardian.transform.rotation;
            Object.DestroyImmediate(guardian);
            guardian = (GameObject)PrefabUtility.InstantiatePrefab(guardianPrefab, scene);
            guardian.transform.SetParent(oldParent, true);
            guardian.transform.SetPositionAndRotation(oldPosition, oldRotation);
            guardian.name = "Guardian_Enemy";
            guardian.SetActive(true);
        }

        var unusedHazard = GameObject.Find("Hazard_Trigger_TEMPLATE");
        if (unusedHazard != null) Object.DestroyImmediate(unusedHazard);

        var exit = GameObject.Find("Exit_Trigger_TEMPLATE");
        if (exit != null)
        {
            exit.transform.SetPositionAndRotation(goalPosition + new Vector3(0f, 2f, 0f), Quaternion.identity);
            exit.transform.localScale = new Vector3(3f, 4f, 2.5f);
            var trigger = exit.GetComponent<BoxCollider>();
            if (trigger != null) trigger.isTrigger = true;
            var visual = exit.GetComponent<Renderer>();
            if (visual != null) visual.enabled = false;
            exit.SetActive(true);
        }
        else Debug.LogWarning("PA3_PORTAL_NO_EXIT_TRIGGER: portal placed, but Exit_Trigger_TEMPLATE was not found.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ApplyPa3BuildSettings();
        Debug.Log("PA3_PORTAL_PLACED: PA3_ExitPortal at (30, 0, 22), with active exit trigger. Removed the unused hazard instance and linked the guardian scene object to its prefab.");
    }

    static GameObject CreateCrystalHud()
    {
        var canvasObject = new GameObject("HUD_PA3", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;

        var card = HudElement("CollectionCard", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(600f, 152f), new Vector2(30f, -28f), new Color(0.025f, 0.075f, 0.12f, 0.91f));
        card.AddComponent<Outline>().effectColor = new Color(0.12f, 0.78f, 0.92f, 0.30f);
        HudElement("CardAccent", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 100f), new Vector2(0f, 0f), new Color(0.15f, 0.88f, 1f, 0.95f));

        var crystal = HudElement("CrystalIcon", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(27f, 27f), new Vector2(36f, 23f), new Color(0.25f, 0.94f, 1f, 1f));
        crystal.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, 45f);
        HudElement("CrystalCore", crystal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 10f), Vector2.zero, new Color(0.88f, 1f, 1f, 0.95f));

        HudText("CollectionLabel", card.transform, "CRISTALES REUNIDOS", 15, FontStyle.Bold, new Color(0.60f, 0.82f, 0.91f), new Vector2(0f, 0.5f), new Vector2(70f, 48f), new Vector2(300f, 25f), TextAnchor.MiddleLeft);
        HudText("ObjectiveText", card.transform, "00", 34, FontStyle.Bold, new Color(0.56f, 0.96f, 1f), new Vector2(0f, 0.5f), new Vector2(70f, 12f), new Vector2(300f, 44f), TextAnchor.MiddleLeft);
        HudText("CollectionHint", card.transform, "REÚNE LOS FRAGMENTOS Y ACTIVA EL PORTAL", 11, FontStyle.Normal, new Color(0.61f, 0.73f, 0.81f), new Vector2(0f, 0.5f), new Vector2(70f, -21f), new Vector2(360f, 22f), TextAnchor.MiddleLeft);

        HudElement("ProgressTrack", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(330f, 9f), new Vector2(76f, -52f), new Color(0.09f, 0.18f, 0.24f, 1f));
        var fill = HudElement("CollectionProgressFill", card.transform.Find("ProgressTrack"), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.84f, 0.98f, 1f));
        var fillImage = fill.GetComponent<Image>(); fillImage.type = Image.Type.Filled; fillImage.fillMethod = Image.FillMethod.Horizontal; fillImage.fillOrigin = (int)Image.OriginHorizontal.Left; fillImage.fillAmount = 0f;

        HudElement("CountDivider", card.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 88f), new Vector2(-144f, 0f), new Color(0.25f, 0.54f, 0.64f, 0.45f));
        HudText("CountCaption", card.transform, "META", 11, FontStyle.Bold, new Color(0.58f, 0.75f, 0.84f), new Vector2(1f, 0.5f), new Vector2(-72f, 20f), new Vector2(120f, 22f), TextAnchor.MiddleCenter);
        HudText("CountValue", card.transform, "06", 24, FontStyle.Bold, Color.white, new Vector2(1f, 0.5f), new Vector2(-72f, -13f), new Vector2(120f, 34f), TextAnchor.MiddleCenter);

        var message = HudElement("ObjectiveMessage", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(760f, 58f), new Vector2(0f, 42f), new Color(0.025f, 0.075f, 0.12f, 0.78f));
        message.AddComponent<Outline>().effectColor = new Color(0.12f, 0.78f, 0.92f, 0.22f);
        HudText("StatusText", message.transform, "Reúne los cristales de luz para abrir el portal", 19, FontStyle.Normal, new Color(0.80f, 0.94f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(710f, 48f), TextAnchor.MiddleCenter);
        return canvasObject;
    }

    static void RepairCrystalHudLayout(GameObject hud)
    {
        var card = hud.transform.Find("CollectionCard");
        if (card != null)
        {
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0f, 1f); cardRect.anchorMax = new Vector2(0f, 1f); cardRect.pivot = new Vector2(0f, 1f);
            cardRect.sizeDelta = new Vector2(600f, 152f); cardRect.anchoredPosition = new Vector2(30f, -28f);
            foreach (var rect in card.GetComponentsInChildren<RectTransform>(true))
                if (rect.anchorMin == rect.anchorMax) rect.pivot = rect.anchorMin;
        }
        var message = hud.transform.Find("ObjectiveMessage");
        if (message != null)
        {
            var messageRect = message.GetComponent<RectTransform>(); messageRect.pivot = new Vector2(0.5f, 0f); messageRect.anchoredPosition = new Vector2(0f, 42f);
        }
    }

    static GameObject HudElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, Color color)
    {
        var element = new GameObject(name, typeof(RectTransform), typeof(Image)); element.transform.SetParent(parent, false);
        var rect = element.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        if (anchorMin == anchorMax) rect.pivot = anchorMin;
        if (size != Vector2.zero) rect.sizeDelta = size;
        if (anchorMin != anchorMax) { rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        rect.anchoredPosition = position; element.GetComponent<Image>().color = color;
        return element;
    }

    static Text HudText(string name, Transform parent, string value, int size, FontStyle style, Color color, Vector2 anchor, Vector2 position, Vector2 dimensions, TextAnchor alignment)
    {
        var element = new GameObject(name, typeof(RectTransform), typeof(Text)); element.transform.SetParent(parent, false);
        var text = element.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = size; text.fontStyle = style; text.color = color; text.alignment = alignment; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        var rect = text.rectTransform; rect.anchorMin = anchor; rect.anchorMax = anchor; rect.sizeDelta = dimensions; rect.anchoredPosition = position;
        return text;
    }

    public static void BuildInternal(bool save)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Debug.LogWarning("PA3_BUILD_BLOCKED: the gameplay scene already exists. Build Vertical Slice would replace the user's terrain and scene objects.");
            return;
        }

        EnsureFolders();
        foreach (var prefab in new[] { "EnergyCrystal", "GuardianEnemy", "NarrativeBeacon", "CrystalMotesVFX", "Player_3D", "HazardTrigger", "ExitTrigger" }) AssetDatabase.DeleteAsset(Root + "/Prefabs/" + prefab + ".prefab");
        foreach (var material in new[] { "PA3_Ground", "PA3_Rock", "PA3_Accent", "PA3_Hazard", "PA3_DynamicPulse" }) AssetDatabase.DeleteAsset(Root + "/Materials/" + material + ".mat");
        AssetDatabase.DeleteAsset(Root + "/Materials/PA3_PostProcessing_Profile.asset");
        AssetDatabase.DeleteAsset(Root + "/Textures/GroundTexture.asset");
        AssetDatabase.DeleteAsset(Root + "/Timeline/PA3_Cinematic_7_5s.playable");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        staticObjects.Clear();

        var materials = CreateMaterials();
        ApplySkybox();
        var root = new GameObject("PA3_VerticalSlice");
        var environment = new GameObject("YOUR_TERRAIN_AND_LEVEL_DESIGN_HERE"); environment.transform.SetParent(root.transform);
        var vfx = new GameObject("VFX"); vfx.transform.SetParent(root.transform);

        CreateVfx(vfx.transform, materials);
        var mist = vfx.transform.Find("VFX_EnergyMist"); if (mist != null) mist.position = new Vector3(5, 1, 5);
        var player = CreatePlayer(root.transform, materials);
        CreateCamera(player.transform);
        BuildCollectibles(root.transform, materials);
        BuildEnemy(root.transform, player.transform, materials);
        BuildHazardAndExit(root.transform, player.transform, materials);
        BuildUi(root.transform, player.transform);
        BuildLightingAndPost(root.transform);
        BuildTimeline(root.transform, player.transform);
        Debug.Log("PA3_TERRAIN_TEMPLATE_READY: generated terrain/platforms/foliage omitted; add your own level design under YOUR_TERRAIN_AND_LEVEL_DESIGN_HERE");
        foreach (var go in staticObjects) if (go != null) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null) ApplyPa3BuildSettings();
        else EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (save) Debug.Log("PA3_BUILD_OK: vertical slice created at " + ScenePath);
    }

    static void EnsureFolders()
    {
        foreach (var folder in new[] { "Assets/Scenes", "Assets/PA3", "Assets/PA3/Materials", "Assets/PA3/Prefabs", "Assets/PA3/Timeline", "Assets/PA3/Textures", "Assets/PA3/Evidence", "Assets/PA3/Shaders", "Assets/PA3/Scripts", "Assets/PA3/Editor" })
        {
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, name);
        }
    }

    static void ApplySkybox()
    {
        const string path = "Assets/AllSkyFree/Cartoon Base BlueSky/Day_BlueSky_Nothing.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (sky == null) { Debug.LogWarning("PA3_SKYBOX_PENDING: import AllSky before rebuilding to enable the authored skybox"); return; }
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.85f;
        DynamicGI.UpdateEnvironment();
        Debug.Log("PA3_ALLSKY_ACTIVE: " + path);
    }

    static (Material ground, Material rock, Material accent, Material hazard) CreateMaterials()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material Make(string name, Color color, float metallic = 0f, float smooth = 0.35f)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = lit; m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m); return m;
        }
        return (null, Make("PA3_Rock", Rock, 0.05f, 0.5f), Make("PA3_Accent", Accent, 0.15f, 0.8f), Make("PA3_Hazard", new Color(0.95f, 0.16f, 0.12f), 0f, 0.25f));
    }

    static Material MakeArtMaterial(string name, Color color, float metallic = 0f, float smoothness = 0.35f, bool emissive = false)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        string path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
        material.shader = shader; material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (emissive && material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.5f); }
        EditorUtility.SetDirty(material);
        return material;
    }

    static Terrain CreateTerrain(Material ground)
    {
        var data = new TerrainData { heightmapResolution = 129, size = new Vector3(120, 22, 120), baseMapResolution = 512 };
        var heights = new float[129, 129];
        for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++)
        {
            float nx = x / 128f, nz = z / 128f;
            float ridge = Mathf.PerlinNoise(nx * 2.7f + 0.2f, nz * 2.7f + 0.4f) * 0.23f;
            float detail = Mathf.PerlinNoise(nx * 7.5f + 4f, nz * 7.5f + 9f) * 0.035f;
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.Min(Mathf.Min(nx, 1f - nx), Mathf.Min(nz, 1f - nz)) * 5f);
            float route = Mathf.Exp(-Mathf.Pow((nx - nz) * 7.5f, 2f));
            float rollingHill = (ridge + detail) * edge;
            heights[z, x] = Mathf.Clamp01(0.018f + rollingHill * (1f - route * 0.48f) + (nz > 0.72f ? (nz - 0.72f) * 0.45f : 0f));
        }
        data.SetHeights(0, 0, heights);
        var terrainGo = Terrain.CreateTerrainGameObject(data); terrainGo.name = "Terrain_PA3_Verticality";
        var terrain = terrainGo.GetComponent<Terrain>(); terrain.drawInstanced = true;
        const string nature = "Assets/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/";
        var grass = AssetDatabase.LoadAssetAtPath<TerrainLayer>(nature + "Ground_Layer_02.terrainlayer");
        var soil = AssetDatabase.LoadAssetAtPath<TerrainLayer>(nature + "Ground_Layer_01.terrainlayer");
        if (grass == null || soil == null)
        {
            var fallback = new TerrainLayer { diffuseTexture = CreateTexture("GroundTexture", Ground), tileSize = new Vector2(12, 12) };
            string layerPath = Root + "/Materials/PA3_GroundTerrain.terrainlayer";
            AssetDatabase.CreateAsset(fallback, layerPath); terrain.terrainData.terrainLayers = new[] { fallback };
        }
        else
        {
            var grassLayer = new TerrainLayer { diffuseTexture = grass.diffuseTexture, normalMapTexture = grass.normalMapTexture, tileSize = new Vector2(8, 8), tileOffset = grass.tileOffset };
            var soilLayer = new TerrainLayer { diffuseTexture = soil.diffuseTexture, normalMapTexture = soil.normalMapTexture, tileSize = new Vector2(7, 7), tileOffset = soil.tileOffset };
            AssetDatabase.CreateAsset(grassLayer, Root + "/Materials/PA3_Ground_Grass.terrainlayer");
            AssetDatabase.CreateAsset(soilLayer, Root + "/Materials/PA3_Ground_Soil.terrainlayer");
            terrain.terrainData.terrainLayers = new[] { grassLayer, soilLayer };
        }
        int layerCount = terrain.terrainData.terrainLayers.Length;
        var terrainWeights = new float[129, 129, layerCount];
        for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++)
        {
            float nx = x / 128f, nz = z / 128f;
            float slope = data.GetSteepness(nx, nz);
            float rock = layerCount > 1 ? Mathf.Clamp01(Mathf.InverseLerp(17f, 42f, slope) * 0.82f + Mathf.InverseLerp(0.30f, 0.82f, heights[z, x]) * 0.36f) : 0f;
            terrainWeights[z, x, 0] = 1f - rock;
            if (layerCount > 1) terrainWeights[z, x, 1] = rock;
        }
        data.SetAlphamaps(0, 0, terrainWeights);
        staticObjects.Add(terrainGo); return terrain;
    }

    static Texture2D CreateTexture(string name, Color color)
    {
        string path = Root + "/Textures/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] { color, color * 0.85f, color * 0.95f, color }); texture.Apply(); texture.name = name;
        AssetDatabase.CreateAsset(texture, path); return texture;
    }

    static void BuildStructures(Transform parent, (Material ground, Material rock, Material accent, Material hazard) m, Terrain terrain)
    {
        for (int i = 0; i < 8; i++)
        {
            float x = 10 + i * 13f; float z = 22 + Mathf.Sin(i * 1.5f) * 14f; float y = 2f + i * 0.4f;
            var block = Primitive("Platform_" + i, PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(8, 1.2f + i * 0.18f, 8), m.rock, parent);
            staticObjects.Add(block);
        }
        for (int i = 0; i < 14; i++)
        {
            float x = 8 + Mathf.Repeat(i * 17.7f, 105); float z = 8 + Mathf.Repeat(i * 29.3f, 100);
            float size = 1.4f + (i % 4) * 0.55f;
            float y = (terrain == null ? 0f : terrain.SampleHeight(new Vector3(x, 0f, z))) + size * 0.22f;
            var rock = Primitive("Rock_" + i, PrimitiveType.Sphere, new Vector3(x, y, z), Vector3.one * size, m.rock, parent);
            rock.transform.localScale = new Vector3(rock.transform.localScale.x, rock.transform.localScale.y * 0.7f, rock.transform.localScale.z);
            staticObjects.Add(rock);
        }
        var archL = Primitive("ExitArch_Left", PrimitiveType.Cube, new Vector3(105, 6, 105), new Vector3(2, 12, 2), m.accent, parent); staticObjects.Add(archL);
        var archR = Primitive("ExitArch_Right", PrimitiveType.Cube, new Vector3(115, 6, 105), new Vector3(2, 12, 2), m.accent, parent); staticObjects.Add(archR);
        var archTop = Primitive("ExitArch_Top", PrimitiveType.Cube, new Vector3(110, 12, 105), new Vector3(12, 2, 2), m.accent, parent); staticObjects.Add(archTop);
        var sign = Primitive("NarrativeBeacon", PrimitiveType.Cylinder, new Vector3(110, 4, 102), new Vector3(2, 4, 2), m.accent, parent);
        var pulseShader = Shader.Find("PA3/DynamicPulse");
        if (pulseShader != null)
        {
            var pulseMaterial = new Material(pulseShader) { name = "PA3_DynamicPulse_Material" };
            pulseMaterial.SetColor("_BaseColor", Accent); pulseMaterial.SetFloat("_PulseSpeed", 2.5f); pulseMaterial.SetFloat("_EmissionStrength", 3f);
            AssetDatabase.CreateAsset(pulseMaterial, Root + "/Materials/PA3_DynamicPulse.mat"); sign.GetComponent<Renderer>().sharedMaterial = pulseMaterial;
        }
        sign.AddComponent<DynamicShaderDriver>();
        CreatePrefab(sign, "NarrativeBeacon");
    }

    static GameObject CreatePlayer(Transform parent, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var player = new GameObject("Player_3D"); player.tag = "Player"; player.transform.SetParent(parent); player.transform.position = new Vector3(0, 1, 0);
        var cc = player.AddComponent<CharacterController>(); cc.height = 2.15f; cc.radius = 0.4f; cc.center = new Vector3(0, 1.08f, 0); cc.slopeLimit = 48f;
        Material suit = MakeArtMaterial("PA3_Player_Suit", new Color(0.045f, 0.13f, 0.22f), 0.18f, 0.48f);
        Material armor = MakeArtMaterial("PA3_Player_Armor", new Color(0.08f, 0.43f, 0.49f), 0.32f, 0.58f);
        Material trim = MakeArtMaterial("PA3_Player_Trim", new Color(0.94f, 0.57f, 0.18f), 0.48f, 0.62f);
        Material skin = MakeArtMaterial("PA3_Player_Skin", new Color(0.76f, 0.48f, 0.31f), 0f, 0.32f);
        Material glow = MakeArtMaterial("PA3_Player_Glow", new Color(0.12f, 0.86f, 1f), 0.12f, 0.72f, true);
        GameObject Part(string name, PrimitiveType shape, Vector3 localPosition, Vector3 scale, Material material)
        {
            var part = Primitive(name, shape, Vector3.zero, scale, material, player.transform);
            part.transform.localPosition = localPosition;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }
        Part("Player_Undersuit", PrimitiveType.Capsule, new Vector3(0, 1.18f, 0), new Vector3(0.56f, 0.72f, 0.42f), suit);
        Part("Player_ChestArmor", PrimitiveType.Cube, new Vector3(0, 1.42f, 0.23f), new Vector3(0.48f, 0.46f, 0.20f), armor);
        Part("Player_ChestBadge", PrimitiveType.Sphere, new Vector3(0, 1.43f, 0.35f), new Vector3(0.19f, 0.21f, 0.09f), glow);
        Part("Player_Belt", PrimitiveType.Cube, new Vector3(0, 0.93f, 0.04f), new Vector3(0.51f, 0.13f, 0.42f), trim);
        Part("Player_Head", PrimitiveType.Sphere, new Vector3(0, 2.0f, 0), new Vector3(0.38f, 0.42f, 0.36f), skin);
        Part("Player_Helmet", PrimitiveType.Sphere, new Vector3(0, 2.18f, -0.01f), new Vector3(0.47f, 0.26f, 0.43f), armor);
        Part("Player_Visor", PrimitiveType.Cube, new Vector3(0, 2.04f, 0.31f), new Vector3(0.34f, 0.12f, 0.08f), glow);
        Part("Player_Backpack", PrimitiveType.Cube, new Vector3(0, 1.36f, -0.34f), new Vector3(0.48f, 0.53f, 0.25f), trim);
        Part("Player_LeftShoulder", PrimitiveType.Sphere, new Vector3(-0.43f, 1.54f, 0), new Vector3(0.29f, 0.28f, 0.32f), armor);
        Part("Player_RightShoulder", PrimitiveType.Sphere, new Vector3(0.43f, 1.54f, 0), new Vector3(0.29f, 0.28f, 0.32f), armor);
        var leftArm = Part("Player_LeftArm", PrimitiveType.Capsule, new Vector3(-0.48f, 1.23f, 0), new Vector3(0.2f, 0.42f, 0.2f), suit); leftArm.transform.localRotation = Quaternion.Euler(0, 0, -10);
        var rightArm = Part("Player_RightArm", PrimitiveType.Capsule, new Vector3(0.48f, 1.23f, 0.04f), new Vector3(0.2f, 0.42f, 0.2f), suit); rightArm.transform.localRotation = Quaternion.Euler(0, 0, 10);
        Part("Player_LeftGlove", PrimitiveType.Sphere, new Vector3(-0.51f, 0.88f, 0.02f), new Vector3(0.22f, 0.18f, 0.2f), trim);
        Part("Player_RightGauntlet", PrimitiveType.Sphere, new Vector3(0.51f, 0.89f, 0.08f), new Vector3(0.24f, 0.22f, 0.24f), glow);
        Part("Player_LeftLeg", PrimitiveType.Capsule, new Vector3(-0.18f, 0.47f, 0), new Vector3(0.23f, 0.34f, 0.23f), suit);
        Part("Player_RightLeg", PrimitiveType.Capsule, new Vector3(0.18f, 0.47f, 0), new Vector3(0.23f, 0.34f, 0.23f), suit);
        Part("Player_LeftBoot", PrimitiveType.Cube, new Vector3(-0.18f, 0.16f, 0.09f), new Vector3(0.28f, 0.22f, 0.40f), trim);
        Part("Player_RightBoot", PrimitiveType.Cube, new Vector3(0.18f, 0.16f, 0.09f), new Vector3(0.28f, 0.22f, 0.40f), trim);
        var trailObject = new GameObject("Player_EnergyTrail"); trailObject.transform.SetParent(player.transform, false); trailObject.transform.localPosition = new Vector3(0f, 0.14f, -0.42f);
        var trail = trailObject.AddComponent<TrailRenderer>(); trail.time = 0.42f; trail.minVertexDistance = 0.08f; trail.startWidth = 0.26f; trail.endWidth = 0f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.22f, 1f), new Keyframe(1f, 0f));
        var trailGradient = new Gradient(); trailGradient.SetKeys(new[] { new GradientColorKey(new Color(0.1f, 0.92f, 1f), 0f), new GradientColorKey(new Color(0.48f, 0.3f, 1f), 1f) }, new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.55f, 0.65f), new GradientAlphaKey(0f, 1f) }); trail.colorGradient = trailGradient;
        trail.alignment = LineAlignment.View; trail.textureMode = LineTextureMode.Stretch; trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false;
        trail.material = glow; trail.emitting = true;
        var spawn = new GameObject("DefaultSpawnPoint"); spawn.transform.SetParent(player.transform, false); spawn.transform.localPosition = Vector3.zero;
        player.AddComponent<PlayerController3D>();
        player.GetComponent<PlayerController3D>().spawnPoint = spawn.transform;
        CreatePrefab(player, "Player_3D"); return player;
    }

    static void CreateCamera(Transform target)
    {
        var cameraGo = new GameObject("Main Camera"); cameraGo.tag = "MainCamera"; cameraGo.transform.position = target.position + new Vector3(0, 5, -7); cameraGo.transform.LookAt(target.position + Vector3.up * 1.2f); cameraGo.AddComponent<Camera>(); cameraGo.AddComponent<AudioListener>(); var follow = cameraGo.AddComponent<CameraFollow3D>(); follow.target = target; follow.offset = new Vector3(0, 4, -6);
        var cine = new GameObject("Cinemachine_CinematicCamera"); cine.transform.SetParent(cameraGo.transform); cine.transform.localPosition = Vector3.zero;
        var type = Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        if (type != null)
        {
            cine.AddComponent(type);
            var followProperty = type.GetProperty("Follow"); followProperty?.SetValue(cine.GetComponent(type), target);
            var lookAt = type.GetProperty("LookAt"); lookAt?.SetValue(cine.GetComponent(type), target);
        }
        else Debug.LogWarning("PA3_CINEMACHINE_PENDING: CinemachineCamera type was not found");
    }

    static void CreateCameraRigPrefab()
    {
        var rig = new GameObject("PA3_CameraRig");
        var cameraGo = new GameObject("Main Camera"); cameraGo.tag = "MainCamera"; cameraGo.transform.SetParent(rig.transform, false); cameraGo.transform.localPosition = new Vector3(0, 4, -6); cameraGo.transform.localRotation = Quaternion.Euler(18, 0, 0); cameraGo.AddComponent<Camera>(); cameraGo.AddComponent<AudioListener>();
        var follow = cameraGo.AddComponent<CameraFollow3D>(); follow.offset = new Vector3(0, 4, -6); follow.lookHeight = 1.1f;
        var cine = new GameObject("Cinemachine_Camera"); cine.transform.SetParent(cameraGo.transform, false);
        var type = Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        if (type != null) cine.AddComponent(type);
        CreatePrefab(rig, "PA3_CameraRig");
    }

    static (GameObject panel, Button[] buttons) CreateMenuPanel(Transform parent, string name, string title, string subtitle, string[] labels)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false);
        var panelRect = panel.GetComponent<RectTransform>(); panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one; panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.075f, 0.88f);

        var card = new GameObject("GlassCard", typeof(RectTransform), typeof(Image)); card.transform.SetParent(panel.transform, false);
        var cardRect = card.GetComponent<RectTransform>(); cardRect.anchorMin = new Vector2(0.5f, 0.5f); cardRect.anchorMax = new Vector2(0.5f, 0.5f); cardRect.sizeDelta = new Vector2(760, 700);
        card.GetComponent<Image>().color = new Color(0.035f, 0.10f, 0.16f, 0.97f);

        CreateMenuText(card.transform, "MenuTitle", title, 44, new Color(0.62f, 0.93f, 1f), new Vector2(0.5f, 0.84f), new Vector2(680, 80));
        CreateMenuText(card.transform, "MenuSubtitle", subtitle, 22, new Color(0.78f, 0.86f, 0.92f), new Vector2(0.5f, 0.73f), new Vector2(650, 64));

        var buttons = new Button[labels.Length];
        float firstY = labels.Length == 4 ? 100f : 60f;
        for (int i = 0; i < labels.Length; i++)
        {
            var buttonObject = new GameObject("Button_" + labels[i].Replace(' ', '_'), typeof(RectTransform), typeof(Image), typeof(Button)); buttonObject.transform.SetParent(card.transform, false);
            var rect = buttonObject.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(420, 64); rect.anchoredPosition = new Vector2(0, firstY - i * 88f);
            var image = buttonObject.GetComponent<Image>(); image.color = new Color(0.045f, 0.27f, 0.37f, 1f);
            var button = buttonObject.GetComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors; colors.normalColor = new Color(0.045f, 0.27f, 0.37f); colors.highlightedColor = new Color(0.08f, 0.49f, 0.60f); colors.pressedColor = new Color(0.10f, 0.72f, 0.80f); colors.selectedColor = colors.highlightedColor; button.colors = colors;
            CreateMenuText(buttonObject.transform, "Label", labels[i], 24, Color.white, new Vector2(0.5f, 0.5f), new Vector2(400, 56));
            buttons[i] = button;
        }
        panel.SetActive(false);
        return (panel, buttons);
    }

    static Text CreateMenuText(Transform parent, string name, string value, int fontSize, Color color, Vector2 anchor, Vector2 size)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = TextAnchor.MiddleCenter; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        var rect = text.rectTransform; rect.anchorMin = anchor; rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero;
        return text;
    }

    static void CreateNarrativeBeaconPrefab(Transform parent, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var root = new GameObject("NarrativeBeacon"); root.transform.SetParent(parent, false);
        var glow = MakeArtMaterial("PA3_Beacon_Glow", new Color(0.12f, 0.84f, 1f), 0.15f, 0.72f, true);
        var metal = MakeArtMaterial("PA3_Beacon_Metal", new Color(0.15f, 0.23f, 0.33f), 0.68f, 0.62f);
        GameObject Part(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var part = Primitive(name, shape, Vector3.zero, scale, material, root.transform); part.transform.localPosition = position;
            var collider = part.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
            return part;
        }
        Part("Beacon_Foot", PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(0.82f, 0.12f, 0.82f), metal);
        Part("Beacon_Pylon", PrimitiveType.Cylinder, new Vector3(0, 0.72f, 0), new Vector3(0.34f, 0.62f, 0.34f), metal);
        Part("Beacon_Rune", PrimitiveType.Cube, new Vector3(0, 1.35f, 0), new Vector3(0.58f, 0.28f, 0.18f), glow);
        var core = Part("Beacon_Core", PrimitiveType.Sphere, new Vector3(0, 1.78f, 0), new Vector3(0.62f, 0.7f, 0.62f), glow);
        var driver = core.AddComponent<DynamicShaderDriver>(); driver.firstColor = new Color(0.1f, 0.8f, 1f); driver.secondColor = new Color(0.9f, 0.32f, 0.1f);
        CreatePrefab(root, "NarrativeBeacon");
    }

    static void CreatePortalPrefab(Transform parent)
    {
        var root = new GameObject("PA3_ExitPortal"); root.transform.SetParent(parent, false);
        var frame = MakeArtMaterial("PA3_Portal_Frame", new Color(0.07f, 0.22f, 0.3f), 0.52f, 0.66f);
        var light = MakeArtMaterial("PA3_Portal_Light", new Color(0.08f, 0.8f, 1f), 0.12f, 0.76f, true);
        GameObject Part(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var part = Primitive(name, shape, Vector3.zero, scale, material, root.transform); part.transform.localPosition = position;
            var collider = part.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
            return part;
        }
        Part("Portal_LeftPillar", PrimitiveType.Cube, new Vector3(-1.25f, 2f, 0), new Vector3(0.32f, 4f, 0.42f), frame);
        Part("Portal_RightPillar", PrimitiveType.Cube, new Vector3(1.25f, 2f, 0), new Vector3(0.32f, 4f, 0.42f), frame);
        Part("Portal_Lintel", PrimitiveType.Cube, new Vector3(0, 4.05f, 0), new Vector3(2.85f, 0.38f, 0.48f), frame);
        Part("Portal_LeftRunicStrip", PrimitiveType.Cube, new Vector3(-1.25f, 2f, 0.24f), new Vector3(0.09f, 3.3f, 0.08f), light);
        Part("Portal_RightRunicStrip", PrimitiveType.Cube, new Vector3(1.25f, 2f, 0.24f), new Vector3(0.09f, 3.3f, 0.08f), light);
        Part("Portal_Crown", PrimitiveType.Sphere, new Vector3(0, 4.35f, 0), new Vector3(0.58f, 0.58f, 0.34f), light);
        CreatePrefab(root, "PA3_ExitPortal");
    }

    static void CreateLightingPrefabs(Transform parent)
    {
        var daylight = new GameObject("PA3_DirectionalLight"); daylight.transform.SetParent(parent, false);
        daylight.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);
        var realtime = daylight.AddComponent<Light>(); realtime.type = LightType.Directional; realtime.color = new Color(1f, 0.91f, 0.76f); realtime.intensity = 1.1f;
        realtime.lightmapBakeType = LightmapBakeType.Realtime; realtime.shadows = LightShadows.Soft; realtime.shadowStrength = 0.72f;
        CreatePrefab(daylight, "PA3_DirectionalLight");

        var rig = new GameObject("PA3_MixedLightingRig"); rig.transform.SetParent(parent, false);
        AddDirectionalLight(rig.transform, "Sun - Baked", new Color(1f, 0.82f, 0.62f), 1.15f, new Vector3(48f, -32f, 0f), LightmapBakeType.Baked, true);
        AddDirectionalLight(rig.transform, "Sky Fill - Realtime", new Color(0.36f, 0.57f, 1f), 0.28f, new Vector3(24f, 148f, 0f), LightmapBakeType.Realtime, false);
        CreatePrefab(rig, "PA3_MixedLightingRig");
    }

    static Light AddDirectionalLight(Transform parent, string name, Color color, float intensity, Vector3 euler, LightmapBakeType bakeType, bool castShadows)
    {
        var lightObject = new GameObject(name); lightObject.transform.SetParent(parent, false); lightObject.transform.localRotation = Quaternion.Euler(euler);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.color = color; light.intensity = intensity;
        light.lightmapBakeType = bakeType; light.shadows = castShadows ? LightShadows.Soft : LightShadows.None; light.shadowStrength = 0.72f;
        return light;
    }

    static void BuildCollectibles(Transform parent, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var source = new GameObject("Crystal_Source"); source.transform.SetParent(parent, false);
        var trigger = source.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = 0.8f; trigger.center = new Vector3(0, 0.9f, 0);
        var gem = MakeArtMaterial("PA3_Crystal_Glow", new Color(0.12f, 0.8f, 1f), 0.22f, 0.82f, true);
        var frame = MakeArtMaterial("PA3_Crystal_Frame", new Color(0.08f, 0.19f, 0.29f), 0.74f, 0.68f);
        GameObject Part(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material, Vector3 rotation)
        {
            var part = Primitive(name, shape, Vector3.zero, scale, material, source.transform); part.transform.localPosition = position; part.transform.localRotation = Quaternion.Euler(rotation);
            var collider = part.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
            return part;
        }
        Part("Crystal_Obsidian_Setting", PrimitiveType.Cylinder, new Vector3(0, 0.55f, 0), new Vector3(0.68f, 0.1f, 0.68f), frame, Vector3.zero);
        Part("Crystal_Core", PrimitiveType.Cylinder, new Vector3(0, 1.0f, 0), new Vector3(0.38f, 0.52f, 0.38f), gem, new Vector3(0, 0, 8));
        Part("Crystal_Crown", PrimitiveType.Sphere, new Vector3(0, 1.58f, 0), new Vector3(0.27f, 0.4f, 0.27f), gem, Vector3.zero);
        Part("Crystal_Bracelet", PrimitiveType.Cylinder, new Vector3(0, 0.63f, 0), new Vector3(0.48f, 0.055f, 0.48f), m.accent, Vector3.zero);
        source.AddComponent<Collectible>(); CreatePrefab(source, "EnergyCrystal");
        var positions = new[] { new Vector3(5, 1.5f, 5), new Vector3(9, 1.5f, 5), new Vector3(13, 1.5f, 8), new Vector3(17, 1.5f, 12), new Vector3(22, 1.5f, 15), new Vector3(27, 1.5f, 20) };
        for (int i = 0; i < positions.Length; i++) { GameObject c = i == 0 ? source : Object.Instantiate(source, parent); c.name = "EnergyCrystal_" + (i + 1); c.transform.position = positions[i]; }
    }

    static void BuildEnemy(Transform parent, Transform player, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var enemy = new GameObject("Guardian_Enemy_PREFAB_TEMPLATE"); enemy.transform.SetParent(parent, false); enemy.transform.position = new Vector3(0, 1, 8);
        var collider = enemy.AddComponent<CapsuleCollider>(); collider.isTrigger = true; collider.radius = 0.62f; collider.height = 2.7f; collider.center = new Vector3(0, 1.35f, 0);
        Material armor = MakeArtMaterial("PA3_Enemy_Armor", new Color(0.19f, 0.045f, 0.075f), 0.38f, 0.52f);
        Material shell = MakeArtMaterial("PA3_Enemy_Shell", new Color(0.43f, 0.1f, 0.1f), 0.22f, 0.45f);
        Material bone = MakeArtMaterial("PA3_Enemy_Bone", new Color(0.75f, 0.59f, 0.37f), 0.1f, 0.38f);
        Material eye = MakeArtMaterial("PA3_Enemy_Eyes", new Color(1f, 0.18f, 0.025f), 0.05f, 0.8f, true);
        GameObject Part(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material, Vector3 rotation)
        {
            var part = Primitive(name, shape, Vector3.zero, scale, material, enemy.transform); part.transform.localPosition = position; part.transform.localRotation = Quaternion.Euler(rotation);
            var childCollider = part.GetComponent<Collider>(); if (childCollider != null) Object.DestroyImmediate(childCollider);
            return part;
        }
        Part("Guardian_Body", PrimitiveType.Capsule, new Vector3(0, 1.15f, 0), new Vector3(0.78f, 0.76f, 0.62f), armor, Vector3.zero);
        Part("Guardian_Breastplate", PrimitiveType.Sphere, new Vector3(0, 1.35f, 0.27f), new Vector3(0.67f, 0.6f, 0.42f), shell, Vector3.zero);
        Part("Guardian_Core", PrimitiveType.Sphere, new Vector3(0, 1.36f, 0.52f), new Vector3(0.25f, 0.28f, 0.12f), eye, Vector3.zero);
        Part("Guardian_Head", PrimitiveType.Sphere, new Vector3(0, 2.08f, 0.04f), new Vector3(0.56f, 0.52f, 0.52f), armor, Vector3.zero);
        Part("Guardian_Mask", PrimitiveType.Cube, new Vector3(0, 2.03f, 0.31f), new Vector3(0.48f, 0.22f, 0.24f), bone, Vector3.zero);
        Part("Guardian_LeftEye", PrimitiveType.Sphere, new Vector3(-0.15f, 2.09f, 0.44f), new Vector3(0.13f, 0.12f, 0.07f), eye, Vector3.zero);
        Part("Guardian_RightEye", PrimitiveType.Sphere, new Vector3(0.15f, 2.09f, 0.44f), new Vector3(0.13f, 0.12f, 0.07f), eye, Vector3.zero);
        Part("Guardian_LeftHorn", PrimitiveType.Capsule, new Vector3(-0.31f, 2.49f, 0.02f), new Vector3(0.15f, 0.3f, 0.15f), bone, new Vector3(0, 0, -28));
        Part("Guardian_RightHorn", PrimitiveType.Capsule, new Vector3(0.31f, 2.49f, 0.02f), new Vector3(0.15f, 0.3f, 0.15f), bone, new Vector3(0, 0, 28));
        Part("Guardian_LeftShoulder", PrimitiveType.Sphere, new Vector3(-0.53f, 1.55f, 0), new Vector3(0.4f, 0.36f, 0.42f), shell, Vector3.zero);
        Part("Guardian_RightShoulder", PrimitiveType.Sphere, new Vector3(0.53f, 1.55f, 0), new Vector3(0.4f, 0.36f, 0.42f), shell, Vector3.zero);
        var script = enemy.AddComponent<EnemyChaser>(); script.target = null;
        CreatePrefab(enemy, "GuardianEnemy");
        script.target = player;
        enemy.SetActive(true);
    }

    static void BuildHazardAndExit(Transform parent, Transform player, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        Material hazardVisual = MakeArtMaterial("PA3_Hazard_Glow", new Color(1f, 0.13f, 0.025f), 0.08f, 0.62f, true);
        var hazard = Primitive("Hazard_Trigger_TEMPLATE", PrimitiveType.Cube, new Vector3(12, 0.15f, 12), new Vector3(4, 0.3f, 4), hazardVisual, parent); hazard.GetComponent<BoxCollider>().isTrigger = true; hazard.AddComponent<HazardTrigger>(); CreatePrefab(hazard, "HazardTrigger"); hazard.SetActive(false);
        var exit = Primitive("Exit_Trigger_TEMPLATE", PrimitiveType.Cube, new Vector3(30, 2, 22), new Vector3(3, 4, 3), m.accent, parent); exit.GetComponent<Renderer>().enabled = false; exit.GetComponent<BoxCollider>().isTrigger = true; exit.AddComponent<ExitTrigger>(); CreatePrefab(exit, "ExitTrigger"); exit.SetActive(false);
        var spawn = new GameObject("PlayerSpawn"); spawn.transform.SetParent(parent); spawn.transform.position = new Vector3(0, 1, 0); player.GetComponent<PlayerController3D>().spawnPoint = spawn.transform;
    }

    static void CreateVfx(Transform parent, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var go = new GameObject("VFX_CrystalMotes");
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.38f, 0.9f, 1f, 0.88f));
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.25f, 2.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.07f, 0.34f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.19f);
        main.maxParticles = 96;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 14f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 1.65f;
        shape.radiusThickness = 0.18f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.42f;
        noise.scrollSpeed = 0.12f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.damping = true;

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.35f, 0.95f, 1f), 0f), new GradientColorKey(new Color(0.67f, 0.55f, 1f), 0.55f), new GradientColorKey(new Color(1f, 0.83f, 0.52f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.92f, 0.16f), new GradientAlphaKey(0.72f, 0.72f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(0.22f, 1f), new Keyframe(0.76f, 0.72f), new Keyframe(1f, 0f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = m.accent;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        CreatePrefab(go, "CrystalMotesVFX");
    }

    static void BuildNatureDressing(Transform parent, Terrain terrain)
    {
        string root = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/";
        var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material foliage = MakeNatureMaterial("PA3_Foliage_URP", new Color(0.12f, 0.38f, 0.16f), lit);
        Material foliageLight = MakeNatureMaterial("PA3_FoliageLight_URP", new Color(0.31f, 0.57f, 0.20f), lit);
        Material bark = MakeNatureMaterial("PA3_Bark_URP", new Color(0.31f, 0.18f, 0.10f), lit);
        Material stone = MakeNatureMaterial("PA3_Stone_URP", new Color(0.37f, 0.40f, 0.36f), lit);
        var choices = new[] { root + "Trees/PT_Pine_Tree_03_green.prefab", root + "Trees/PT_Pine_Tree_03_green_cut.prefab", root + "Trees/PT_Fruit_Tree_01_pears.prefab", root + "Trees/PT_Fruit_Tree_01_plums.prefab", root + "Rocks/PT_Menhir_Rock_02.prefab", root + "Rocks/PT_Generic_Rock_01.prefab" };
        var prefabs = new GameObject[choices.Length];
        for (int i = 0; i < choices.Length; i++) prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(choices[i]);
        int placed = 0;
        var random = new System.Random(27031);
        var treePositions = new List<Vector2>();
        for (int i = 0; i < 200 && placed < 38; i++)
        {
            float x = 4f + (float)random.NextDouble() * 112f, z = 4f + (float)random.NextDouble() * 112f;
            if ((x < 28 && z < 29) || (x > 96 && z > 93) || Mathf.Abs(x - z) < 6f) continue;
            if (new Vector2(x,z).sqrMagnitude < 1f) continue;
            if (treePositions.Exists(p => Vector2.Distance(p, new Vector2(x,z)) < 7f)) continue;
            GameObject prefab = prefabs[i % 3]; if (prefab == null) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Nature_Dressing_" + placed.ToString("00"); instance.transform.SetParent(parent);
            float groundY = terrain == null ? 0f : terrain.SampleHeight(new Vector3(x, 0f, z));
            instance.transform.SetPositionAndRotation(new Vector3(x, groundY, z), Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
            float scale = i % 4 >= 2 ? 0.7f + (i % 4) * 0.12f : 0.85f + (i % 5) * 0.1f;
            instance.transform.localScale = Vector3.one * scale; treePositions.Add(new Vector2(x,z)); placed++;
            foreach (var mesh in instance.GetComponentsInChildren<Renderer>(true))
            {
                var replacements = new Material[mesh.sharedMaterials.Length];
                for (int slot = 0; slot < replacements.Length; slot++)
                {
                    string oldName = mesh.sharedMaterials[slot] == null ? "" : mesh.sharedMaterials[slot].name.ToLowerInvariant();
                    replacements[slot] = choices[i % choices.Length].Contains("Rocks/") ? stone
                        : oldName.Contains("trunk") || oldName.Contains("bark") || oldName.Contains("wood") ? bark
                        : oldName.Contains("leaf") || oldName.Contains("foliage") ? (i % 2 == 0 ? foliage : foliageLight) : foliage;
                }
                mesh.sharedMaterials = replacements;
            }
        }
        Debug.Log("PA3_NATURE_PREFABS: " + placed + " objects placed from Low Poly Environment - Nature Free");

        string helpers = "Assets/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/";
        var undergrowth = new[] { "PT_Grass_02_v2.prefab", "PT_High_Grass_02_v1.prefab", "PT_Poppy_02_v1.prefab" };
        var groundCover = new GameObject[undergrowth.Length];
        for (int i = 0; i < undergrowth.Length; i++) groundCover[i] = AssetDatabase.LoadAssetAtPath<GameObject>(helpers + undergrowth[i]);
        int coverPlaced = 0;
        for (int i = 0; i < 88; i++)
        {
            float x = 4f + Mathf.Repeat(i * 31.77f + 12f, 112f), z = 4f + Mathf.Repeat(i * 19.43f + 31f, 112f);
            if ((x < 29f && z < 30f) || (x > 96f && z > 93f)) continue;
            float diagonal = Mathf.Abs(x - z);
            bool onRoute = diagonal < 5.8f;
            bool nearCrystal = Vector2.Distance(new Vector2(x,z), new Vector2(18,18)) < 4.5f || Vector2.Distance(new Vector2(x,z), new Vector2(37,27)) < 4.5f || Vector2.Distance(new Vector2(x,z), new Vector2(54,20)) < 4.5f || Vector2.Distance(new Vector2(x,z), new Vector2(69,42)) < 4.5f || Vector2.Distance(new Vector2(x,z), new Vector2(86,68)) < 4.5f || Vector2.Distance(new Vector2(x,z), new Vector2(103,92)) < 4.5f;
            if ((onRoute && i % 3 != 0) || nearCrystal) continue;
            GameObject prefab = groundCover[i % groundCover.Length]; if (prefab == null) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Groundcover_" + coverPlaced.ToString("00"); instance.transform.SetParent(parent);
            float y = terrain == null ? 0f : terrain.SampleHeight(new Vector3(x, 0f, z));
            instance.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, Mathf.Repeat(i * 91.7f, 360f), 0f));
            float size = i % 3 == 2 ? 1.05f : 0.98f + (i % 4) * 0.12f;
            instance.transform.localScale = Vector3.one * size;
            foreach (var mesh in instance.GetComponentsInChildren<Renderer>(true))
            {
                var replacements = new Material[mesh.sharedMaterials.Length];
                for (int slot = 0; slot < replacements.Length; slot++) replacements[slot] = i % 3 == 2 ? foliageLight : foliage;
                mesh.sharedMaterials = replacements;
            }
            coverPlaced++;
        }
        Debug.Log("PA3_GROUNDCOVER_PREFABS: " + coverPlaced + " grass and flower clumps placed from Environment Free");
    }

    static Material MakeNatureMaterial(string name, Color tint, Shader shader)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader; material.name = name; material.color = tint;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void BuildUi(Transform parent, Transform player)
    {
        var canvasGo = new GameObject("HUD_PA3"); canvasGo.transform.SetParent(parent); var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvasGo.AddComponent<CanvasScaler>(); canvasGo.AddComponent<GraphicRaycaster>();
        Text MakeText(string name, string value, Vector2 anchor, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(canvasGo.transform, false); var text = go.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = TextAnchor.MiddleCenter; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; var rt = text.rectTransform; rt.anchorMin = anchor; rt.anchorMax = anchor; rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero; return text;
        }
        var objective = MakeText("ObjectiveText", "CRISTALES  0/6", new Vector2(0.13f, 0.92f), new Vector2(300, 60), 28, Color.white);
        var status = MakeText("StatusText", "Recupera los cristales de energía", new Vector2(0.5f, 0.08f), new Vector2(700, 60), 24, new Color(0.7f, 0.95f, 1f));
        var title = MakeText("TitleText", "VALLE DE LA ÚLTIMA LUZ", new Vector2(0.5f, 0.95f), new Vector2(800, 60), 32, new Color(0.8f, 0.9f, 1f));
        var manager = new GameObject("PA3_GameManager"); manager.transform.SetParent(parent); var gm = manager.AddComponent<PA3GameManager>(); gm.objectiveText = objective; gm.statusText = status;
    }

    static void BuildLightingAndPost(Transform parent)
    {
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.012f; RenderSettings.fogColor = new Color(0.12f, 0.18f, 0.25f); RenderSettings.ambientLight = new Color(0.12f, 0.18f, 0.24f);
        var baked = new GameObject("Sun_BAKED"); baked.transform.SetParent(parent); baked.transform.rotation = Quaternion.Euler(45, -30, 0); var bakedLight = baked.AddComponent<Light>(); bakedLight.type = LightType.Directional; bakedLight.intensity = 1.2f; bakedLight.color = new Color(1f, 0.8f, 0.62f); bakedLight.lightmapBakeType = LightmapBakeType.Baked;
        var realtime = new GameObject("Moon_REALTIME"); realtime.transform.SetParent(parent); realtime.transform.rotation = Quaternion.Euler(25, 150, 0); var realLight = realtime.AddComponent<Light>(); realLight.type = LightType.Directional; realLight.intensity = 0.35f; realLight.color = new Color(0.25f, 0.45f, 1f); realLight.lightmapBakeType = LightmapBakeType.Realtime;
        var volumeGo = new GameObject("URP_PostProcessing_Volume"); volumeGo.transform.SetParent(parent); var volume = volumeGo.AddComponent<Volume>(); volume.isGlobal = true; var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "PA3_PostProcessing_Profile"; AssetDatabase.CreateAsset(profile, Root + "/Materials/PA3_PostProcessing_Profile.asset"); volume.sharedProfile = profile; var bloom = profile.Add<Bloom>(); bloom.intensity.value = 0.35f; bloom.threshold.value = 0.8f; var color = profile.Add<ColorAdjustments>(); color.postExposure.value = 0.35f; color.contrast.value = 12f; color.colorFilter.value = new Color(0.85f, 0.95f, 1f);
    }

    static void BuildTimeline(Transform parent, Transform player)
    {
        var camera = Camera.main; if (camera == null) camera = Object.FindAnyObjectByType<Camera>();
        var directorGo = new GameObject("Cinematic_Director"); directorGo.transform.SetParent(parent); var director = directorGo.AddComponent<PlayableDirector>(); var auto = directorGo.AddComponent<CinematicAutoPlay>(); auto.director = director;
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>(); timeline.name = "PA3_Cinematic_7_5s"; AssetDatabase.CreateAsset(timeline, Root + "/Timeline/PA3_Cinematic_7_5s.playable"); director.playableAsset = timeline; director.playOnAwake = false; director.extrapolationMode = DirectorWrapMode.None;
        var anim = new AnimationClip { name = "PA3_CinematicCameraMove_7_5s", frameRate = 30f, legacy = false }; anim.name = "PA3_CinematicCameraMove_7_5s"; AssetDatabase.AddObjectToAsset(anim, timeline);
        var posX = new AnimationCurve(new Keyframe(0, camera.transform.position.x), new Keyframe(7.5f, camera.transform.position.x + 7f)); var posY = new AnimationCurve(new Keyframe(0, camera.transform.position.y + 2f), new Keyframe(7.5f, camera.transform.position.y + 1f)); var posZ = new AnimationCurve(new Keyframe(0, camera.transform.position.z - 4f), new Keyframe(7.5f, camera.transform.position.z + 4f)); anim.SetCurve("", typeof(Transform), "localPosition.x", posX); anim.SetCurve("", typeof(Transform), "localPosition.y", posY); anim.SetCurve("", typeof(Transform), "localPosition.z", posZ);
        var track = timeline.CreateTrack<AnimationTrack>(null, "Camera_Reveal"); var clip = track.CreateClip<AnimationPlayableAsset>(); ((AnimationPlayableAsset)clip.asset).clip = anim; director.SetGenericBinding(track, camera.transform); timeline.editorSettings.frameRate = 30; timeline.durationMode = TimelineAsset.DurationMode.FixedLength; timeline.fixedDuration = 7.5;
        var cinematicCam = GameObject.Find("Cinemachine_CinematicCamera");
        if (cinematicCam != null)
        {
            var cineCameraType = Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            var cineTrackType = Type.GetType("Unity.Cinemachine.CinemachineTrack, Unity.Cinemachine");
            var cineShotType = Type.GetType("Unity.Cinemachine.CinemachineShot, Unity.Cinemachine");
            if (cineTrackType != null && cineShotType != null)
            {
                var cineTrack = timeline.CreateTrack(cineTrackType, null, "Cinemachine Shot") as TrackAsset;
                var cineClip = cineTrack.CreateDefaultClip();
                var shotAsset = ScriptableObject.CreateInstance(cineShotType);
                AssetDatabase.AddObjectToAsset(shotAsset, timeline);
                cineClip.asset = shotAsset;
                cineClip.duration = 7.5;
                var cameraField = shotAsset.GetType().GetField("VirtualCamera");
                var cameraProperty = shotAsset.GetType().GetProperty("VirtualCamera");
                var cameraComponent = cinematicCam.GetComponent(cineCameraType);
                if (cameraField != null && cameraComponent != null)
                {
                    var exposedRef = Activator.CreateInstance(cameraField.FieldType);
                    var defaultValue = cameraField.FieldType.GetMethod("SetDefaultValue");
                    defaultValue?.Invoke(exposedRef, new object[] { cameraComponent });
                    cameraField.SetValue(shotAsset, exposedRef);
                }
                else if (cameraProperty != null && cameraComponent != null) cameraProperty.SetValue(shotAsset, cameraComponent);
                director.SetGenericBinding(cineTrack, directorGo);
            }
        }
        timeline.editorSettings.frameRate = 30; timeline.durationMode = TimelineAsset.DurationMode.FixedLength; timeline.fixedDuration = 7.5;
    }

    static void BuildNavMesh(Transform parent)
    {
        var go = new GameObject("NavigationSurface"); go.transform.SetParent(parent); var type = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation"); if (type == null) { Debug.LogWarning("PA3_NAVMESH_PENDING: AI Navigation package unavailable"); return; } var surface = go.AddComponent(type); type.GetMethod("BuildNavMesh")?.Invoke(surface, null);
    }

    static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale; var renderer = go.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material; return go;
    }

    static void CreatePrefab(GameObject source, string name)
    {
        string path = Root + "/Prefabs/" + name + ".prefab";
        Vector3 position = source.transform.localPosition; Quaternion rotation = source.transform.localRotation; Vector3 scale = source.transform.localScale;
        source.transform.localPosition = Vector3.zero; source.transform.localRotation = Quaternion.identity; source.transform.localScale = Vector3.one;
        var savedPrefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        if (savedPrefab == null) Debug.LogError("PA3_PREFAB_SAVE_FAILED: " + path);
        else Debug.Log("PA3_PREFAB_SAVED: " + path);
        source.transform.localPosition = position; source.transform.localRotation = rotation; source.transform.localScale = scale;
    }
}
