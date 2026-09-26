using UnityEngine;
using UnityEngine.UI;

public sealed class PA3GameManager : MonoBehaviour
{
    public static PA3GameManager Instance { get; private set; }
    public Text objectiveText, statusText;
    public int totalCollectibles = 6, collected;
    public Transform player, exitPoint;
    public bool cinematicFinished;
    void Awake() { Instance = this; RefreshUi(); }
    public void Collect() { collected++; RefreshUi(); if (collected >= totalCollectibles && statusText != null) statusText.text = "Ruta despejada: llega al portal de salida"; }
    public void CompleteCinematic() { cinematicFinished = true; if (statusText != null) statusText.text = "Explora el valle y recupera los cristales"; }
    public void ReachExit() { if (collected < totalCollectibles) { if (statusText != null) statusText.text = "Aún faltan cristales por recuperar"; return; } if (statusText != null) statusText.text = "PA3 COMPLETADO · vertical slice validado"; }
    void RefreshUi() { if (objectiveText != null) objectiveText.text = $"CRISTALES  {collected}/{totalCollectibles}"; if (statusText != null && collected == 0) statusText.text = "Recupera los cristales de energía"; }
}
