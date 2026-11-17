using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Marea: el agua sube y baja por turnos. Con marea alta las piedras bajas quedan sumergidas y Kael se ahoga
///   si sus pies quedan bajo la superficie.
///
/// POR QUÉ EXISTE
///   Es la dinámica y el peligro propio del nivel 5 (Lago): el mismo camino es seguro o mortal según el momento,
///   así que hay que esperar en las rocas altas y cruzar con la marea baja.
///
/// CÓMO SE USA
///   Este script va en el objeto del agua (una lámina plana). Su altura inicial es la marea BAJA; sube
///   "Rise" metros. Tiempos ajustables. Hereda de Hazard: se queda quieta en las cámaras de evento.
/// </summary>
public class TideWater : Hazard
{
    [Tooltip("Metros que sube el agua con marea alta.")]
    [SerializeField] private float rise = 2.2f;

    [Tooltip("Segundos con marea baja (momento de cruzar).")]
    [SerializeField] private float lowSeconds = 6f;

    [Tooltip("Segundos que tarda en subir o en bajar.")]
    [SerializeField] private float moveSeconds = 3f;

    [Tooltip("Segundos con marea alta.")]
    [SerializeField] private float highSeconds = 3f;

    [Tooltip("Cuánto pueden hundirse los pies antes de ahogarse (metros).")]
    [SerializeField] private float tolerance = 0.25f;

    private float lowY;
    private float clock;

    /// <summary>Altura actual de la superficie del agua.</summary>
    public float SurfaceY => transform.position.y;

    /// <summary>True con la marea arriba del todo (para pruebas).</summary>
    public bool IsHigh { get; private set; }

    /// <summary>True con la marea abajo del todo (para pruebas).</summary>
    public bool IsLow { get; private set; }

    /// <summary>Configura la marea desde código (la usa el constructor de escena).</summary>
    public void Setup(float riseMeters)
    {
        rise = riseMeters;
    }

    private void Awake()
    {
        lowY = transform.position.y;
        IsLow = true;
    }

    protected override void Tick()
    {
        float cycle = lowSeconds + moveSeconds + highSeconds + moveSeconds;
        clock = (clock + Time.deltaTime) % cycle;

        float level; // 0 = baja, 1 = alta
        if (clock < lowSeconds)
        {
            level = 0f;
        }
        else if (clock < lowSeconds + moveSeconds)
        {
            level = (clock - lowSeconds) / moveSeconds;
        }
        else if (clock < lowSeconds + moveSeconds + highSeconds)
        {
            level = 1f;
        }
        else
        {
            level = 1f - (clock - lowSeconds - moveSeconds - highSeconds) / moveSeconds;
        }

        IsLow = level <= 0f;
        IsHigh = level >= 1f;

        Vector3 position = transform.position;
        position.y = lowY + rise * Mathf.SmoothStep(0f, 1f, level);
        transform.position = position;

        if (Player.transform.position.y < SurfaceY - tolerance)
        {
            TryKill(PlayerCenter, 1f);
        }
    }
}
