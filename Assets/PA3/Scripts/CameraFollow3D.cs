using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-100)]
public sealed class CameraFollow3D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0,4,-6);
    public float lookHeight=1.2f, smooth=7f;
    [Header("Primera persona - tecla C")]
    public float eyeHeight = 1.7f;
    public float mouseSensitivity = 0.12f;
    public bool IsFirstPerson { get; private set; }
    float yaw, pitch, originalNearClip;
    Camera viewCamera;
    Renderer[] playerRenderers;
    bool[] previousVisibility;
    CursorLockMode previousCursorLock;
    bool previousCursorVisible;

    void Awake()
    {
        viewCamera = GetComponent<Camera>();
        if (viewCamera != null) originalNearClip = viewCamera.nearClipPlane;
    }

    void Update()
    {
        FindTarget();
        if (target == null || Time.timeScale == 0f) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
            SetFirstPerson(!IsFirstPerson);
        if (!IsFirstPerson) return;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked) return;
        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        yaw += delta.x;
        pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
        target.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void SetFirstPerson(bool enabled)
    {
        FindTarget();
        if (enabled == IsFirstPerson || (enabled && target == null)) return;
        IsFirstPerson = enabled;
        if (enabled)
        {
            yaw = target.eulerAngles.y;
            pitch = 0f;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            playerRenderers = target.GetComponentsInChildren<Renderer>(true);
            previousVisibility = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                previousVisibility[i] = playerRenderers[i].forceRenderingOff;
                playerRenderers[i].forceRenderingOff = true;
            }
            if (viewCamera != null) viewCamera.nearClipPlane = 0.05f;
            transform.SetPositionAndRotation(target.position + Vector3.up * eyeHeight,
                Quaternion.Euler(0f, yaw, 0f));
        }
        else
        {
            if (playerRenderers != null)
                for (int i = 0; i < playerRenderers.Length; i++)
                    if (playerRenderers[i] != null) playerRenderers[i].forceRenderingOff = previousVisibility[i];
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            if (viewCamera != null) viewCamera.nearClipPlane = originalNearClip;
            if (target != null)
            {
                transform.position = target.position + target.TransformDirection(offset);
                transform.LookAt(target.position + Vector3.up * lookHeight);
            }
        }
    }

    void OnDisable() { SetFirstPerson(false); }

    void FindTarget()
    {
        if (target != null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }
    void LateUpdate()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        if (target == null) return;
        if (IsFirstPerson)
        {
            transform.SetPositionAndRotation(target.position + Vector3.up * eyeHeight,
                Quaternion.Euler(pitch, yaw, 0f));
            return;
        }
        Vector3 desired = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
