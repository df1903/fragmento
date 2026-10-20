using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Guarda los datos de un fragmento (número, nombre, mensaje, evento que dispara, color)
///   en un archivo del proyecto (ScriptableObject), no dentro del código.
///
/// POR QUÉ EXISTE
///   Para separar datos de lógica: cambiar el nombre o el evento de un fragmento no obliga a tocar scripts,
///   y el Inspector permite ver y editar los 7 fragmentos sin programar.
///
/// CÓMO SE USA
///   Los archivos viven en Assets/Data/ (los crea Fragmento > Construir Escena).
///   Para crear uno a mano: clic derecho en Project > Create > Fragmento > Datos de Fragmento.
///   Cada <see cref="Fragment"/> en la escena apunta a uno de estos archivos.
/// </summary>
[CreateAssetMenu(fileName = "Fragmento_", menuName = "Fragmento/Datos de Fragmento")]
public class FragmentData : ScriptableObject
{
    [Tooltip("Número del fragmento (1 a 7). Debe ser único: el contador lo usa para no contar dos veces el mismo.")]
    [SerializeField] private int id = 1;

    [Tooltip("Nombre que se muestra al jugador.")]
    [SerializeField] private string displayName = "Fragmento";

    [Tooltip("Texto corto que aparece en pantalla al absorberlo.")]
    [SerializeField, TextArea] private string message = "";

    [Tooltip("Id del evento de mundo que se dispara. Debe coincidir con el Event Id de un WorldEventBase de la escena.")]
    [SerializeField] private string eventId = "";

    [Tooltip("Color de la luz del fragmento.")]
    [SerializeField] private Color color = Color.white;

    public int Id => id;
    public string DisplayName => displayName;
    public string Message => message;
    public string EventId => eventId;
    public Color Color => color;
}
