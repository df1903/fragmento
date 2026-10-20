using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Una plataforma "apagada" que, al recoger su fragmento, se enciende y empieza a ir y venir entre su
///   posición inicial y otra a "Travel" metros, parando unos segundos en cada extremo.
///
/// POR QUÉ EXISTE
///   Es el evento del Fragmento 3 ("transfiere energía a una plataforma extinta"). Cruza una brecha que
///   Kael no puede saltar.
///
/// CÓMO SE USA
///   Va en la propia plataforma, que debe tener collider, Rigidbody (se fuerza a cinemático con interpolación)
///   y un Renderer. Kael viaja encima gracias a <see cref="MovingPlatformCarrier"/>.
///   Se mueve en FixedUpdate con Rigidbody.MovePosition: así la física mueve a los que están encima de forma
///   estable; la interpolación del Rigidbody la dibuja suave.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatformActivate : WorldEventBase
{
    [Tooltip("Desplazamiento (en el mundo) entre el punto de salida y el punto de llegada.")]
    [SerializeField] private Vector3 travel = new Vector3(0f, 0f, 16f);

    [Tooltip("Velocidad media en metros por segundo.")]
    [SerializeField] private float speed = 3.5f;

    [Tooltip("Segundos que espera en cada extremo.")]
    [SerializeField] private float pauseSeconds = 2f;

    [Tooltip("Renderer cuyo material cambia al activarse.")]
    [SerializeField] private Renderer platformRenderer;

    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private Material activeMaterial;

    private Rigidbody body;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool moving;
    private float progress;     // 0 = salida, 1 = llegada
    private float direction = 1f;
    private float pauseTimer;

    public bool IsMoving => moving;

    protected override void Prepare()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        startPosition = transform.position;
        endPosition = startPosition + travel;

        if (platformRenderer != null && inactiveMaterial != null)
        {
            platformRenderer.sharedMaterial = inactiveMaterial;
        }
    }

    protected override IEnumerator Run()
    {
        if (platformRenderer != null && activeMaterial != null)
        {
            platformRenderer.sharedMaterial = activeMaterial;
        }

        // Pequeña pausa para que se vea que "despierta" antes de moverse.
        yield return new WaitForSeconds(0.5f);
        moving = true;
    }

    private void FixedUpdate()
    {
        if (!moving)
        {
            return;
        }

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            return;
        }

        float distance = Mathf.Max(travel.magnitude, 0.01f);
        progress += direction * speed * Time.fixedDeltaTime / distance;
        if (progress >= 1f)
        {
            progress = 1f;
            direction = -1f;
            pauseTimer = pauseSeconds;
        }
        else if (progress <= 0f)
        {
            progress = 0f;
            direction = 1f;
            pauseTimer = pauseSeconds;
        }

        // SmoothStep suaviza la salida y la llegada para que no frene en seco.
        body.MovePosition(Vector3.Lerp(startPosition, endPosition, Mathf.SmoothStep(0f, 1f, progress)));
    }
}
