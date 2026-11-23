using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jugadora;
    [SerializeField] private AudioSource musicaFondo;
    [SerializeField] private GameObject panelGameOver;

    [Header("Audio de Derrota")]
    [SerializeField] private AudioClip sfxGameOver;

    [Header("Configuración de Caída")]
    [Tooltip("Distancia en metros hacia abajo respecto a la altura máxima alcanzada para perder.")]
    [SerializeField] private float limiteCaidaAbajo = 18f;
    [Tooltip("Tiempo en segundos que espera el juego para congelarse mientras el personaje cae fuera de pantalla.")]
    [SerializeField] private float tiempoEsperaDesaparicion = 0.5f;

    private float alturaMaximaAlcanzada = 0f;
    private bool juegoTerminado = false;

    private void Start()
    {
        if (jugadora == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) jugadora = playerObj.transform;
        }

        if (jugadora != null)
        {
            alturaMaximaAlcanzada = jugadora.position.y;
        }

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (jugadora == null || juegoTerminado) return;

        if (jugadora.position.y > alturaMaximaAlcanzada)
        {
            alturaMaximaAlcanzada = jugadora.position.y;
        }

        if (jugadora.position.y < alturaMaximaAlcanzada - limiteCaidaAbajo)
        {
            ActivarGameOver();
        }
    }

    private void ActivarGameOver()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        StartCoroutine(MostrarRutinaGameOver());
    }

    private IEnumerator MostrarRutinaGameOver()
    {
        yield return new WaitForSeconds(tiempoEsperaDesaparicion);

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
        }

        if (musicaFondo != null)
        {
            musicaFondo.Stop();
            if (sfxGameOver != null)
            {
                musicaFondo.PlayOneShot(sfxGameOver, 1f);
            }
        }

        Time.timeScale = 0f;
    }

    public void ReintentarJuego()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuInicio");
    }
}