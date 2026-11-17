using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Placa de presión: cuando una caja (o Kael, si se permite) se pone encima, se hunde y lanza un evento de
///   mundo por su id, igual que hace un fragmento. Se queda pulsada para siempre.
///
/// POR QUÉ EXISTE
///   Es el puzle del nivel 7: empujar la caja hasta la placa abre una puerta. Reutiliza los eventos de mundo que
///   ya existen (DoorOpen, MoveOnEvent...): la placa no sabe qué abre, solo avisa.
///
/// CÓMO SE USA
///   Objeto con BoxCollider (se fuerza a Trigger: es la zona sobre la placa) y este script. "Event Id" debe
///   coincidir con el del evento de mundo. "Plate" es la pieza visual que se hunde (opcional).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class PressurePlate : MonoBehaviour
{
    [Tooltip("Id del evento de mundo que se lanza al pulsarla.")]
    [SerializeField] private string eventId = "";

    [Tooltip("Si está marcado, solo una caja (Rigidbody) la pulsa; Kael no pesa lo suficiente.")]
    [SerializeField] private bool requireCrate = true;

    [Tooltip("Pieza visual que se hunde (opcional).")]
    [SerializeField] private Transform plate;

    [Tooltip("Mensaje que se muestra al pulsarla (opcional).")]
    [SerializeField] private string message = "";

    /// <summary>True cuando ya fue pulsada.</summary>
    public bool IsPressed { get; private set; }

    /// <summary>Configura la placa desde código (la usa el constructor de escena).</summary>
    public void Setup(string id, bool onlyCrate, Transform plateTransform, string text)
    {
        eventId = id;
        requireCrate = onlyCrate;
        plate = plateTransform;
        message = text;
    }

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    // OnTriggerStay se llama en cada paso de física mientras algo sigue dentro: sirve aunque la caja llegue despacio.
    private void OnTriggerStay(Collider other)
    {
        if (IsPressed)
        {
            return;
        }

        bool isCrate = other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic;
        bool isPlayer = other.GetComponentInParent<PlayerController>() != null;
        if (!isCrate && (requireCrate || !isPlayer))
        {
            return;
        }

        IsPressed = true;
        if (plate != null)
        {
            plate.localPosition += Vector3.down * 0.08f;
        }

        if (!string.IsNullOrEmpty(message))
        {
            GameEvents.RaiseMessageRequested(message);
        }

        GameEvents.RaiseWorldEventRequested(eventId);
    }
}
