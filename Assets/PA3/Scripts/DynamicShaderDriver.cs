using UnityEngine;
public sealed class DynamicShaderDriver : MonoBehaviour
{
    public Color firstColor=new Color(.05f,.8f,1f,1f),secondColor=new Color(.75f,.12f,1f,1f); Renderer cachedRenderer; MaterialPropertyBlock block;
    void Awake(){cachedRenderer=GetComponent<Renderer>();block=new MaterialPropertyBlock();}
    void Update(){if(cachedRenderer==null)return;float pulse=(Mathf.Sin(Time.time*2f)+1f)*.5f;cachedRenderer.GetPropertyBlock(block);Color color=Color.Lerp(firstColor,secondColor,pulse);block.SetColor("_Diffuse",color);block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*2f);cachedRenderer.SetPropertyBlock(block);}
}
