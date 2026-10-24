using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// QUÉ HACE
///   Lee el ratón / stick derecho y gira un objeto "pivote" (CameraTarget) hacia donde mira el jugador.
///   No mueve ninguna cámara directamente.
///
/// POR QUÉ EXISTE
///   La cámara real es una CinemachineCamera (Third Person Follow) que sigue a este pivote y copia su rotación.
///   Separar "qué quiere mirar el jugador" (este script) de "cómo se coloca la cámara" (Cinemachine) mantiene
///   cada parte simple, y permite cambiar a cámaras de evento sin tocar el control.
///
/// CÓMO SE USA
///   Va en el objeto CameraTarget, hijo de Kael (el prefab Kael.prefab ya lo trae).
///   La CinemachineCamera de la escena tiene a CameraTarget como "Follow".
///   Clic en la ventana de juego captura el cursor; Esc lo libera.
///   También avisa a Cinemachine cuando Kael se teletransporta (respawn) para que la cámara salte con él
///   en vez de cruzar el mapa "volando".
/// </summary>
[DefaultExecutionOrder(100)] // después de PlayerController, para ver su teletransporte en el mismo fotograma
public class ThirdPersonCamera : MonoBehaviour
{
    [Tooltip("Si el pivote se mueve más que esto (metros) en un fotograma, se considera un teletransporte.")]
    [SerializeField] private float warpDistance = 6f;

    [Tooltip("Grados que gira la cámara por cada píxel que se mueve el ratón.")]
    [SerializeField] private float mouseSensitivity = 0.12f;

    [Tooltip("Grados por segundo con el stick derecho a tope.")]
    [SerializeField] private float stickSensitivity = 160f;

    [Tooltip("Límite al mirar hacia arriba (grados, negativo = mirar arriba).")]
    [SerializeField] private float minPitch = -25f;

    [Tooltip("Límite al mirar hacia abajo (grados).")]
    [SerializeField] private float maxPitch = 65f;

    [Tooltip("Inclinación inicial de la cámara (grados hacia abajo).")]
    [SerializeField] private float startPitch = 20f;

    [Tooltip("Capturar y ocultar el cursor al jugar.")]
    [SerializeField] private bool lockCursor = true;

    private InputAction mouseLookAction;
    private InputAction stickLookAction;
    private float yaw;
    private float pitch;
    private bool inputLocked;
    private Vector3 lastPosition;

    private void Awake()
    {
        mouseLookAction = new InputAction("MouseLook", InputActionType.Value, "<Mouse>/delta");
        stickLookAction = new InputAction("StickLook", InputActionType.Value, "<Gamepad>/rightStick");
    }

    private void OnEnable()
    {
        mouseLookAction.Enable();
        stickLookAction.Enable();
        GameEvents.CinematicStarted += LockInput;
        GameEvents.CinematicEnded += UnlockInput;
        SetCursorLocked(lockCursor);
    }

    private void OnDisable()
    {
        mouseLookAction.Disable();
        stickLookAction.Disable();
        GameEvents.CinematicStarted -= LockInput;
        GameEvents.CinematicEnded -= UnlockInput;
        SetCursorLocked(false);
    }

    private void OnDestroy()
    {
        mouseLookAction.Dispose();
        stickLookAction.Dispose();
    }

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = startPitch;
        lastPosition = transform.position;
        ApplyRotation();
    }

    private void Update()
    {
        Vector3 moved = transform.position - lastPosition;
        if (moved.sqrMagnitude > warpDistance * warpDistance)
        {
            CinemachineCore.OnTargetObjectWarped(transform, moved);
        }
        lastPosition = transform.position;

        // Durante un evento o la pantalla final no se toca ni el giro ni el cursor.
        if (inputLocked)
        {
            return;
        }

        // Si el cursor fue liberado (Esc), un clic en la ventana lo vuelve a capturar.
        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        if (lockCursor && !cursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
            return;
        }

        // El delta del ratón ya viene "por fotograma", el stick no: por eso solo este se multiplica por el tiempo.
        Vector2 look = Vector2.zero;
        if (cursorLocked || !lockCursor)
        {
            look += mouseLookAction.ReadValue<Vector2>() * mouseSensitivity;
        }
        look += stickLookAction.ReadValue<Vector2>() * (stickSensitivity * Time.deltaTime);

        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void SetCursorLocked(bool locked)
    {
        if (!lockCursor && locked)
        {
            return;
        }

        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void LockInput() => inputLocked = true;
    private void UnlockInput() => inputLocked = false;
}
