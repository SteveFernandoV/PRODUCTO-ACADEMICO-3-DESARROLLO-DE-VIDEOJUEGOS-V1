using UnityEngine;
using UnityEngine.UI;

public sealed class PA3GameManager : MonoBehaviour
{
    public static PA3GameManager Instance { get; private set; }
    public Text objectiveText, statusText;
    public Text collectionTotalText;
    public Image collectionProgress;
    public int totalCollectibles = 6, collected;
    public bool cinematicFinished;
    void Awake() { Instance = this; RefreshUi(); if (collected >= totalCollectibles) PA3PortalReadyVisual.ShowReady(); }
    public void Collect() { collected++; RefreshUi(); if (collected >= totalCollectibles) { if (statusText != null) statusText.text = "Ruta despejada: llega al portal de salida"; PA3PortalReadyVisual.ShowReady(); } }
    public void CompleteCinematic() { cinematicFinished = true; if (statusText != null) statusText.text = "Explora el valle y recupera los cristales"; }
    public void ReachExit()
    {
        if (collected < totalCollectibles)
        {
            if (statusText != null) statusText.text = "Aún faltan cristales por recuperar";
            return;
        }
        PA3MenuController.ReturnToMainMenu("¡Victoria! Recuperaste los cristales y restauraste el valle.");
    }
    void RefreshUi()
    {
        if (objectiveText != null) objectiveText.text = collected.ToString("00");
        if (collectionTotalText != null) collectionTotalText.text = totalCollectibles.ToString("00");
        if (collectionProgress != null) collectionProgress.fillAmount = totalCollectibles <= 0 ? 0f : Mathf.Clamp01((float)collected / totalCollectibles);
        if (statusText != null && collected == 0) statusText.text = "Reúne los cristales de luz para abrir el portal";
    }
}
