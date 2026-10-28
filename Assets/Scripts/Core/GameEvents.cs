using System;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Es el "tablón de anuncios" del juego: una lista de eventos C# a la que cualquier script
///   puede avisar ("recogí un fragmento") o suscribirse ("avísame cuando se recoja un fragmento").
///
/// POR QUÉ EXISTE
///   Para que los sistemas no se conozcan entre sí. El fragmento no sabe que existe un puente;
///   solo avisa de que fue recogido. Así cada script se puede explicar, probar y cambiar por separado
///   (patrón Observer).
///
/// CÓMO SE USA
///   Para avisar:      GameEvents.RaiseFragmentCollected(datos);
///   Para escuchar:    en OnEnable  -> GameEvents.FragmentCollected += MiMetodo;
///                     en OnDisable -> GameEvents.FragmentCollected -= MiMetodo;   (¡siempre darse de baja!)
///   Solo esta clase puede lanzar los eventos; por eso existen los métodos Raise...
/// </summary>
public static class GameEvents
{
    /// <summary>Kael absorbió un fragmento (lo lanza <see cref="Fragment"/>).</summary>
    public static event Action<FragmentData> FragmentCollected;

    /// <summary>Cambió el contador: (recogidos, total). Lo lanza <see cref="FragmentManager"/>.</summary>
    public static event Action<int, int> FragmentCountChanged;

    /// <summary>Hay que ejecutar el evento de mundo con ese id (lo lanza <see cref="FragmentManager"/>).</summary>
    public static event Action<string> WorldEventRequested;

    /// <summary>Terminó la animación del evento de mundo con ese id.</summary>
    public static event Action<string> WorldEventFinished;

    /// <summary>Empieza un momento en que el jugador no debe moverse (p. ej. cámara de evento).</summary>
    public static event Action CinematicStarted;

    /// <summary>Termina ese momento y el jugador recupera el control.</summary>
    public static event Action CinematicEnded;

    /// <summary>Hay un texto corto para mostrar al jugador en el HUD.</summary>
    public static event Action<string> MessageRequested;

    /// <summary>Kael cayó al vacío (lo lanza <see cref="KillZone"/>).</summary>
    public static event Action PlayerFell;

    /// <summary>Cambiaron las vidas: (actuales, máximas). Lo lanza <see cref="LivesManager"/>.</summary>
    public static event Action<int, int> LivesChanged;

    /// <summary>Kael murió por cualquier causa (caída o enemigo). <see cref="RespawnManager"/> lo devuelve al checkpoint.</summary>
    public static event Action PlayerDied;

    /// <summary>Kael pisó una isla nueva: este es su punto de reaparición (lo lanza <see cref="CheckpointZone"/>).</summary>
    public static event Action<Vector3> CheckpointReached;

    /// <summary>Hay que devolver a Kael a esta posición (lo lanza <see cref="RespawnManager"/>).</summary>
    public static event Action<Vector3> PlayerRespawned;

    /// <summary>El Núcleo quedó restaurado en el pedestal (lo lanza <see cref="CoreRestoration"/>).</summary>
    public static event Action CoreRestored;

    /// <summary>Kael cruzó el Portal de Salida (lo lanza <see cref="ExitPortal"/>).</summary>
    public static event Action LevelCompleted;

    /// <summary>Un evento de mundo pide una cámara breve que mire a este objeto durante N segundos.</summary>
    public static event Action<Transform, float> EventShotRequested;

    public static void RaiseCoreRestored() => CoreRestored?.Invoke();
    public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
    public static void RaiseEventShotRequested(Transform focus, float seconds) => EventShotRequested?.Invoke(focus, seconds);
    public static void RaiseFragmentCollected(FragmentData data) => FragmentCollected?.Invoke(data);
    public static void RaiseFragmentCountChanged(int collected, int total) => FragmentCountChanged?.Invoke(collected, total);
    public static void RaiseWorldEventRequested(string eventId) => WorldEventRequested?.Invoke(eventId);
    public static void RaiseWorldEventFinished(string eventId) => WorldEventFinished?.Invoke(eventId);
    public static void RaiseCinematicStarted() => CinematicStarted?.Invoke();
    public static void RaiseCinematicEnded() => CinematicEnded?.Invoke();
    public static void RaiseMessageRequested(string message) => MessageRequested?.Invoke(message);
    public static void RaisePlayerDied() => PlayerDied?.Invoke();
    public static void RaiseLivesChanged(int current, int max) => LivesChanged?.Invoke(current, max);

    // Caer al vacío también es una muerte: se avisa de las dos cosas.
    public static void RaisePlayerFell()
    {
        PlayerFell?.Invoke();
        PlayerDied?.Invoke();
    }
    public static void RaiseCheckpointReached(Vector3 position) => CheckpointReached?.Invoke(position);
    public static void RaisePlayerRespawned(Vector3 position) => PlayerRespawned?.Invoke(position);

    /// <summary>
    /// Los eventos estáticos sobreviven entre sesiones de Play si el Editor no recarga el dominio
    /// (Enter Play Mode Options). Este método los vacía cada vez que arranca el juego.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        FragmentCollected = null;
        FragmentCountChanged = null;
        WorldEventRequested = null;
        WorldEventFinished = null;
        CinematicStarted = null;
        CinematicEnded = null;
        MessageRequested = null;
        PlayerFell = null;
        PlayerDied = null;
        LivesChanged = null;
        CheckpointReached = null;
        PlayerRespawned = null;
        CoreRestored = null;
        LevelCompleted = null;
        EventShotRequested = null;
    }
}
