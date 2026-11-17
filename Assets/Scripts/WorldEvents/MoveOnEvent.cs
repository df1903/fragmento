using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Evento de mundo genérico: mueve y/o gira un objeto de donde está a otra posición, con suavizado.
///   Sirve para un río que baja de nivel, un puente levadizo que cae, un portón que se abre o una campana.
///
/// POR QUÉ EXISTE
///   Muchos eventos de los niveles 6 y 7 son "esto se mueve hasta allí". Con un solo script configurable no hace
///   falta una clase por cada uno.
///
/// CÓMO SE USA
///   "Target" es lo que se mueve. "Offset" el desplazamiento (en el mundo) y "Rotation" el giro en grados
///   (local) que se le suma. "Event Id" debe coincidir con el FragmentData o la placa que lo dispara.
/// </summary>
public class MoveOnEvent : WorldEventBase
{
    [SerializeField] private Transform target;

    [Tooltip("Desplazamiento en el mundo.")]
    [SerializeField] private Vector3 offset;

    [Tooltip("Giro en grados que se suma a la rotación local.")]
    [SerializeField] private Vector3 rotation;

    [Tooltip("Segundos que dura el movimiento.")]
    [SerializeField] private float seconds = 2f;

    /// <summary>Configura el evento desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform targetTransform, Vector3 moveOffset, Vector3 rotationEuler, float duration)
    {
        target = targetTransform;
        offset = moveOffset;
        rotation = rotationEuler;
        seconds = duration;
    }

    protected override IEnumerator Run()
    {
        Vector3 startPosition = target.position;
        Quaternion startRotation = target.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(rotation);

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            target.position = startPosition + offset * t;
            target.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        target.position = startPosition + offset;
        target.localRotation = endRotation;
    }
}
