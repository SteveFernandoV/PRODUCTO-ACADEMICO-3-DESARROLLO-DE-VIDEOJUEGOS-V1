using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class PA3TerrainRecovery
{
    private const string ScenePath = "Assets/Scenes/PA3_VerticalSlice.unity";
    private const string TerrainDataPath = "Assets/New Terrain 9.asset";

    [MenuItem("PA3/Restore My Terrain")]
    private static void RestoreTerrain()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("PA3_TERRAIN_RECOVERY_WAITING: open PA3_VerticalSlice to restore its terrain.");
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<Terrain>(true) != null)
            {
                Debug.Log("PA3_TERRAIN_RECOVERY_SKIPPED: a Terrain is already present in the scene.");
                return;
            }
        }

        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (data == null)
        {
            Debug.LogError("PA3_TERRAIN_RECOVERY_FAILED: missing " + TerrainDataPath);
            return;
        }

        GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
        terrainObject.name = "Terrain";
        terrainObject.transform.position = Vector3.zero;
        SceneManager.MoveGameObjectToScene(terrainObject, scene);

        if (!EditorSceneManager.SaveScene(scene))
        {
            Object.DestroyImmediate(terrainObject);
            Debug.LogError("PA3_TERRAIN_RECOVERY_FAILED: scene could not be saved.");
            return;
        }

        Debug.Log("PA3_TERRAIN_RECOVERED: Terrain reconnected to New Terrain 9.asset in PA3_VerticalSlice.");
    }

    [MenuItem("PA3/Place Gameplay Back on My Terrain")]
    private static void RestoreGameplayPosition()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject gameplay = null;
        GameObject portal = null;
        GameObject camera = null;
        Terrain terrain = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "PA3_VerticalSlice") gameplay = root;
            else if (root.name == "PA3_ExitPortal") portal = root;
            else if (root.name == "Main Camera") camera = root;
            else if (root.GetComponent<Terrain>() != null) terrain = root.GetComponent<Terrain>();
        }

        if (gameplay == null || terrain == null)
            return;
        if (Mathf.Abs(gameplay.transform.position.x) > 0.01f || Mathf.Abs(gameplay.transform.position.z) > 0.01f)
            return;

        // The last visible placement of the user's VFX was at this forest clearing.
        Vector3 clearing = new Vector3(375.67f, 0f, 435.77f);
        clearing.y = terrain.SampleHeight(clearing) + 0.05f;
        Vector3 offset = clearing - gameplay.transform.position;

        Undo.RecordObject(gameplay.transform, "Restore gameplay to terrain");
        gameplay.transform.position += offset;
        if (portal != null)
        {
            Undo.RecordObject(portal.transform, "Restore portal to terrain");
            portal.transform.position += offset;
        }
        if (camera != null)
        {
            Undo.RecordObject(camera.transform, "Restore camera to terrain");
            camera.transform.position += offset;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorSceneManager.SaveScene(scene))
            Debug.Log("PA3_GAMEPLAY_REPOSITIONED: gameplay, portal, and camera moved to the recovered forest clearing at " + clearing);
        else
            Debug.LogError("PA3_GAMEPLAY_REPOSITION_FAILED: scene could not be saved.");
    }
}
