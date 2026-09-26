using UnityEngine;
public sealed class CameraFollow3D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0,4,-6);
    public float lookHeight=1.2f, smooth=7f;
    void LateUpdate() { if(target==null)return; Vector3 desired=target.position+target.TransformDirection(offset); transform.position=Vector3.Lerp(transform.position,desired,smooth*Time.deltaTime); transform.LookAt(target.position+Vector3.up*lookHeight); }
}
