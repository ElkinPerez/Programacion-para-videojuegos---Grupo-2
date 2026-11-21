

using UnityEngine;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Panel de ajustes")]
    public GameObject panelAjustes;

    [Header("Panel de creditos")]
    public GameObject panelCreditos;

    // AJUSTES
    public void AbrirAjustes()
    {
        panelAjustes.SetActive(true);
    }

    public void CerrarAjustes()
    {
        panelAjustes.SetActive(false);
    }

    // CREDITOS
    public void AbrirCreditos()
    {
        panelCreditos.SetActive(true);
    }

    public void CerrarCreditos()
    {
        panelCreditos.SetActive(false);
    }
}
