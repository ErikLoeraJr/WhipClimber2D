using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento Ágil")]
    [SerializeField] private float velocidad = 14f;
    [SerializeField] private float fuerzaSalto = 16f;
    [SerializeField] private float multiplicadorCaida = 1.5f;

    [Header("Detección de Suelo")]
    [SerializeField] private Transform detectorSuelo;
    [SerializeField] private float radioDeteccion = 0.2f;
    [SerializeField] private LayerMask capaSuelo;

    private Rigidbody2D rb;
    private float movimientoH;
    private bool enSuelo;

    // CONTROL DE INERCIA DEL LÁTIGO
    [HideInInspector] public bool estaEnLatigo = false;
    private float tiempoInerciaRestante = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        movimientoH = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            movimientoH = -1f;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            movimientoH = 1f;

        enSuelo = Physics2D.OverlapCircle(detectorSuelo.position, radioDeteccion, capaSuelo);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && enSuelo && !estaEnLatigo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }

        if (movimientoH > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (movimientoH < 0)
            transform.localScale = new Vector3(-1, 1, 1);

        // Contador del temporizador de inercia protegida
        if (tiempoInerciaRestante > 0)
        {
            tiempoInerciaRestante -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (!estaEnLatigo)
        {
            if (enSuelo)
            {
                rb.linearVelocity = new Vector2(movimientoH * velocidad, rb.linearVelocity.y);
            }
            else
            {
                // Si el temporizador de inercia está activo, NO dejamos que A/D reduzcan la velocidad acumulada
                if (tiempoInerciaRestante > 0)
                {
                    // Solo permitimos acelerar más si el jugador presiona en la misma dirección de la inercia
                    if (Mathf.Abs(movimientoH) > 0.1f)
                    {
                        float nuevaVelocidadX = rb.linearVelocity.x;
                        
                        // Si presiona hacia la derecha y se mueve a la derecha, conserva o aumenta velocidad
                        if (movimientoH > 0 && nuevaVelocidadX > 0)
                            nuevaVelocidadX = Mathf.Max(nuevaVelocidadX, movimientoH * velocidad);
                        // Si presiona hacia la izquierda y se mueve a la izquierda, conserva o aumenta velocidad
                        else if (movimientoH < 0 && nuevaVelocidadX < 0)
                            nuevaVelocidadX = Mathf.Min(nuevaVelocidadX, movimientoH * velocidad);

                        rb.linearVelocity = new Vector2(nuevaVelocidadX, rb.linearVelocity.y);
                    }
                }
                else
                {
                    // Control aéreo normal cuando acaba la inercia del látigo
                    if (Mathf.Abs(movimientoH) > 0.1f)
                    {
                        rb.linearVelocity = new Vector2(movimientoH * velocidad, rb.linearVelocity.y);
                    }
                }
            }

            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (multiplicadorCaida - 1) * Time.fixedDeltaTime;
            }
        }
    }

    // Método para activar la protección de inercia desde el LatigoController
    public void ActivarInerciaProtegida(float duracion)
    {
        tiempoInerciaRestante = duracion;
    }

    private void OnDrawGizmosSelected()
    {
        if (detectorSuelo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(detectorSuelo.position, radioDeteccion);
        }
    }
}