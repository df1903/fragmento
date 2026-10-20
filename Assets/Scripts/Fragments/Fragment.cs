using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Es el coleccionable. Flota y gira esperando a Kael; al tocarlo se encoge hacia él
///   y avisa a todo el juego con GameEvents.FragmentCollected.
///
/// POR QUÉ EXISTE
///   Separa "recoger" (este script) de "qué pasa después" (FragmentManager y los eventos de mundo).
///   El fragmento no sabe que existe un puente: solo avisa de que fue recogido.
///
/// CÓMO SE USA
///   Prefab Assets/Prefabs/Fragmento.prefab. En la escena se asigna su "Data" (FragmentData).
///   Necesita un collider marcado como Trigger (el prefab ya lo trae).
///   Solo reacciona a objetos con <see cref="PlayerController"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Fragment : MonoBehaviour
{
    [Tooltip("Datos de este fragmento (id, nombre, evento que dispara).")]
    [SerializeField] private FragmentData data;

    [Tooltip("Parte visual que gira y flota. Si está vacío, se anima este mismo objeto.")]
    [SerializeField] private Transform visual;

    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 2f;

    [Tooltip("Segundos que tarda en encogerse hacia Kael al recogerlo.")]
    [SerializeField] private float absorbDuration = 0.5f;

    private bool collected;
    private Vector3 visualStartPosition;

    public FragmentData Data => data;
    public bool IsCollected => collected;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (visual == null)
        {
            visual = transform;
        }
        visualStartPosition = visual.localPosition;

        if (data == null)
        {
            Debug.LogError("Fragment: falta asignar el FragmentData.", this);
        }
    }

    private void Update()
    {
        if (collected)
        {
            return;
        }

        visual.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        visual.localPosition = visualStartPosition + new Vector3(0f, offset, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        collected = true;
        GetComponent<Collider>().enabled = false;
        GameEvents.RaiseFragmentCollected(data);
        StartCoroutine(AbsorbAnimation(player.transform));
    }

    // Animación barata: el fragmento se encoge mientras viaja hacia Kael y luego desaparece.
    private IEnumerator AbsorbAnimation(Transform target)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < absorbDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / absorbDuration);
            Vector3 targetPosition = target.position + Vector3.up * 0.5f;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t * t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
