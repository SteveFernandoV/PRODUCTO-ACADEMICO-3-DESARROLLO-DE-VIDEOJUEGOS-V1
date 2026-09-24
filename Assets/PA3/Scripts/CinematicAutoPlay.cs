using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;
public sealed class CinematicAutoPlay : MonoBehaviour
{
    public PlayableDirector director;
    bool completed;
    CameraFollow3D follow;
    CinemachineBrain brain;
    PlayerController3D player;
    float previousUnlockTime;

    void Start()
    {
        if (director == null) director = GetComponent<PlayableDirector>();
        if (director == null || director.playableAsset == null) return;

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            follow = mainCamera.GetComponent<CameraFollow3D>();
            brain = mainCamera.GetComponent<CinemachineBrain>();
        }
        player = FindAnyObjectByType<PlayerController3D>();
        if (player != null)
        {
            previousUnlockTime = player.unlockAfterSeconds;
            player.unlockAfterSeconds = Mathf.Max(previousUnlockTime,
                Time.timeSinceLevelLoad + (float)director.duration);
        }
        if (follow != null) follow.enabled = false;
        if (brain != null) brain.enabled = true;
        director.stopped += OnStopped;
        director.Play();
    }

    void OnStopped(PlayableDirector _)
    {
        if (completed) return;
        completed = true;
        if (brain != null) brain.enabled = false;
        if (follow != null) follow.enabled = true;
        if (player != null) player.unlockAfterSeconds = previousUnlockTime;
        PA3GameManager.Instance?.CompleteCinematic();
    }

    void OnDestroy()
    {
        if (director != null) director.stopped -= OnStopped;
        if (!completed)
        {
            if (brain != null) brain.enabled = false;
            if (follow != null) follow.enabled = true;
            if (player != null) player.unlockAfterSeconds = previousUnlockTime;
        }
    }
}
