using UnityEngine;
using UnityEngine.InputSystem;

public class LatigoController : MonoBehaviour
{
    [Header("Configuración del Látigo Normal")]
    [Range(3f, 25f)]
    [SerializeField] private float alcanceMaximo = 8f;
    [SerializeField] private LayerMask capaEnganche;
    [SerializeField] private float fuerzaBalanceo = 30f;
    [SerializeField] private float impulsoSaltoLatigo = 18f;
    [SerializeField] private float velocidadRecoger = 8f;
    [SerializeField] private float anguloLimiteDesenganche = 0.5f;
    [SerializeField] private float friccionAireBalanceo = 0.5f;
    [SerializeField] private float cooldownLatigoNormal = 0.4f;
    private float tiempoSiguienteLatigoNormal = 0f;

    [Header("Asistencia de Apuntado (Aim Assist / Magnetismo)")]
    [Tooltip("Grosor del área de detección al apuntar con el ratón.")]
    [SerializeField] private float radioMagnetismo = 0.8f;

    [Header("Mecánica: Impulso Rápido (Grapple Boost)")]
    [SerializeField] private float fuerzaImpulsoRapido = 20f;
    [SerializeField] private float distanciaMinimaLlegada = 2.0f;
    [SerializeField] private float impulsosSalidaExtra = 5f;
    [SerializeField] private float cooldownImpulsoRapido = 0.4f;
    private float tiempoSiguienteImpulsoRapido = 0f;

    [Header("Feedback Visual y VFX")]
    [Tooltip("Prefab de Partículas que se instanciará en el pivote al enganchar.")]
    [SerializeField] private ParticleSystem vfxImpactoPrefabs;

    [Header("Referencias")]
    [SerializeField] private Transform puntoDisparo;

    private DistanceJoint2D distanceJoint;
    private LineRenderer lineRenderer;
    private Rigidbody2D rb;
    private PlayerController playerController;

    private Vector2 puntoAgarrado;
    private bool estaEnganchado = false;
    private bool realizandoImpulsoRapido = false;
    private float gravedadOriginal;

    // --- BLOQUEO DE REENGANCHE AL MISMO PIVOTE ---
    private Vector2? ultimoPivoteUsado = null;
    private const float TOLERANCIA_MISMO_PIVOTE = 0.2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        distanceJoint = GetComponent<DistanceJoint2D>();
        lineRenderer = GetComponent<LineRenderer>();
        playerController = GetComponent<PlayerController>();

        gravedadOriginal = rb.gravityScale;
        distanceJoint.enabled = false;
        lineRenderer.enabled = false;

        // Fijamos la cuerda a 2 puntos rígidos de fábrica para evitar vibraciones
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
        }
    }

    void Update()
    {
        // --- 1. LÁTIGO NORMAL (Clic Izquierdo / Tecla J / Tecla O) ---
        if (Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame)
        {
            if (PuedeUsarLatigoNormal()) IntentarEngancharConMouse();
        }
        else if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (PuedeUsarLatigoNormal()) IntentarEngancharAuto();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame || Keyboard.current.jKey.wasReleasedThisFrame || Keyboard.current.oKey.wasReleasedThisFrame)
        {
            if (!realizandoImpulsoRapido) SoltarLatigo(darImpulso: false);
        }

        // --- 2. IMPULSO RÁPIDO (Clic Derecho / Tecla K / Tecla P) ---
        if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (PuedeUsarImpulsoRapido()) IntentarImpulsoRapidoConMouse();
        }
        else if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (PuedeUsarImpulsoRapido()) IntentarImpulsoRapidoAuto();
        }

        // Salto manual para catapultarse
        if (Keyboard.current.spaceKey.wasPressedThisFrame && (estaEnganchado || realizandoImpulsoRapido))
        {
            SoltarLatigo(darImpulso: true);
        }

        // --- LÓGICA DEL IMPULSO RÁPIDO EN EJECUCIÓN ---
        if (realizandoImpulsoRapido)
        {
            Vector2 centroPersonaje = rb.position;
            float distanciaAlPivote = Vector2.Distance(centroPersonaje, puntoAgarrado);

            Vector2 direccionHaciaPivote = (puntoAgarrado - centroPersonaje).normalized;
            rb.linearVelocity = direccionHaciaPivote * fuerzaImpulsoRapido;

            if (distanciaAlPivote <= distanciaMinimaLlegada)
            {
                FinalizarImpulsoRapido();
            }
        }

        // --- LÓGICA DEL BALANCEO NORMAL ---
        if (estaEnganchado && !realizandoImpulsoRapido)
        {
            if (rb.position.y > puntoAgarrado.y + anguloLimiteDesenganche)
            {
                SoltarLatigo(darImpulso: false);
                return;
            }

            float inputV = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputV = -1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputV = 1f;

            distanceJoint.distance += inputV * velocidadRecoger * Time.deltaTime;
            distanceJoint.distance = Mathf.Clamp(distanceJoint.distance, 1f, alcanceMaximo);
        }
    }

    void FixedUpdate()
    {
        if (estaEnganchado && !realizandoImpulsoRapido)
        {
            float inputH = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputH = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputH = 1f;

            if (Mathf.Abs(inputH) > 0.1f)
            {
                rb.AddForce(new Vector2(inputH * fuerzaBalanceo, 0), ForceMode2D.Force);
            }
            else
            {
                Vector2 velActual = rb.linearVelocity;
                velActual.x = Mathf.Lerp(velActual.x, 0f, Time.fixedDeltaTime * friccionAireBalanceo);
                rb.linearVelocity = velActual;
            }
        }
    }

    // --- DIBUJO RÍGIDO Y DIRECTO DE LA CUERDA EN LateUpdate ---
    void LateUpdate()
    {
        if (estaEnganchado || realizandoImpulsoRapido)
        {
            Vector2 origen = puntoDisparo != null ? (Vector2)puntoDisparo.position : rb.position;
            lineRenderer.SetPosition(0, origen);
            lineRenderer.SetPosition(1, puntoAgarrado);
        }
    }

    private void GenerarEfectoImpacto(Vector2 posicion)
    {
        if (vfxImpactoPrefabs != null)
        {
            ParticleSystem vfx = Instantiate(vfxImpactoPrefabs, posicion, Quaternion.identity);
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & capaEnganche) == 0)
        {
            ultimoPivoteUsado = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (((1 << collider.gameObject.layer) & capaEnganche) == 0)
        {
            ultimoPivoteUsado = null;
        }
    }

    private bool EsElMismoPivote(Vector2 nuevoPunto)
    {
        if (!ultimoPivoteUsado.HasValue) return false;
        return Vector2.Distance(ultimoPivoteUsado.Value, nuevoPunto) <= TOLERANCIA_MISMO_PIVOTE;
    }

    private bool PuedeUsarLatigoNormal()
    {
        return Time.time >= tiempoSiguienteLatigoNormal && !estaEnganchado && !realizandoImpulsoRapido;
    }

    private bool PuedeUsarImpulsoRapido()
    {
        return Time.time >= tiempoSiguienteImpulsoRapido && !realizandoImpulsoRapido;
    }

    private void IntentarEngancharConMouse()
    {
        Vector3 posicionMouse = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 centroPersonaje = rb.position; 
        Vector2 direccion = ((Vector2)posicionMouse - centroPersonaje).normalized;

        RaycastHit2D hit = Physics2D.CircleCast(centroPersonaje, radioMagnetismo, direccion, alcanceMaximo, capaEnganche);

        if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Enganche"))
        {
            Vector2 puntoEngancheExacto = hit.transform.position;

            if (EsElMismoPivote(puntoEngancheExacto)) return;

            tiempoSiguienteLatigoNormal = Time.time + cooldownLatigoNormal;
            EngancharAPunto(puntoEngancheExacto);
        }
    }

    private void IntentarImpulsoRapidoConMouse()
    {
        Vector3 posicionMouse = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 centroPersonaje = rb.position; 
        Vector2 direccion = ((Vector2)posicionMouse - centroPersonaje).normalized;

        RaycastHit2D hit = Physics2D.CircleCast(centroPersonaje, radioMagnetismo, direccion, alcanceMaximo, capaEnganche);

        if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Enganche"))
        {
            Vector2 puntoEngancheExacto = hit.transform.position;

            if (EsElMismoPivote(puntoEngancheExacto)) return;

            tiempoSiguienteImpulsoRapido = Time.time + cooldownImpulsoRapido;
            IniciarImpulsoRapido(puntoEngancheExacto);
        }
    }

    private void IntentarEngancharAuto()
    {
        Transform pivoteCercano = ObtenerPivoteMasCercano();
        if (pivoteCercano != null)
        {
            if (EsElMismoPivote(pivoteCercano.position)) return;

            tiempoSiguienteLatigoNormal = Time.time + cooldownLatigoNormal;
            EngancharAPunto(pivoteCercano.position);
        }
    }

    private void IntentarImpulsoRapidoAuto()
    {
        Transform pivoteCercano = ObtenerPivoteMasCercano();
        if (pivoteCercano != null)
        {
            if (EsElMismoPivote(pivoteCercano.position)) return;

            tiempoSiguienteImpulsoRapido = Time.time + cooldownImpulsoRapido;
            IniciarImpulsoRapido(pivoteCercano.position);
        }
    }

    private Transform ObtenerPivoteMasCercano()
    {
        Collider2D[] colisionadores = Physics2D.OverlapCircleAll(rb.position, alcanceMaximo, capaEnganche);
        Transform mejorPivote = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Collider2D col in colisionadores)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Enganche"))
            {
                if (EsElMismoPivote(col.transform.position)) continue;

                float distancia = Vector2.Distance(rb.position, col.transform.position);
                if (distancia <= alcanceMaximo && distancia < menorDistancia)
                {
                    menorDistancia = distancia;
                    mejorPivote = col.transform;
                }
            }
        }

        return mejorPivote;
    }

    private void EngancharAPunto(Vector2 punto)
    {
        CancelarImpulsoRapidoSinInercia();

        puntoAgarrado = punto;
        ultimoPivoteUsado = puntoAgarrado;

        distanceJoint.connectedAnchor = puntoAgarrado;

        float distanciaCalculada = Vector2.Distance(rb.position, puntoAgarrado);
        distanceJoint.distance = distanciaCalculada;

        distanceJoint.enabled = true;
        lineRenderer.enabled = true;
        estaEnganchado = true;

        GenerarEfectoImpacto(puntoAgarrado);

        if (playerController != null) playerController.estaEnLatigo = true;
    }

    private void IniciarImpulsoRapido(Vector2 punto)
    {
        if (estaEnganchado)
        {
            distanceJoint.enabled = false;
            estaEnganchado = false;
        }

        puntoAgarrado = punto;
        ultimoPivoteUsado = puntoAgarrado;

        rb.gravityScale = 0f;

        Vector2 direccionHaciaPivote = (puntoAgarrado - rb.position).normalized;
        rb.linearVelocity = direccionHaciaPivote * fuerzaImpulsoRapido;

        lineRenderer.enabled = true;
        realizandoImpulsoRapido = true;

        GenerarEfectoImpacto(puntoAgarrado);

        if (playerController != null) playerController.estaEnLatigo = true;
    }

    private void FinalizarImpulsoRapido()
    {
        realizandoImpulsoRapido = false;
        lineRenderer.enabled = false;
        
        rb.gravityScale = gravedadOriginal;

        float inerciaHorizontal = rb.linearVelocity.x;

        if (Mathf.Abs(inerciaHorizontal) < 2f)
        {
            Vector2 direccionPivote = (puntoAgarrado - rb.position).normalized;
            inerciaHorizontal = direccionPivote.x * fuerzaImpulsoRapido;
        }

        rb.linearVelocity = new Vector2(inerciaHorizontal, impulsosSalidaExtra);

        if (playerController != null)
        {
            playerController.estaEnLatigo = false;
            playerController.ActivarInerciaProtegida(0.5f);
        }
    }

    private void CancelarImpulsoRapidoSinInercia()
    {
        if (realizandoImpulsoRapido)
        {
            realizandoImpulsoRapido = false;
            lineRenderer.enabled = false;
            rb.gravityScale = gravedadOriginal;
            if (playerController != null) playerController.estaEnLatigo = false;
        }
    }

    private void SoltarLatigo(bool darImpulso)
    {
        distanceJoint.enabled = false;
        lineRenderer.enabled = false;
        estaEnganchado = false;
        
        CancelarImpulsoRapidoSinInercia();

        if (playerController != null) playerController.estaEnLatigo = false;

        if (darImpulso)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, impulsoSaltoLatigo);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceMaximo);
    }
}