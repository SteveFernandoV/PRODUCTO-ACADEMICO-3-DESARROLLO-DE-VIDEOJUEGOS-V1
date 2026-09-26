using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using Object = UnityEngine.Object;

public static class PA3SceneBuilder
{
    const string Root = "Assets/PA3";
    const string ScenePath = "Assets/Scenes/PA3_VerticalSlice.unity";
    const string NaturePackage = @"C:\Users\fer10\AppData\Roaming\Unity\Asset Store-5.x\Polytope Studio\3D ModelsEnvironments\Low Poly Environment - Nature Free - LOWPOLY MEDIEVAL FANTASY SERIES.unitypackage";
    const string SkyPackage = @"C:\Users\fer10\AppData\Roaming\Unity\Asset Store-5.x\rpgwhitelock\Textures MaterialsSkies\AllSky Free - 10 Sky Skybox Set.unitypackage";
    static readonly Color Ground = new Color(0.10f, 0.22f, 0.17f);
    static readonly Color Rock = new Color(0.23f, 0.28f, 0.34f);
    static readonly Color Accent = new Color(0.10f, 0.75f, 1f);
    static readonly List<GameObject> staticObjects = new List<GameObject>();

    [MenuItem("PA3/Build Vertical Slice")]
    public static void Build() { BuildInternal(true); }

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

    public static void BuildInternal(bool save)
    {
        EnsureFolders();
        foreach (var prefab in new[] { "EnergyCrystal", "GuardianEnemy", "NarrativeBeacon", "EnergyMistVFX", "Player_3D" }) AssetDatabase.DeleteAsset(Root + "/Prefabs/" + prefab + ".prefab");
        foreach (var material in new[] { "PA3_Ground", "PA3_Rock", "PA3_Accent", "PA3_Hazard", "PA3_DynamicPulse" }) AssetDatabase.DeleteAsset(Root + "/Materials/" + material + ".mat");
        AssetDatabase.DeleteAsset(Root + "/Materials/PA3_GroundTerrain.terrainlayer");
        AssetDatabase.DeleteAsset(Root + "/Materials/PA3_Ground_Grass.terrainlayer");
        AssetDatabase.DeleteAsset(Root + "/Materials/PA3_Ground_Soil.terrainlayer");
        AssetDatabase.DeleteAsset(Root + "/Materials/PA3_PostProcessing_Profile.asset");
        AssetDatabase.DeleteAsset(Root + "/Textures/GroundTexture.asset");
        AssetDatabase.DeleteAsset(Root + "/Timeline/PA3_Cinematic_7_5s.playable");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        staticObjects.Clear();

        var materials = CreateMaterials();
        ApplySkybox();
        var terrain = CreateTerrain(materials.ground);
        var root = new GameObject("PA3_VerticalSlice");
        var environment = new GameObject("Environment"); environment.transform.SetParent(root.transform);
        var structures = new GameObject("Vertical_Structures"); structures.transform.SetParent(environment.transform);
        var vfx = new GameObject("VFX"); vfx.transform.SetParent(root.transform);

        BuildStructures(structures.transform, materials, terrain);
        BuildNatureDressing(environment.transform, terrain);
        CreateVfx(vfx.transform, materials);
        var player = CreatePlayer(root.transform, materials);
        CreateCamera(player.transform);
        BuildCollectibles(root.transform, materials);
        BuildEnemy(root.transform, player.transform, materials);
        BuildHazardAndExit(root.transform, player.transform, materials);
        BuildUi(root.transform, player.transform);
        BuildLightingAndPost(root.transform);
        BuildTimeline(root.transform, player.transform);
        BuildNavMesh(root.transform);

        if (terrain != null) terrain.transform.SetParent(environment.transform);
        foreach (var go in staticObjects) if (go != null) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!buildScenes.Exists(s => s.path == ScenePath)) buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
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
            var m = new Material(lit) { name = name, color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            string path = Root + "/Materials/" + name + ".mat";
            AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path); return m;
        }
        return (Make("PA3_Ground", Ground), Make("PA3_Rock", Rock, 0.05f, 0.5f), Make("PA3_Accent", Accent, 0.15f, 0.8f), Make("PA3_Hazard", new Color(0.95f, 0.16f, 0.12f), 0f, 0.25f));
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
        var player = new GameObject("Player_3D"); player.tag = "Player"; player.transform.SetParent(parent); player.transform.position = new Vector3(12, 5, 12);
        var cc = player.AddComponent<CharacterController>(); cc.height = 2.2f; cc.radius = 0.45f; cc.center = new Vector3(0, 1.1f, 0); cc.slopeLimit = 45f;
        GameObject Part(string name, PrimitiveType shape, Vector3 localPosition, Vector3 scale, Material material)
        {
            var part = Primitive(name, shape, Vector3.zero, scale, material, player.transform);
            part.transform.localPosition = localPosition;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }
        Part("Player_Torso", PrimitiveType.Capsule, new Vector3(0, 1.28f, 0), new Vector3(0.62f, 0.72f, 0.46f), m.rock);
        Part("Player_Head", PrimitiveType.Sphere, new Vector3(0, 2.12f, 0), new Vector3(0.48f, 0.48f, 0.48f), m.accent);
        Part("Player_ChestCore", PrimitiveType.Sphere, new Vector3(0, 1.42f, 0.36f), new Vector3(0.24f, 0.24f, 0.12f), m.accent);
        Part("Player_LeftArm", PrimitiveType.Capsule, new Vector3(-0.45f, 1.32f, 0), new Vector3(0.20f, 0.62f, 0.20f), m.rock);
        Part("Player_RightArm", PrimitiveType.Capsule, new Vector3(0.45f, 1.32f, 0), new Vector3(0.20f, 0.62f, 0.20f), m.rock);
        Part("Player_LeftLeg", PrimitiveType.Capsule, new Vector3(-0.18f, 0.48f, 0), new Vector3(0.24f, 0.58f, 0.24f), m.rock);
        Part("Player_RightLeg", PrimitiveType.Capsule, new Vector3(0.18f, 0.48f, 0), new Vector3(0.24f, 0.58f, 0.24f), m.rock);
        player.AddComponent<PlayerController3D>();
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

    static void BuildCollectibles(Transform parent, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var source = Primitive("Crystal_Source", PrimitiveType.Cylinder, new Vector3(18, 5, 18), new Vector3(0.8f, 1.2f, 0.8f), m.accent, parent); source.GetComponent<Collider>().isTrigger = true; source.AddComponent<Collectible>(); CreatePrefab(source, "EnergyCrystal");
        var positions = new[] { new Vector3(18, 5, 18), new Vector3(37, 7, 27), new Vector3(54, 9, 20), new Vector3(69, 9, 42), new Vector3(86, 11, 68), new Vector3(103, 12, 92) };
        for (int i = 0; i < positions.Length; i++) { GameObject c = i == 0 ? source : Object.Instantiate(source, parent); c.name = "EnergyCrystal_" + (i + 1); c.transform.position = positions[i]; }
    }

    static void BuildEnemy(Transform parent, Transform player, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var enemy = Primitive("Guardian_Enemy", PrimitiveType.Capsule, new Vector3(82, 11, 52), Vector3.one * 1.5f, m.hazard, parent); var agent = enemy.AddComponent<NavMeshAgent>(); agent.speed = 2.5f; agent.radius = 0.55f; agent.height = 2.5f; var script = enemy.AddComponent<EnemyChaser>(); script.target = player; CreatePrefab(enemy, "GuardianEnemy");
    }

    static void BuildHazardAndExit(Transform parent, Transform player, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var hazard = Primitive("Hazard_Trigger", PrimitiveType.Cube, new Vector3(55, 1, 58), new Vector3(12, 0.3f, 12), m.hazard, parent); hazard.GetComponent<Renderer>().material.color = new Color(1f, 0.1f, 0.05f, 0.35f); hazard.GetComponent<BoxCollider>().isTrigger = true; hazard.AddComponent<HazardTrigger>();
        var exit = Primitive("Exit_Trigger", PrimitiveType.Cube, new Vector3(110, 5, 105), new Vector3(10, 8, 5), m.accent, parent); exit.GetComponent<BoxCollider>().isTrigger = true; exit.GetComponent<ExitTrigger>();
        var spawn = new GameObject("PlayerSpawn"); spawn.transform.SetParent(parent); spawn.transform.position = new Vector3(12, 5, 12); player.GetComponent<PlayerController3D>().spawnPoint = spawn.transform;
    }

    static void CreateVfx(Transform parent, (Material ground, Material rock, Material accent, Material hazard) m)
    {
        var go = new GameObject("VFX_EnergyMist"); go.transform.SetParent(parent); go.transform.position = new Vector3(62, 5, 62); var ps = go.AddComponent<ParticleSystem>(); var main = ps.main; main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.1f, 0.8f, 1f, 0.55f)); main.startLifetime = 2.4f; main.startSpeed = 0.8f; main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.48f); main.maxParticles = 550; main.simulationSpace = ParticleSystemSimulationSpace.World; var emission = ps.emission; emission.rateOverTime = 65f; var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = 3.5f; var color = ps.colorOverLifetime; color.enabled = true; var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(new Color(0.15f,0.85f,1f),0f), new GradientColorKey(new Color(0.58f,0.35f,1f),1f) }, new[] { new GradientAlphaKey(0f,0f), new GradientAlphaKey(0.8f,0.18f), new GradientAlphaKey(0f,1f) }); color.color = gradient; var size = ps.sizeOverLifetime; size.enabled = true; var curve = new AnimationCurve(new Keyframe(0,0.15f),new Keyframe(0.35f,1f),new Keyframe(1,0f)); size.size = new ParticleSystem.MinMaxCurve(1f,curve); var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.material = m.accent; renderer.renderMode = ParticleSystemRenderMode.Billboard; CreatePrefab(go, "EnergyMistVFX");
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
        var manager = new GameObject("PA3_GameManager"); manager.transform.SetParent(parent); var gm = manager.AddComponent<PA3GameManager>(); gm.objectiveText = objective; gm.statusText = status; gm.player = player;
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
        string path = Root + "/Prefabs/" + name + ".prefab"; PrefabUtility.SaveAsPrefabAsset(source, path);
    }
}
