using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Mantiene un disco luminoso en el suelo justo debajo de Kael (marca dónde va a aterrizar). El disco se
///   encoge y se apaga con la altura.
///
/// POR QUÉ EXISTE
///   En un juego de plataformas 3D es difícil juzgar la distancia al suelo y, con la esfera de Kael sobre
///   piedra oscura, a veces ni se ve dónde está. El marcador resuelve las dos cosas con un objeto barato.
///
/// CÓMO SE USA
///   Va en un objeto hijo de Kael (el disco, sin collider). Lanza un rayo hacia abajo cada fotograma; si
///   no hay suelo (vacío), el disco se oculta.
/// </summary>
public class GroundMarker : MonoBehaviour
{
    [Tooltip("Alcance máximo del rayo hacia abajo (metros).")]
    [SerializeField] private float maxDistance = 30f;

    [Tooltip("Diámetro del disco en el suelo (metros).")]
    [SerializeField] private float baseSize = 1.1f;

    [Tooltip("Diámetro mínimo cuando Kael está muy alto (metros).")]
    [SerializeField] private float minSize = 0.35f;

    [Tooltip("Altura a la que el disco llega a su tamaño mínimo (metros).")]
    [SerializeField] private float fullHeight = 8f;

    private Renderer markerRenderer;
    private Transform parentTransform;

    private void Awake()
    {
        markerRenderer = GetComponentInChildren<Renderer>();
        parentTransform = transform.parent;
    }

    private void LateUpdate()
    {
        if (markerRenderer == null || parentTransform == null)
        {
            return;
        }

        Vector3 origin = parentTransform.position + Vector3.up * 0.5f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            markerRenderer.enabled = false;
            return;
        }

        markerRenderer.enabled = true;
        float height = Mathf.Max(origin.y - 0.5f - hit.point.y, 0f);
        float size = Mathf.Lerp(baseSize, minSize, Mathf.Clamp01(height / fullHeight));

        // Un poco por encima del suelo para no parpadear con la superficie (z-fighting).
        transform.SetPositionAndRotation(hit.point + hit.normal * 0.03f, Quaternion.FromToRotation(Vector3.up, hit.normal));
        transform.localScale = new Vector3(size, 0.01f, size);
    }
}
