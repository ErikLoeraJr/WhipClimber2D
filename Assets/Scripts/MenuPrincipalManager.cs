using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("Nombre exacto de la escena donde se juega.")]
    [SerializeField] private string nombreEscenaJuego = "SampleScene";

    [Header("Paneles Opcionales")]
    [SerializeField] private GameObject panelControles;

    private void Start()
    {
        // Asegurar que el tiempo corra con normalidad al volver desde un Game Over
        Time.timeScale = 1f;

        if (panelControles != null)
        {
            panelControles.SetActive(false);
        }
    }

    // Asignar al evento OnClick() de BotonJugar
    public void IniciarJuego()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // Asignar al evento OnClick() de BotonControles
    public void AbrirControles()
    {
        if (panelControles != null)
        {
            panelControles.SetActive(true);
        }
    }

    // Asignar al botón "Cerrar" o "Volver" dentro del panel de controles
    public void CerrarControles()
    {
        if (panelControles != null)
        {
            panelControles.SetActive(false);
        }
    }

    // Asignar al evento OnClick() de BotonSalir
    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}