using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PA3VisualFinish
{
    [MenuItem("PA3/Apply Crystal Shader Graph and Gentle URP Polish")]
    public static void Apply()
    {
        const string root = "Assets/PA3/";
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(root + "Shaders/DynamicWorld.shadergraph");
        var crystal = AssetDatabase.LoadAssetAtPath<Material>(root + "Materials/PA3_Crystal_Glow.mat");
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(root + "Materials/PA3_PostProcessing_Profile.asset");
        if (shader == null || crystal == null || profile == null ||
            !profile.TryGet(out Bloom bloom) ||
            !profile.TryGet(out ColorAdjustments color) ||
            !profile.TryGet(out Vignette vignette))
        {
            Debug.LogError("PA3_VISUAL_FINISH: Shader Graph, crystal material, or URP profile is missing. Nothing changed.");
            return;
        }

        var previousShader = crystal.shader;
        crystal.shader = shader;
        if (!crystal.HasProperty("_Diffuse"))
        {
            crystal.shader = previousShader;
            Debug.LogError("PA3_VISUAL_FINISH: Graph has no _Diffuse color property. Material was restored.");
            return;
        }
        crystal.SetColor("_Diffuse", new Color(0.08f, 0.72f, 1.2f, 1f));
        EditorUtility.SetDirty(crystal);

        bloom.threshold.Override(0.8f);
        bloom.intensity.Override(0.5f);
        color.postExposure.Override(0.18f);
        color.contrast.Override(10f);
        color.saturation.Override(6f);
        vignette.intensity.Override(0.15f);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("PA3_VISUAL_FINISH_READY: Crystal material uses time-pulsing URP Shader Graph; Bloom and color tuned gently.");
    }
}
