using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Abre una compuerta sellada por sombras: la losa tiembla un instante y se hunde en el suelo.
///   Al terminar, su collider se apaga para que no estorbe aunque quede un borde asomando.
///
/// POR QUÉ EXISTE
///   Es el evento del Fragmento 4 ("abre compuertas selladas por sombras"). Bloquea el camino al norte del
///   mirador hasta que Kael recoge el fragmento.
///
/// CÓMO SE USA
///   Va en un objeto padre siempre activo; "Door" es la losa (con collider) que baja.
///   Estructura que crea Fragmento > Construir Escena:
///     Compuerta (este script)
///       └ Losa (cubo con collider)
/// </summary>
public class DoorOpen : WorldEventBase
{
    [Tooltip("La losa que bloquea el paso.")]
    [SerializeField] private Transform door;

    [Tooltip("Cuántos metros se hunde la losa.")]
    [SerializeField] private float sinkDistance = 4.5f;

    [Tooltip("Segundos que tarda en abrirse.")]
    [SerializeField] private float duration = 2.5f;

    [Tooltip("Cuánto tiembla al empezar (metros).")]
    [SerializeField] private float shakeAmount = 0.05f;

    [Tooltip("Segundos de temblor antes de hundirse.")]
    [SerializeField] private float shakeSeconds = 0.6f;

    public bool IsOpen { get; private set; }

    protected override IEnumerator Run()
    {
        if (door == null)
        {
            Debug.LogError("DoorOpen: falta asignar la losa (Door).", this);
            yield break;
        }

        Vector3 closed = door.position;
        Vector3 open = closed + Vector3.down * sinkDistance;

        // Temblor: avisa de que algo está despertando.
        float elapsed = 0f;
        while (elapsed < shakeSeconds)
        {
            elapsed += Time.deltaTime;
            door.position = closed + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * shakeAmount;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            door.position = Vector3.Lerp(closed, open, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }

        door.position = open;
        foreach (Collider doorCollider in door.GetComponentsInChildren<Collider>())
        {
            doorCollider.enabled = false;
        }
        IsOpen = true;
    }
}
