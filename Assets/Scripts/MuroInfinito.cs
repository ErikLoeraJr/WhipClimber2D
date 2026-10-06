using UnityEngine;

public class MuroInfinito : MonoBehaviour
{
    public Transform camara; // O la Jugadora
    private float posicionXFija;

    void Start()
    {
        // Guardar la posición horizontal original del muro
        posicionXFija = transform.position.x;

        if (camara == null && Camera.main != null)
        {
            camara = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camara != null)
        {
            // El muro copia la altura Y de la cámara, pero mantiene su X fija
            transform.position = new Vector3(posicionXFija, camara.position.y, transform.position.z);
        }
    }
}