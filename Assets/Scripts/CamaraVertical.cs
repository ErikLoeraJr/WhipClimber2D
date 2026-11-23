using UnityEngine;

public class CamaraVertical : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jugadora;

    [Header("Configuración de Seguimiento")]
    [SerializeField] private float velocidadSuavizado = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Límite de Caída")]
    [Tooltip("Distancia máxima en Y que la cámara puede BARRER hacia abajo cuando la jugadora cae.")]
    [SerializeField] private float margenPermitidoAbajo = 3f;

    private float alturaMaximaAlcanzada;

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
            // Posicionar la cámara inicialmente en la jugadora
            transform.position = new Vector3(transform.position.x, jugadora.position.y + offset.y, offset.z);
        }
    }

    private void LateUpdate()
    {
        if (jugadora == null) return;

        // 1. Si la jugadora sube a una nueva altura, actualizamos la altura máxima
        if (jugadora.position.y > alturaMaximaAlcanzada)
        {
            alturaMaximaAlcanzada = jugadora.position.y;
        }

        // 2. Calculamos el límite inferior de la cámara (Altura Máxima - Margen Permitido)
        float limiteCamaraY = alturaMaximaAlcanzada - margenPermitidoAbajo + offset.y;

        // 3. Calculamos la Y deseada: seguirá a la jugadora si sube, pero NO bajará del límite
        float yDeseada = Mathf.Max(jugadora.position.y + offset.y, limiteCamaraY);

        // 4. Mantenemos la X fija (o centrada) y la Z de la cámara en -10
        Vector3 posicionObjetivo = new Vector3(transform.position.x, yDeseada, offset.z);

        // 5. Mover suavemente la cámara a la posición objetivo
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, velocidadSuavizado * Time.deltaTime);
    }
}