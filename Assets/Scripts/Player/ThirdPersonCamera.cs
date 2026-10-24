using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// QUÉ HACE
///   Gira un objeto "pivote" (CameraTarget) hacia donde mira la cámara. Tres formas de girarla:
///   1) Automática: al avanzar y girar hacia un lado (W + A / W + D) la cámara acompaña a Kael.
///   2) Manual con ratón: mantener el botón derecho y mover el ratón.
///   3) Manual con teclado / mando: Q / E o stick derecho.
///   Sin botón derecho el ratón NO mueve la cámara. No mueve ninguna cámara directamente.
///
/// POR QUÉ EXISTE
///   La cámara real es una CinemachineCamera (Third Person Follow) que sigue a este pivote y copia su rotación.
///   Separar "qué quiere mirar el jugador" (este script) de "cómo se coloca la cámara" (Cinemachine) mantiene
///   cada parte simple, y permite cambiar a cámaras de evento sin tocar el control.
///
/// CÓMO SE USA
///   Va en el objeto CameraTarget, hijo de Kael (el prefab Kael.prefab ya lo trae).
///   La CinemachineCamera de la escena tiene a CameraTarget como "Follow".
///   También avisa a Cinemachine cuando Kael se teletransporta (respawn) para que la cámara salte con él
///   en vez de cruzar el mapa "volando".
/// </summary>
[DefaultExecutionOrder(100)] // después de PlayerController, para ver su teletransporte en el mismo fotograma
public class ThirdPersonCamera : MonoBehaviour
{
    [Tooltip("Si el pivote se mueve más que esto (metros) en un fotograma, se considera un teletransporte.")]
    [SerializeField] private float warpDistance = 6f;

    [Tooltip("Grados por segundo al girar con Q / E o con el stick derecho a tope.")]
    [SerializeField] private float turnSpeed = 140f;

    [Tooltip("Grados por píxel que se mueve el ratón mientras se mantiene el botón derecho.")]
    [SerializeField] private float dragSensitivity = 0.15f;

    [Tooltip("Grados por segundo con los que la cámara acompaña a Kael al avanzar y girar a un lado.")]
    [SerializeField] private float autoTurnSpeed = 80f;

    [Tooltip("Segundos sin giro automático después de mover la cámara a mano.")]
    [SerializeField] private float autoPause = 1.2f;

    [Tooltip("Inclinación mínima y máxima de la cámara al arrastrar con el ratón (grados hacia abajo).")]
    [SerializeField] private Vector2 pitchLimits = new Vector2(8f, 55f);

    [Tooltip("Inclinación inicial de la cámara (grados hacia abajo).")]
    [SerializeField] private float pitch = 22f;

    private InputAction turnAction;
    private InputAction dragAction;
    private InputAction lookAction;
    private PlayerController player;
    private float yaw;
    private float manualTimer;
    private bool dragging;
    private bool inputLocked;
    private Vector3 lastPosition;

    private void Awake()
    {
        turnAction = new InputAction("CameraTurn", InputActionType.Value);
        turnAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/q")
            .With("Positive", "<Keyboard>/e");
        turnAction.AddBinding("<Gamepad>/rightStick/x");

        dragAction = new InputAction("CameraDrag", InputActionType.Button, "<Mouse>/rightButton");
        lookAction = new InputAction("CameraLook", InputActionType.Value, "<Mouse>/delta");
    }

    private void OnEnable()
    {
        turnAction.Enable();
        dragAction.Enable();
        lookAction.Enable();
        GameEvents.CinematicStarted += LockInput;
        GameEvents.CinematicEnded += UnlockInput;
    }

    private void OnDisable()
    {
        turnAction.Disable();
        dragAction.Disable();
        lookAction.Disable();
        GameEvents.CinematicStarted -= LockInput;
        GameEvents.CinematicEnded -= UnlockInput;
        SetDragging(false);
    }

    private void OnDestroy()
    {
        turnAction.Dispose();
        dragAction.Dispose();
        lookAction.Dispose();
    }

    private void Start()
    {
        player = GetComponentInParent<PlayerController>();
        yaw = transform.eulerAngles.y;
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

        // Durante un evento o la pantalla final no se toca el giro.
        if (inputLocked)
        {
            SetDragging(false);
            return;
        }

        float manualYaw = turnAction.ReadValue<float>() * turnSpeed * Time.deltaTime;

        // Arrastre con botón derecho: el cursor se captura mientras dura y se libera al soltar.
        SetDragging(dragAction.IsPressed());
        if (dragging)
        {
            Vector2 look = lookAction.ReadValue<Vector2>() * dragSensitivity;
            manualYaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, pitchLimits.x, pitchLimits.y);
        }

        if (Mathf.Abs(manualYaw) > 0.001f || dragging)
        {
            manualTimer = autoPause;
        }
        else
        {
            manualTimer -= Time.deltaTime;
        }

        yaw += manualYaw;

        // Giro automático: solo al avanzar (W) y a la vez ir a un lado (A / D). Con A / D solos (strafe)
        // o retrocediendo no gira, para que Kael no dé vueltas sin querer.
        if (manualTimer <= 0f && player != null)
        {
            Vector2 input = player.MoveInput;
            if (input.y > 0.1f)
            {
                yaw += input.x * input.y * autoTurnSpeed * Time.deltaTime;
            }
        }

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void SetDragging(bool value)
    {
        if (dragging == value)
        {
            return;
        }

        dragging = value;
        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }

    private void LockInput() => inputLocked = true;
    private void UnlockInput() => inputLocked = false;
}
