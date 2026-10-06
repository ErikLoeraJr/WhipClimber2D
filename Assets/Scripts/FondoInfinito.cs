using UnityEngine;

public class FondoInfinito : MonoBehaviour
{
    [Header("Velocidad de movimiento propio (Nubes)")]
    public Vector2 velocidadNubes = new Vector2(0.05f, 0f);

    [Header("Seguir a la cámara (Parallax)")]
    public Transform camara;
    [Range(0f, 1f)] public float efectoParallax = 0.8f; // 1 = se mueve pegado a la cámara, 0 = no sigue a la cámara

    private Material materialFondo;
    private Vector2 offsetActual;
    private Vector3 ultimaPosicionCamara;

    void Start()
    {
        // Obtener el material del SpriteRenderer para desplazar su textura
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        materialFondo = sr.material;

        if (camara == null && Camera.main != null)
        {
            camara = Camera.main.transform;
        }

        if (camara != null)
        {
            ultimaPosicionCamara = camara.position;
        }
    }

    void Update()
    {
        // 1. Movimiento continuo automático de las nubes
        offsetActual += velocidadNubes * Time.deltaTime;

        // 2. Movimiento relativo cuando el jugador/cámara se mueve
        if (camara != null)
        {
            Vector3 deltaCamara = camara.position - ultimaPosicionCamara;
            // Se convierte el movimiento de la cámara a desplazamiento de textura
            offsetActual.x += deltaCamara.x * (1f - efectoParallax) * 0.1f;
            offsetActual.y += deltaCamara.y * (1f - efectoParallax) * 0.1f;

            // Mover el objeto del fondo junto a la cámara para que nunca se salga de la pantalla
            transform.position = new Vector3(camara.position.x, camara.position.y, transform.position.z);
            ultimaPosicionCamara = camara.position;
        }

        // Aplicar el desplazamiento a la textura
        materialFondo.mainTextureOffset = offsetActual;
    }
}