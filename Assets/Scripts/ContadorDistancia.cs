using UnityEngine;
using TMPro; // Necesario para usar TextMeshPro

public class ContadorDistancia : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jugadora;
    [SerializeField] private TextMeshProUGUI textoDistancia; // El componente de texto UI

    [Header("Configuración de Medición")]
    [Tooltip("Punto Y desde donde comenzará a contar 0 metros.")]
    [SerializeField] private float alturaInicial = 0f;
    [Tooltip("Multiplicador por si quieres que los metros avancen más rápido o lento.")]
    [SerializeField] private float escalaMetros = 1f;

    private float recordAltura = 0f;
    private int metrosMaximos = 0;

    private void Start()
    {
        if (jugadora == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) jugadora = playerObj.transform;
        }

        // Si no definiste altura inicial manual, toma la posición inicial de la jugadora
        if (jugadora != null && alturaInicial == 0f)
        {
            alturaInicial = jugadora.position.y;
        }

        recordAltura = alturaInicial;
    }

    private void Update()
    {
        if (jugadora == null) return;

        // Solo actualizamos la distancia si la jugadora alcanza una NUEVA altura máxima (no baja al caer)
        if (jugadora.position.y > recordAltura)
        {
            recordAltura = jugadora.position.y;
            
            // Calculamos la distancia desde el origen
            float metrosCalculados = (recordAltura - alturaInicial) * escalaMetros;
            metrosMaximos = Mathf.Max(0, Mathf.FloorToInt(metrosCalculados));

            ActualizarTexto();
        }
    }

    private void ActualizarTexto()
    {
        if (textoDistancia != null)
        {
            textoDistancia.text = $"{metrosMaximos} m";
            // O también: textoDistancia.text = "ALTURA: " + metrosMaximos + "m";
        }
    }

    // Método público por si quieres obtener la puntuación/distancia final al morir
    public int ObtenerDistanciaActual()
    {
        return metrosMaximos;
    }
}