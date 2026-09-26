using UnityEngine;
using UnityEngine.Playables;
public sealed class CinematicAutoPlay : MonoBehaviour
{
    public PlayableDirector director; bool completed;
    void Start(){if(director==null)director=GetComponent<PlayableDirector>();if(director!=null){director.stopped+=OnStopped;director.Play();}}
    void OnStopped(PlayableDirector _){if(completed)return;completed=true;PA3GameManager.Instance?.CompleteCinematic();}
    void OnDestroy(){if(director!=null)director.stopped-=OnStopped;}
}
