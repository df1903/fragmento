using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Base común de las zonas que afectan a Kael mientras está dentro (corriente, baja gravedad, rebote).
///   Cada fotograma comprueba si los pies de Kael están dentro de la caja del collider y avisa a la clase hija.
///
/// POR QUÉ EXISTE
///   Kael se teletransporta al reaparecer y los avisos de Unity (OnTriggerEnter/Exit) no siempre llegan en ese
///   caso. Preguntar "¿está dentro ahora?" en cada fotograma es más simple y nunca se queda en un estado viejo.
///
/// CÓMO SE USA
///   Crear una clase hija e implementar WhileInside (cada fotograma dentro) y, si hace falta, OnExit.
///   El objeto necesita un BoxCollider sin rotar (se fuerza a Trigger): su caja es la zona.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public abstract class PlayerZone : MonoBehaviour
{
    private BoxCollider box;
    private PlayerController player;
    private bool inside;

    /// <summary>True si Kael estaba dentro en el último fotograma (para pruebas y depuración).</summary>
    public bool IsPlayerInside => inside;

    protected virtual void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }
        }

        // Se mira un punto un poco por encima de los pies para que el suelo de la zona cuente como "dentro".
        bool nowInside = box.bounds.Contains(player.transform.position + Vector3.up * 0.2f);
        if (nowInside)
        {
            WhileInside(player);
        }
        else if (inside)
        {
            OnExit(player);
        }

        inside = nowInside;
    }

    private void OnDisable()
    {
        if (inside && player != null)
        {
            OnExit(player);
        }

        inside = false;
    }

    /// <summary>Se llama cada fotograma mientras Kael está dentro.</summary>
    protected abstract void WhileInside(PlayerController kael);

    /// <summary>Se llama una vez cuando Kael sale. Por defecto no hace nada.</summary>
    protected virtual void OnExit(PlayerController kael) { }
}
