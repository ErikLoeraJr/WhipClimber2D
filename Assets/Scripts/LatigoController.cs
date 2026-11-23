using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LatigoController : MonoBehaviour
{
    [Header("Configuración del Látigo Normal")]
    [Range(3f, 25f)]
    [SerializeField] private float alcanceMaximo = 8f;
    [SerializeField] private LayerMask capaEnganche;
    [SerializeField] private float velocidadLanzamiento = 30f;
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

    [Header("Impulso Vertical en Boost")]
    [Tooltip("Multiplicador que amplifica la inercia vertical al salir disparado desde abajo.")]
    [SerializeField] private float multiplicadorInerciaVertical = 1.5f;
    [Tooltip("Fuerza mínima de salida hacia arriba asegurada al llegar al pivote.")]
    [SerializeField] private float impulsoVerticalMinimo = 18f;

    [Header("Feedback Visual y VFX")]
    [Tooltip("Prefab de Partículas que se instanciará en el pivote al enganchar.")]
    [SerializeField] private ParticleSystem vfxImpactoPrefabs;

    [Header("Audio del Látigo")]
    [Tooltip("AudioSource para efectos puntuales (disparo, impacto).")]
    [SerializeField] private AudioSource audioEfectos;
    [Tooltip("AudioSource con Loop activado para el sonido continuo de balanceo.")]
    [SerializeField] private AudioSource audioBalanceoLoop;
    [SerializeField] private AudioClip sfxLanzarLatigo;
    [SerializeField] private AudioClip sfxEngancharPivote;
    [SerializeField] private AudioClip sfxImpulsoRapido;
    [Tooltip("Velocidad a la que el sonido de balanceo alcanza su volumen máximo.")]
    [SerializeField] private float velocidadParaVolumenMaximo = 14f;

    [Header("Referencias")]
    [SerializeField] private Transform puntoDisparo;

    private DistanceJoint2D distanceJoint;
    private LineRenderer lineRenderer;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private Animator anim;

    private Vector2 puntoAgarrado;
    private bool estaEnganchado = false;
    private bool estaLanzando = false;
    private bool realizandoImpulsoRapido = false;
    private float gravedadOriginal;

    private Vector2 direccionEntradaBoost;
    private Coroutine rutinaLanzamiento;

    private Vector2? ultimoPivoteUsado = null;
    private const float TOLERANCIA_MISMO_PIVOTE = 0.2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        distanceJoint = GetComponent<DistanceJoint2D>();
        lineRenderer = GetComponent<LineRenderer>();
        playerController = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();

        gravedadOriginal = rb.gravityScale;
        distanceJoint.enabled = false;
        lineRenderer.enabled = false;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
        }

        if (audioBalanceoLoop != null)
        {
            audioBalanceoLoop.loop = true;
            audioBalanceoLoop.volume = 0f;
        }
    }

    void Update()
    {
        // --- 1. LÁTIGO NORMAL ---
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
            if (!realizandoImpulsoRapido && (estaEnganchado || estaLanzando)) SoltarLatigo(darImpulso: false);
        }

        // --- 2. IMPULSO RÁPIDO ---
        if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (PuedeUsarImpulsoRapido()) IntentarImpulsoRapidoConMouse();
        }
        else if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (PuedeUsarImpulsoRapido()) IntentarImpulsoRapidoAuto();
        }

        // Salto manual para catapultarse
        if (Keyboard.current.spaceKey.wasPressedThisFrame && (estaEnganchado || estaLanzando || realizandoImpulsoRapido))
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

        ActualizarAudioBalanceo();
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

    void LateUpdate()
    {
        if (estaEnganchado || realizandoImpulsoRapido)
        {
            Vector2 origen = puntoDisparo != null ? (Vector2)puntoDisparo.position : rb.position;
            lineRenderer.SetPosition(0, origen);
            lineRenderer.SetPosition(1, puntoAgarrado);
        }
    }

    private void ActualizarAudioBalanceo()
    {
        if (audioBalanceoLoop == null) return;

        if (estaEnganchado && !realizandoImpulsoRapido)
        {
            if (!audioBalanceoLoop.isPlaying)
            {
                audioBalanceoLoop.Play();
            }

            // El volumen y tono aumentan proporcionalmente a la velocidad de balanceo
            float factorVelocidad = Mathf.Clamp01(rb.linearVelocity.magnitude / velocidadParaVolumenMaximo);
            audioBalanceoLoop.volume = Mathf.Lerp(audioBalanceoLoop.volume, factorVelocidad, Time.deltaTime * 10f);
            audioBalanceoLoop.pitch = Mathf.Lerp(0.85f, 1.35f, factorVelocidad);
        }
        else
        {
            if (audioBalanceoLoop.isPlaying)
            {
                audioBalanceoLoop.volume = Mathf.Lerp(audioBalanceoLoop.volume, 0f, Time.deltaTime * 12f);
                if (audioBalanceoLoop.volume <= 0.02f)
                {
                    audioBalanceoLoop.Stop();
                }
            }
        }
    }

    private void ReproducirSonido(AudioClip clip, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        if (audioEfectos != null && clip != null)
        {
            // Variación leve de tono para que no suene repetitivo
            audioEfectos.pitch = Random.Range(pitchMin, pitchMax);
            audioEfectos.PlayOneShot(clip);
        }
    }

    private void IntentarEngancharConMouse()
    {
        Vector3 posicionMouse = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 centroPersonaje = rb.position;
        Vector2 direccion = ((Vector2)posicionMouse - centroPersonaje).normalized;

        RaycastHit2D hit = Physics2D.CircleCast(centroPersonaje, radioMagnetismo, direccion, alcanceMaximo, capaEnganche);

        if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Enganche"))
        {
            Vector2 puntoEngancheExacto = hit.collider.bounds.center;

            if (EsElMismoPivote(puntoEngancheExacto)) return;

            tiempoSiguienteLatigoNormal = Time.time + cooldownLatigoNormal;
            rutinaLanzamiento = StartCoroutine(LanzarLatigoRutina(puntoEngancheExacto));
        }
    }

    private void IntentarEngancharAuto()
    {
        Transform pivoteCercano = ObtenerPivoteMasCercano();
        if (pivoteCercano != null)
        {
            Collider2D col = pivoteCercano.GetComponent<Collider2D>();
            Vector2 puntoDestino = col != null ? (Vector2)col.bounds.center : (Vector2)pivoteCercano.position;

            if (EsElMismoPivote(puntoDestino)) return;

            tiempoSiguienteLatigoNormal = Time.time + cooldownLatigoNormal;
            rutinaLanzamiento = StartCoroutine(LanzarLatigoRutina(puntoDestino));
        }
    }

    private IEnumerator LanzarLatigoRutina(Vector2 puntoDestino)
    {
        CancelarImpulsoRapidoSinInercia();

        estaLanzando = true;
        lineRenderer.enabled = true;

        // Sonido al lanzar la cuerda
        ReproducirSonido(sfxLanzarLatigo, 0.95f, 1.1f);

        if (anim != null)
        {
            anim.SetBool("estaEnganchado", true);
            anim.SetBool("estaColgada", false);
        }

        Vector2 origenInicial = puntoDisparo != null ? (Vector2)puntoDisparo.position : rb.position;
        float distanciaTotal = Vector2.Distance(origenInicial, puntoDestino);
        float distanciaActual = 0f;

        while (distanciaActual < distanciaTotal)
        {
            if (!estaLanzando) yield break;

            distanciaActual += velocidadLanzamiento * Time.deltaTime;
            float porcentaje = Mathf.Clamp01(distanciaActual / distanciaTotal);

            Vector2 puntoActualPunta = Vector2.Lerp(origenInicial, puntoDestino, porcentaje);
            Vector2 origenActual = puntoDisparo != null ? (Vector2)puntoDisparo.position : rb.position;

            lineRenderer.SetPosition(0, origenActual);
            lineRenderer.SetPosition(1, puntoActualPunta);

            yield return null;
        }

        puntoAgarrado = puntoDestino;
        ultimoPivoteUsado = puntoAgarrado;

        distanceJoint.connectedAnchor = puntoAgarrado;
        distanceJoint.distance = Vector2.Distance(rb.position, puntoAgarrado);

        distanceJoint.enabled = true;
        estaEnganchado = true;
        estaLanzando = false;

        if (anim != null)
        {
            anim.SetBool("estaColgada", true);
        }

        // Sonido de impacto al engancharse
        ReproducirSonido(sfxEngancharPivote, 0.9f, 1.05f);
        GenerarEfectoImpacto(puntoAgarrado);

        if (playerController != null) playerController.estaEnLatigo = true;
    }

    private void IntentarImpulsoRapidoConMouse()
    {
        Vector3 posicionMouse = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 centroPersonaje = rb.position;
        Vector2 direccion = ((Vector2)posicionMouse - centroPersonaje).normalized;

        RaycastHit2D hit = Physics2D.CircleCast(centroPersonaje, radioMagnetismo, direccion, alcanceMaximo, capaEnganche);

        if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Enganche"))
        {
            Vector2 puntoEngancheExacto = hit.collider.bounds.center;

            if (EsElMismoPivote(puntoEngancheExacto)) return;

            tiempoSiguienteImpulsoRapido = Time.time + cooldownImpulsoRapido;
            IniciarImpulsoRapido(puntoEngancheExacto);
        }
    }

    private void IntentarImpulsoRapidoAuto()
    {
        Transform pivoteCercano = ObtenerPivoteMasCercano();
        if (pivoteCercano != null)
        {
            Collider2D col = pivoteCercano.GetComponent<Collider2D>();
            Vector2 puntoDestino = col != null ? (Vector2)col.bounds.center : (Vector2)pivoteCercano.position;

            if (EsElMismoPivote(puntoDestino)) return;

            tiempoSiguienteImpulsoRapido = Time.time + cooldownImpulsoRapido;
            IniciarImpulsoRapido(puntoDestino);
        }
    }

    private void IniciarImpulsoRapido(Vector2 punto)
    {
        if (rutinaLanzamiento != null) StopCoroutine(rutinaLanzamiento);

        if (estaEnganchado)
        {
            distanceJoint.enabled = false;
            estaEnganchado = false;
        }

        puntoAgarrado = punto;
        ultimoPivoteUsado = puntoAgarrado;

        rb.gravityScale = 0f;

        direccionEntradaBoost = (puntoAgarrado - rb.position).normalized;
        rb.linearVelocity = direccionEntradaBoost * fuerzaImpulsoRapido;

        lineRenderer.enabled = true;
        realizandoImpulsoRapido = true;

        if (anim != null)
        {
            anim.SetBool("estaEnganchado", true);
            anim.SetBool("estaColgada", true);
        }

        // Sonido al engancharse con impulso rápido
        ReproducirSonido(sfxEngancharPivote, 1.05f, 1.15f);
        ReproducirSonido(sfxImpulsoRapido, 0.95f, 1.05f);
        GenerarEfectoImpacto(puntoAgarrado);

        if (playerController != null) playerController.estaEnLatigo = true;
    }

    private void FinalizarImpulsoRapido()
    {
        realizandoImpulsoRapido = false;
        lineRenderer.enabled = false;

        rb.gravityScale = gravedadOriginal;

        if (anim != null)
        {
            anim.SetBool("estaEnganchado", false);
            anim.SetBool("estaColgada", false);
        }

        float inerciaHorizontal = rb.linearVelocity.x;
        if (Mathf.Abs(inerciaHorizontal) < 2f)
        {
            inerciaHorizontal = direccionEntradaBoost.x * fuerzaImpulsoRapido;
        }

        float inerciaVertical = 0f;

        if (direccionEntradaBoost.y > 0.1f)
        {
            float impulsoVerticalCalculado = direccionEntradaBoost.y * fuerzaImpulsoRapido * multiplicadorInerciaVertical;
            inerciaVertical = Mathf.Max(impulsoVerticalCalculado, impulsoVerticalMinimo);
        }
        else
        {
            inerciaVertical = impulsosSalidaExtra;
        }

        rb.linearVelocity = new Vector2(inerciaHorizontal, inerciaVertical);

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

            if (anim != null)
            {
                anim.SetBool("estaEnganchado", false);
                anim.SetBool("estaColgada", false);
            }

            if (playerController != null) playerController.estaEnLatigo = false;
        }
    }

    private void SoltarLatigo(bool darImpulso)
    {
        if (rutinaLanzamiento != null)
        {
            StopCoroutine(rutinaLanzamiento);
        }

        estaLanzando = false;
        distanceJoint.enabled = false;
        lineRenderer.enabled = false;
        estaEnganchado = false;

        if (anim != null)
        {
            anim.SetBool("estaEnganchado", false);
            anim.SetBool("estaColgada", false);
        }

        CancelarImpulsoRapidoSinInercia();

        if (playerController != null) playerController.estaEnLatigo = false;

        if (darImpulso)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, impulsoSaltoLatigo);
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
                Vector2 centro = col.bounds.center;
                if (EsElMismoPivote(centro)) continue;

                float distancia = Vector2.Distance(rb.position, centro);
                if (distancia <= alcanceMaximo && distancia < menorDistancia)
                {
                    menorDistancia = distancia;
                    mejorPivote = col.transform;
                }
            }
        }

        return mejorPivote;
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
        return Time.time >= tiempoSiguienteLatigoNormal && !estaEnganchado && !estaLanzando && !realizandoImpulsoRapido;
    }

    private bool PuedeUsarImpulsoRapido()
    {
        return Time.time >= tiempoSiguienteImpulsoRapido && !realizandoImpulsoRapido;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceMaximo);
    }
}