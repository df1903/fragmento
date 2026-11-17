using UnityEngine;

/// <summary>Desliza la textura principal del material: da sensación de agua que corre. Ver el resumen de Sway.</summary>
[RequireComponent(typeof(Renderer))]
public class UvScroll : MonoBehaviour
{
    [Tooltip("Velocidad de deslizamiento de la textura (unidades de UV por segundo).")]
    [SerializeField] private Vector2 speed = new Vector2(0f, 0.15f);

    private Material material;

    public void Setup(Vector2 scrollSpeed)
    {
        speed = scrollSpeed;
    }

    private void Awake()
    {
        material = GetComponent<Renderer>().material; // copia propia: no cambia el asset compartido
    }

    private void Update()
    {
        material.SetTextureOffset("_BaseMap", speed * Time.time);
    }
}
