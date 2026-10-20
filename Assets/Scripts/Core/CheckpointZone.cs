using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Zona sobre una isla. Cuando Kael entra avisa con GameEvents.CheckpointReached y le dice
///   cuál es el punto seguro de esa isla.
///
/// POR QUÉ EXISTE
///   Si Kael cae, debe reaparecer en la última isla que pisó, no al principio del nivel.
///   El punto de reaparición es un objeto aparte para poder colocarlo en un sitio seguro y lejos del borde.
///
/// CÓMO SE USA
///   Un objeto con un collider (se fuerza a Trigger) que cubra la isla, y "Respawn Point" apuntando a un objeto
///   vacío sobre el suelo. Lo coloca Fragmento > Construir Escena.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointZone : MonoBehaviour
{
    [Tooltip("Dónde reaparece Kael si cae después de haber pisado esta zona. Si está vacío, se usa el centro de la zona.")]
    [SerializeField] private Transform respawnPoint;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position;
        GameEvents.RaiseCheckpointReached(position);
    }
}
