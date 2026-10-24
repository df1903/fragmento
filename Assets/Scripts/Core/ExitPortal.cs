using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   El Portal de Salida: está sellado (disco apagado, sin trigger) hasta que el Núcleo se restaura.
///   Entonces el disco de luz crece y, si Kael lo cruza, avisa con GameEvents.LevelCompleted.
///
/// POR QUÉ EXISTE
///   Regla del juego: "el Portal está sellado hasta restaurar el Núcleo". Solo escucha CoreRestored; no conoce
///   al Núcleo ni a los fragmentos.
///
/// CÓMO SE USA
///   Va en el objeto Portal, con un Collider marcado como Trigger. "Sealed Visual" es lo que se ve sellado,
///   "Open Visual" el disco de luz (empieza desactivado). Fragmento > Construir Escena lo arma.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitPortal : MonoBehaviour
{
    [Tooltip("Aspecto del portal sellado (se oculta al abrirse).")]
    [SerializeField] private GameObject sealedVisual;

    [Tooltip("Disco de luz del portal abierto (empieza desactivado).")]
    [SerializeField] private Transform openVisual;

    [Tooltip("Luz del portal (opcional): se enciende al abrirse.")]
    [SerializeField] private Light portalLight;

    [Tooltip("Segundos que tarda el disco en abrirse.")]
    [SerializeField] private float openSeconds = 1.5f;

    private Collider trigger;
    private bool isOpen;
    private bool completed;
    private Vector3 openScale;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        trigger.enabled = false;

        if (openVisual != null)
        {
            openScale = openVisual.localScale;
            openVisual.gameObject.SetActive(false);
        }

        if (portalLight != null)
        {
            portalLight.enabled = false;
        }
    }

    private void OnEnable()
    {
        GameEvents.CoreRestored += Open;
    }

    private void OnDisable()
    {
        GameEvents.CoreRestored -= Open;
    }

    private void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        StartCoroutine(OpenAnimation());
    }

    private IEnumerator OpenAnimation()
    {
        if (sealedVisual != null)
        {
            sealedVisual.SetActive(false);
        }

        if (portalLight != null)
        {
            portalLight.enabled = true;
        }

        if (openVisual != null)
        {
            openVisual.localScale = openScale * 0.01f;
            openVisual.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < openSeconds)
            {
                elapsed += Time.deltaTime;
                openVisual.localScale = Vector3.Lerp(openScale * 0.01f, openScale, Mathf.SmoothStep(0f, 1f, elapsed / openSeconds));
                yield return null;
            }
            openVisual.localScale = openScale;
        }

        GameEvents.RaiseMessageRequested("El portal está abierto.");
        trigger.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isOpen || completed || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        completed = true;
        GameEvents.RaiseLevelCompleted();
    }
}
