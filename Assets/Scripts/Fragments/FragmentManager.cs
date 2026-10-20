using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Lleva la cuenta de los fragmentos recogidos y, por cada fragmento NUEVO, pide una sola vez
///   el evento de mundo que le corresponde.
///
/// POR QUÉ EXISTE
///   Es el único sitio que decide "esto cuenta". Así un fragmento no puede disparar su evento dos veces
///   aunque el trigger se active dos veces (regla del juego: exactamente un evento por fragmento).
///
/// CÓMO SE USA
///   Poner UN componente FragmentManager en la escena (lo hace Fragmento > Construir Escena).
///   Escucha GameEvents.FragmentCollected y lanza FragmentCountChanged y WorldEventRequested.
///   "Total Fragments" es la cantidad total del nivel (7).
/// </summary>
public class FragmentManager : MonoBehaviour
{
    [Tooltip("Cantidad total de fragmentos del nivel.")]
    [SerializeField] private int totalFragments = 7;

    // Un HashSet no admite repetidos: Add() devuelve false si el id ya estaba.
    private readonly HashSet<int> collectedIds = new HashSet<int>();

    public int CollectedCount => collectedIds.Count;
    public int TotalFragments => totalFragments;

    private void OnEnable()
    {
        GameEvents.FragmentCollected += OnFragmentCollected;
    }

    private void OnDisable()
    {
        GameEvents.FragmentCollected -= OnFragmentCollected;
    }

    // Start corre después de todos los OnEnable: el HUD ya está suscrito y puede mostrar "0 / 7".
    private void Start()
    {
        GameEvents.RaiseFragmentCountChanged(collectedIds.Count, totalFragments);
    }

    private void OnFragmentCollected(FragmentData data)
    {
        if (data == null)
        {
            Debug.LogWarning("FragmentManager: llegó un fragmento sin datos (FragmentData).", this);
            return;
        }

        // Idempotencia: si este id ya se contó, no se hace nada más.
        if (!collectedIds.Add(data.Id))
        {
            return;
        }

        GameEvents.RaiseFragmentCountChanged(collectedIds.Count, totalFragments);
        GameEvents.RaiseMessageRequested(data.Message);
        GameEvents.RaiseWorldEventRequested(data.EventId);
    }
}
