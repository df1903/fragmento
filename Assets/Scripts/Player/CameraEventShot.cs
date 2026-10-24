using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Cámara breve de evento: cuando un evento de mundo lo pide (GameEvents.EventShotRequested), una segunda
///   cámara virtual se coloca por encima y detrás del objeto del evento, lo mira unos segundos y luego
///   devuelve la vista a Kael. Mientras tanto el jugador no puede moverse.
///
/// POR QUÉ EXISTE
///   Regla del juego: cada evento tiene feedback visible. Si el puente aparece fuera de pantalla, el jugador
///   no entiende qué cambió. Esta cámara se lo enseña sin cinemáticas largas.
///
/// CÓMO SE USA
///   Va en el objeto "Camara Evento" (una CinemachineCamera de prioridad 0) que crea Fragmento > Construir Escena.
///   Un evento la activa asignando "Camera Focus" en su WorldEventBase. El bloqueo de movimiento lo hace este
///   script (CinematicStarted/Ended), por eso esos eventos no necesitan "Lock Player During Event".
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CameraEventShot : MonoBehaviour
{
    [Tooltip("Distancia horizontal entre la cámara y el objeto del evento (metros).")]
    [SerializeField] private float distance = 16f;

    [Tooltip("Altura de la cámara sobre el objeto del evento (metros).")]
    [SerializeField] private float height = 7f;

    [Tooltip("Prioridad mientras está activa (la cámara de Kael tiene 0).")]
    [SerializeField] private int activePriority = 20;

    [Tooltip("Segundos extra para que la cámara vuelva a Kael antes de devolverle el control.")]
    [SerializeField] private float returnSeconds = 1f;

    private CinemachineCamera shotCamera;
    private Coroutine routine;

    private void Awake()
    {
        shotCamera = GetComponent<CinemachineCamera>();
        shotCamera.Priority = 0;
    }

    private void OnEnable()
    {
        GameEvents.EventShotRequested += OnShotRequested;
    }

    private void OnDisable()
    {
        GameEvents.EventShotRequested -= OnShotRequested;
    }

    private void OnShotRequested(Transform focus, float seconds)
    {
        if (focus == null)
        {
            return;
        }

        // Un tiro nuevo reemplaza al anterior (sin dejar el input bloqueado dos veces).
        if (routine != null)
        {
            StopCoroutine(routine);
            GameEvents.RaiseCinematicEnded();
        }
        routine = StartCoroutine(Shoot(focus, seconds));
    }

    private IEnumerator Shoot(Transform focus, float seconds)
    {
        GameEvents.RaiseCinematicStarted();

        // La cámara se pone del lado del jugador, para que el objeto del evento quede entre ambos y a la vista.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector3 away = player != null ? player.transform.position - focus.position : Vector3.back;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
        {
            away = Vector3.back;
        }

        Vector3 position = focus.position + away.normalized * distance + Vector3.up * height;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus.position - position, Vector3.up));

        shotCamera.Priority = activePriority;
        yield return new WaitForSeconds(seconds);

        shotCamera.Priority = 0;
        yield return new WaitForSeconds(returnSeconds);

        GameEvents.RaiseCinematicEnded();
        routine = null;
    }
}
