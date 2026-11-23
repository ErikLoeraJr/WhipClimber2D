using System.Collections.Generic;
using UnityEngine;

public class GeneradorMundoInfinito : MonoBehaviour
{
    [Header("Referencias Principales")]
    [SerializeField] private Transform jugadora; // Referencia al Transform de la jugadora

    [Header("Prefabs de Secciones/Chunks")]
    [SerializeField] private GameObject chunkInicial; // El primer bloque/suelo donde aparece la jugadora
    [SerializeField] private List<GameObject> prefabsChunks; // Lista con tus diferentes módulos de nivel

    [Header("Configuración del Generador")]
    [SerializeField] private float anchoChunk = 20f; // Longitud horizontal de cada bloque de nivel
    [SerializeField] private int chunksVisiblesEnPantalla = 5; // Cuántos bloques se mantienen activos a la vez
    [SerializeField] private float distanciaDestruccionAtras = 30f; // Distancia detrás de la jugadora para eliminar bloques viejos

    // Control interno de posición y lista activa
    private float puntoGeneracionX = 0f;
    private List<GameObject> chunksActivos = new List<GameObject>();

    private void Start()
    {
        if (jugadora == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) jugadora = playerObj.transform;
        }

        // Si existe un chunk inicial ya colocado en la escena, registrarlo
        if (chunkInicial != null)
        {
            chunksActivos.Add(chunkInicial);
            puntoGeneracionX = chunkInicial.transform.position.x + anchoChunk;
        }
        else
        {
            puntoGeneracionX = jugadora.position.x - (anchoChunk / 2f);
        }

        // Generar los primeros chunks hacia adelante
        for (int i = 0; i < chunksVisiblesEnPantalla; i++)
        {
            GenerarSiguienteChunk();
        }
    }

    private void Update()
    {
        if (jugadora == null) return;

        // Si la jugadora avanza lo suficiente, generar un nuevo chunk adelante
        float limiteAdelante = puntoGeneracionX - (chunksVisiblesEnPantalla * anchoChunk);
        if (jugadora.position.x > limiteAdelante)
        {
            GenerarSiguienteChunk();
        }

        // Eliminar o reciclar chunks que quedaron muy atrás
        LimpiarChunksAtras();
    }

    private void GenerarSiguienteChunk()
    {
        if (prefabsChunks == null || prefabsChunks.Count == 0) return;

        // Seleccionar un prefab aleatorio de la lista
        int indiceAleatorio = Random.Range(0, prefabsChunks.Count);
        GameObject prefabSeleccionado = prefabsChunks[indiceAleatorio];

        // Instanciar el nuevo chunk alineado al punto X actual
        Vector3 posicionGeneracion = new Vector3(puntoGeneracionX, 0f, 0f);
        GameObject nuevoChunk = Instantiate(prefabSeleccionado, posicionGeneracion, Quaternion.identity, transform);

        chunksActivos.Add(nuevoChunk);

        // Avanzar el punto X para el próximo módulo
        puntoGeneracionX += anchoChunk;
    }

    private void LimpiarChunksAtras()
    {
        if (chunksActivos.Count == 0) return;

        // Comprobar si el primer chunk de la lista quedó demasiado atrás de la jugadora
        GameObject primerChunk = chunksActivos[0];
        if (primerChunk != null && jugadora.position.x - primerChunk.transform.position.x > distanciaDestruccionAtras)
        {
            chunksActivos.RemoveAt(0);
            Destroy(primerChunk);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibuja guías amarillas en la vista Scene para visualizar el ancho de generación
        Gizmos.color = Color.cyan;
        Vector3 posGizmo = transform.position;
        Gizmos.DrawWireCube(posGizmo + new Vector3(anchoChunk / 2f, 0, 0), new Vector3(anchoChunk, 10f, 0));
    }
}