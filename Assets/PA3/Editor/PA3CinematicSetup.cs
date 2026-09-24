using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class PA3CinematicSetup
{
    [MenuItem("PA3/Complete Cinematic Intro In Current Scene")]
    public static void CompleteIntro()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "PA3_VerticalSlice" || EditorApplication.isPlaying)
        {
            Debug.LogError("Open PA3_VerticalSlice in Edit mode before completing the intro.");
            return;
        }

        var directorObject = GameObject.Find("Cinematic_Director");
        var mainCamera = Camera.main;
        var player = Object.FindAnyObjectByType<PlayerController3D>();
        var portal = Object.FindAnyObjectByType<ExitTrigger>();
        var crystal = Object.FindAnyObjectByType<Collectible>();
        var director = directorObject != null ? directorObject.GetComponent<PlayableDirector>() : null;
        var timeline = director != null ? director.playableAsset as TimelineAsset : null;
        if (director == null || timeline == null || mainCamera == null || player == null || portal == null || crystal == null)
        {
            Debug.LogError("Intro setup needs the existing Director, Main Camera, player, portal and crystal.");
            return;
        }
        if (timeline.GetOutputTracks().Any())
        {
            Debug.LogWarning("Timeline already has tracks. No scene or asset changes were made.");
            return;
        }

        var brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null) brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
        brain.enabled = false; // CinematicAutoPlay enables it only for the intro.

        var wide = MakeCamera("Intro_Wide_Forest", directorObject.transform,
            player.transform.position + new Vector3(-7f, 6f, -11f),
            player.transform.position + Vector3.up * 1.5f);
        var crystalTarget = crystal.transform.position;
        var close = MakeCamera("Intro_Crystal", directorObject.transform,
            crystalTarget + new Vector3(4f, 3.5f, -6f), crystalTarget + Vector3.up);
        var portalTarget = portal.transform.position;
        var exit = MakeCamera("Intro_Portal", directorObject.transform,
            portalTarget + new Vector3(5f, 3.5f, -8f), portalTarget + Vector3.up * 1.5f);

        var track = timeline.CreateTrack<CinemachineTrack>(null, "Intro: bosque, cristal y portal");
        director.SetGenericBinding(track, brain);
        AddShot(track, director, wide, 0, 2.5, "Bosque");
        AddShot(track, director, close, 2.5, 2.5, "Cristal");
        AddShot(track, director, exit, 5, 2.5, "Portal");
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = 7.5;
        timeline.editorSettings.frameRate = 30;
        director.extrapolationMode = DirectorWrapMode.None;
        director.playOnAwake = false;

        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(director);
        EditorUtility.SetDirty(brain);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("PA3_CINEMATIC_READY: Three Cinemachine shots, 7.5 seconds, terrain untouched.");
    }

    static CinemachineCamera MakeCamera(string name, Transform parent, Vector3 position, Vector3 lookAt)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.LookAt(lookAt);
        var camera = go.AddComponent<CinemachineCamera>();
        camera.Lens.FieldOfView = 48f;
        return camera;
    }

    static void AddShot(CinemachineTrack track, PlayableDirector director,
        CinemachineCamera camera, double start, double duration, string displayName)
    {
        var clip = track.CreateClip<CinemachineShot>();
        clip.start = start;
        clip.duration = duration;
        clip.displayName = displayName;
        var shot = (CinemachineShot)clip.asset;
        var reference = shot.VirtualCamera;
        reference.exposedName = new PropertyName("PA3_" + camera.name);
        shot.VirtualCamera = reference;
        director.SetReferenceValue(reference.exposedName, camera);
    }
}
