using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Zona invisible bajo el nivel. Cuando Kael la toca avisa con GameEvents.PlayerFell (cayó al vacío).
///
/// POR QUÉ EXISTE
///   Kael no tiene vida ni combate: el único fallo del juego es caer. Esta zona detecta la caída;
///   quien decide a dónde vuelve es <see cref="RespawnManager"/>.
///
/// CÓMO SE USA
///   Un objeto con un collider (se fuerza a Trigger) grande y plano bajo todas las islas.
///   Lo coloca Fragmento > Construir Escena.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            GameEvents.RaisePlayerFell();
        }
    }
}
