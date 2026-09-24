using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class PA3CharacterSwap
{
    private const string ModelPath = "Assets/PA3/Models/LowPolyAdventurer/poza.fbx";
    private const string PlayerPrefabPath = "Assets/PA3/Prefabs/Player_3D.prefab";
    private const string MaterialPath = "Assets/PA3/Models/LowPolyAdventurer/PA3_Adventurer_URP.mat";
    private const string PalettePath = "Assets/PA3/Models/LowPolyAdventurer/paleta_kolorow_paczka.png";

    [InitializeOnLoadMethod]
    private static void ScheduleModelInspection()
    {
        EditorApplication.delayCall += InspectModel;
    }

    [MenuItem("PA3/Inspect Imported Adventurer")]
    private static void InspectModel()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogError("PA3_ADVENTURER_MISSING: " + ModelPath);
            return;
        }

        var report = new StringBuilder("PA3_ADVENTURER_INSPECT root=").Append(model.name);
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            report.Append(" | ").Append(renderer.gameObject.name)
                .Append(" type=").Append(renderer.GetType().Name)
                .Append(" bounds=").Append(renderer.bounds);
            foreach (Material material in renderer.sharedMaterials)
                report.Append(" material=").Append(material != null ? material.name : "null");
        }
        Debug.Log(report.ToString());
        GameObject player = GameObject.Find("Player_3D");
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (player != null && terrain != null)
        {
            Vector3 position = player.transform.position;
            float ground = terrain.SampleHeight(position) + terrain.transform.position.y;
            Debug.Log("PA3_PLAYER_TERRAIN_CHECK playerY=" + position.y + " groundY=" + ground + " delta=" + (position.y - ground));
        }
    }

    [MenuItem("PA3/Replace Player Visual Only")]
    private static void ReplacePlayerVisual()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogError("PA3: No se encontro el modelo del aventurero.");
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("PA3: No se encontro el shader URP Lit.");
                return;
            }
            material = new Material(shader) { name = "PA3_Adventurer_URP" };
            Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
            if (palette != null) material.SetTexture("_BaseMap", palette);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            ApplyVisual(prefabRoot, model, material);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject scenePlayer = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Player_3D") { scenePlayer = root; break; }
        }
        if (scenePlayer != null)
        {
            // The scene player can be an unpacked copy, so update it independently.
            ApplyVisual(scenePlayer, model, material);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("PA3_PLAYER_VISUAL_REPLACED prefab=" + PlayerPrefabPath + " scene=" + scene.path + " scenePlayer=" + (scenePlayer != null));
    }

    private static void ApplyVisual(GameObject player, GameObject model, Material material)
    {
        Transform existing = player.transform.Find("AdventurerVisual");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Keep the controller, colliders, scripts, camera references and trail intact.
        foreach (MeshRenderer oldRenderer in player.GetComponentsInChildren<MeshRenderer>(true))
            oldRenderer.enabled = false;
        foreach (SkinnedMeshRenderer oldRenderer in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            oldRenderer.enabled = false;

        GameObject visual = Object.Instantiate(model, player.transform);
        visual.name = "AdventurerVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new System.InvalidOperationException("El modelo no contiene geometria visible.");

        foreach (Renderer renderer in renderers)
        {
            Material[] mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            renderer.sharedMaterials = mats;
            renderer.enabled = true;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float scale = 1.95f / bounds.size.y;
        visual.transform.localScale = Vector3.one * scale;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 correction = new Vector3(player.transform.position.x - bounds.center.x,
            player.transform.position.y - bounds.min.y,
            player.transform.position.z - bounds.center.z);
        visual.transform.localPosition += player.transform.InverseTransformVector(correction);
        Debug.Log("PA3_PLAYER_VISUAL size=" + bounds.size + " scale=" + scale + " root=" + player.name);
    }
}
