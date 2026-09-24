using UnityEngine;
using UnityEngine.Rendering;

public static class PA3PortalReadyVisual
{
    public static void ShowReady()
    {
        Shader shader = Shader.Find("PA3/PortalReady");
        if (shader == null)
        {
            Debug.LogWarning("No se encontró el shader del portal listo.");
            return;
        }

        GameObject portalObject = GameObject.Find("PA3_ExitPortal");
        if (portalObject == null)
        {
            Debug.LogWarning("No se encontró PA3_ExitPortal en la escena al completar los cristales.");
            return;
        }
        Transform portal = portalObject.transform;
        if (portal.Find("PA3_ReadyInterior") != null) return;

            GameObject interior = GameObject.CreatePrimitive(PrimitiveType.Quad);
            interior.name = "PA3_ReadyInterior";
            interior.transform.SetParent(portal, false);
            interior.transform.localPosition = new Vector3(0f, 2f, 0f);
            interior.transform.localScale = new Vector3(2.05f, 3.55f, 1f);
            Collider collider = interior.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Object.Destroy(collider); }
            MeshRenderer renderer = interior.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(shader);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            GameObject glow = new GameObject("PA3_ReadyLight");
            glow.transform.SetParent(portal, false);
            glow.transform.localPosition = new Vector3(0f, 2f, 0.1f);
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.25f, 0.75f, 1f);
            light.intensity = 4f;
            light.range = 6f;
            light.shadows = LightShadows.None;
            Debug.Log("PA3_PORTAL_READY: interior luminoso activado.");
    }
}
