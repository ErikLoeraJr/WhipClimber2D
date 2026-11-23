using System.Collections.Generic;
using UnityEngine;

public class GeneradorProceduralVertical : MonoBehaviour
{
    [Header("Referencias Principales")]
    [SerializeField] private Transform jugadora;
    [SerializeField] private GameObject prefabPuntoEnganche;
    [SerializeField] private GameObject prefabPlataformaGrande;
    [SerializeField] private GameObject prefabPlataformaPequena;

    [Header("Punto de Inicio de Generación")]
    [SerializeField] private float alturaInicioGeneracion = 15f; 

    [Header("Parámetros de Vista y Limpieza")]
    [SerializeField] private float distanciaVistaArriba = 22f;
    [SerializeField] private float distanciaLimpiezaAbajo = 18f;

    [Header("Límites del Mapa (Eje X)")]
    [SerializeField] private float limiteIzquierdaX = -6f;
    [SerializeField] private float limiteDerechaX = 6f;

    [Header("Progreso y Curva de Dificultad")]
    [SerializeField] private float alturaMaximaDificultad = 150f;
    
    [Header("Reglas de Separación Inteligente (Anti-Encimado)")]
    [Tooltip("Distancia X mínima requerida cuando dos elementos se generan a poca altura (Y).")]
    [SerializeField] private float separacionXSiYCerca = 5.0f;
    [Tooltip("Distancia Y que se considera 'cerca' para activar la regla.")]
    [SerializeField] private float umbralYCerca = 4.0f;

    [SerializeField] private float separacionVerticalMinFácil = 3.5f;
    [SerializeField] private float separacionVerticalMaxFácil = 5.0f;
    [SerializeField] private float separacionVerticalMinDificil = 5.0f;
    [SerializeField] private float separacionVerticalMaxDificil = 7.0f;

    // Control de estado
    private float ultimoYGenerado = 0f;
    private Vector2 ultimaPosicionElemento = Vector2.zero; // Guarda la posición (X, Y) del último objeto
    private List<GameObject> elementosGenerados = new List<GameObject>();

    private void Start()
    {
        if (jugadora == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) jugadora = playerObj.transform;
        }

        ultimoYGenerado = alturaInicioGeneracion;
        
        if (jugadora != null)
        {
            ultimaPosicionElemento = new Vector2(jugadora.position.x, ultimoYGenerado);
        }
    }

    private void Update()
    {
        if (jugadora == null) return;

        while (ultimoYGenerado < jugadora.position.y + distanciaVistaArriba)
        {
            GenerarNivelProcedural();
        }

        LimpiarElementosAbajo();
    }

    private void GenerarNivelProcedural()
    {
        // 1. FACTOR DE DIFICULTAD
        float progresoAltura = Mathf.Clamp01((ultimoYGenerado - alturaInicioGeneracion) / alturaMaximaDificultad);

        // 2. INCREMENTO VERTICAL (EJE Y)
        float sepMinActual = Mathf.Lerp(separacionVerticalMinFácil, separacionVerticalMinDificil, progresoAltura);
        float sepMaxActual = Mathf.Lerp(separacionVerticalMaxFácil, separacionVerticalMaxDificil, progresoAltura);

        float pasoY = Random.Range(sepMinActual, sepMaxActual);
        ultimoYGenerado += pasoY;

        float deltaY = ultimoYGenerado - ultimaPosicionElemento.y;

        // 3. GENERAR PIVOTE O PLATAFORMA RESPETANDO LA REGLA X vs Y
        float probabilidadPlataforma = Mathf.Lerp(60f, 15f, progresoAltura);
        bool esPlataforma = Random.Range(0f, 100f) <= probabilidadPlataforma;

        // Obtener una posición X que garantice distancia lejana si Y es pequeña
        float posXValida = CalcularPosXConRelacionY(deltaY);
        Vector3 nuevaPosicion = new Vector3(posXValida, ultimoYGenerado, 0f);

        if (esPlataforma)
        {
            bool usarGrande = Random.Range(0f, 1f) > progresoAltura;
            GameObject prefabElegido = (usarGrande && prefabPlataformaGrande != null) ? prefabPlataformaGrande : prefabPlataformaPequena;

            if (prefabElegido != null)
            {
                InstanciarObjeto(prefabElegido, nuevaPosicion);
                ultimaPosicionElemento = nuevaPosicion;
            }
        }
        else
        {
            if (prefabPuntoEnganche != null)
            {
                InstanciarObjeto(prefabPuntoEnganche, nuevaPosicion);
                ultimaPosicionElemento = nuevaPosicion;
            }
        }
    }

    // ALGORITMO REVISE Y VALIDE LA REGLA "X DISTANTE SI Y ESTÁ CERCA"
    private float CalcularPosXConRelacionY(float deltaY)
    {
        float nuevaX = Random.Range(limiteIzquierdaX, limiteDerechaX);
        int intentos = 0;

        // Si la distancia vertical respecto al objeto anterior es chica (menor a umbralYCerca)
        if (deltaY < umbralYCerca)
        {
            // Forzamos a que la diferencia en X sea mayor a separacionXSiYCerca
            while (Mathf.Abs(nuevaX - ultimaPosicionElemento.x) < separacionXSiYCerca && intentos < 15)
            {
                nuevaX = Random.Range(limiteIzquierdaX, limiteDerechaX);
                intentos++;
            }

            // Si por los límites del mapa no encuentra espacio, lo manda al lado opuesto del mapa
            if (intentos >= 15)
            {
                nuevaX = (ultimaPosicionElemento.x > 0) ? Random.Range(limiteIzquierdaX, -1f) : Random.Range(1f, limiteDerechaX);
            }
        }

        return nuevaX;
    }

    private void InstanciarObjeto(GameObject prefab, Vector3 posicion)
    {
        if (prefab == null) return;
        GameObject nuevoObj = Instantiate(prefab, posicion, Quaternion.identity, transform);
        elementosGenerados.Add(nuevoObj);
    }

    private void LimpiarElementosAbajo()
    {
        for (int i = elementosGenerados.Count - 1; i >= 0; i--)
        {
            GameObject obj = elementosGenerados[i];
            if (obj != null)
            {
                if (jugadora.position.y - obj.transform.position.y > distanciaLimpiezaAbajo)
                {
                    elementosGenerados.RemoveAt(i);
                    Destroy(obj);
                }
            }
            else
            {
                elementosGenerados.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(limiteIzquierdaX - 2, alturaInicioGeneracion, 0), new Vector3(limiteDerechaX + 2, alturaInicioGeneracion, 0));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(limiteIzquierdaX, alturaInicioGeneracion - 5, 0), new Vector3(limiteIzquierdaX, alturaInicioGeneracion + 30, 0));
        Gizmos.DrawLine(new Vector3(limiteDerechaX, alturaInicioGeneracion - 5, 0), new Vector3(limiteDerechaX, alturaInicioGeneracion + 30, 0));
    }
}