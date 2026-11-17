using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// QUÉ HACE
///   Mueve a Kael: caminar relativo a la cámara, saltar con "coyote time" y "jump buffer", y gravedad.
///
/// POR QUÉ EXISTE
///   Es el sistema de física/input más importante del juego. Usa CharacterController (movimiento por código,
///   sin Rigidbody) porque es predecible y fácil de explicar.
///   - Coyote time: unos milisegundos después de salir del borde todavía se puede saltar (el juego perdona).
///   - Jump buffer: si se pulsa saltar un instante ANTES de tocar el suelo, el salto se hace al aterrizar.
///
/// CÓMO SE USA
///   Está en el prefab Assets/Prefabs/Kael.prefab junto a un CharacterController.
///   Controles: WASD / flechas / stick izquierdo para moverse, Espacio / botón sur del mando para saltar.
///   Las acciones de input se crean aquí en código (no hay archivo .inputactions que mantener).
///   Durante un evento con GameEvents.CinematicStarted el input se ignora hasta CinematicEnded.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad al caminar, en metros por segundo.")]
    [SerializeField] private float moveSpeed = 7f;

    [Tooltip("Aceleración en el suelo (m/s²): qué tan rápido alcanza la velocidad máxima.")]
    [SerializeField] private float groundAcceleration = 50f;

    [Tooltip("Aceleración en el aire (m/s²): menor que en el suelo, pero suficiente para corregir el salto.")]
    [SerializeField] private float airAcceleration = 25f;

    [Tooltip("Si está marcado, Kael puede correr manteniendo Shift (niveles 6 en adelante).")]
    [SerializeField] private bool canSprint;

    [Tooltip("Cuánto más rápido va al correr (1.5 = un 50 % más).")]
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Tooltip("Objeto visual de Kael. Gira hacia donde camina (la raíz no gira, para no mover la cámara).")]
    [SerializeField] private Transform visual;

    [Tooltip("Qué tan rápido gira el modelo hacia la dirección de movimiento, en grados por segundo.")]
    [SerializeField] private float turnSpeed = 720f;

    [Header("Salto y gravedad")]
    [Tooltip("Altura del salto en metros.")]
    [SerializeField] private float jumpHeight = 1.8f;

    [Tooltip("Gravedad (negativa). Más alta que la real para que el salto se sienta ágil.")]
    [SerializeField] private float gravity = -25f;

    [Tooltip("Al caer, la gravedad se multiplica por esto: la caída es más rápida que la subida y se siente con peso.")]
    [SerializeField] private float fallGravityMultiplier = 1.4f;

    [Tooltip("Si se suelta el salto mientras se sube, la velocidad vertical se multiplica por esto (salto corto).")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Tooltip("Velocidad máxima de caída.")]
    [SerializeField] private float maxFallSpeed = -40f;

    [Tooltip("Segundos después de dejar el suelo en los que aún se puede saltar.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Segundos que se recuerda una pulsación de salto hecha antes de aterrizar.")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Detección de suelo")]
    [Tooltip("Distancia bajo los pies en la que se considera que Kael todavía pisa el suelo (metros).")]
    [SerializeField] private float groundCheckDistance = 0.1f;

    private CharacterController controller;
    private Transform cameraTransform;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private float verticalVelocity;
    private Vector3 horizontalVelocity;
    private float timeSinceGrounded = 999f;
    private float timeSinceJumpPressed = 999f;
    private bool inputLocked;
    private bool isGrounded;
    private bool jumpCutAllowed;
    private Collider groundCollider;

    // Búfer reutilizable para no crear basura (garbage) en cada fotograma.
    private readonly Collider[] groundHits = new Collider[8];

    /// <summary>Input de movimiento del último Update (x = derecha, y = adelante). Lo usa la cámara para alinearse.</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>Multiplicador de velocidad al caminar (lo sube <see cref="PlayerPowerUp"/> con cada muerte).</summary>
    public float SpeedMultiplier { get; set; } = 1f;

    /// <summary>Multiplicador de altura de salto (lo sube <see cref="PlayerPowerUp"/> con cada muerte).</summary>
    public float JumpMultiplier { get; set; } = 1f;

    /// <summary>True mientras Kael corre (Shift pulsado y moviéndose). Los lobos lo oyen y la hierba alta deja de esconderlo.</summary>
    public bool IsSprinting { get; private set; }

    /// <summary>Dirección en el mundo hacia la que el jugador quiere moverse (longitud 0 a 1). La usan las zonas de trepar.</summary>
    public Vector3 MoveDirection { get; private set; }

    /// <summary>Velocidad horizontal actual en m/s (la usa la animación de Kael).</summary>
    public float HorizontalSpeed => horizontalVelocity.magnitude;

    /// <summary>Velocidad vertical actual en m/s (positiva = subiendo).</summary>
    public float VerticalVelocity => verticalVelocity;

    /// <summary>
    /// True mientras algo lleva a Kael (la tirolina): se ignoran el input, la gravedad y su velocidad propia;
    /// solo cuenta ExternalVelocity. Al soltarlo, Kael cae con normalidad.
    /// </summary>
    public bool IsCarried { get; set; }

    /// <summary>Multiplicador de gravedad (1 = normal). Lo baja <see cref="LowGravityZone"/>: salto más alto y lento.</summary>
    public float GravityScale { get; set; } = 1f;

    /// <summary>
    /// Empuje externo en m/s (corrientes de agua, viento). Quien empuja lo suma en cada fotograma
    /// (ver <see cref="PushZone"/>); aquí se usa una vez y se pone a cero.
    /// </summary>
    public Vector3 ExternalVelocity { get; set; }

    /// <summary>True si Kael pisa el suelo (calculado en el último Update).</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>True mientras un evento o cámara de evento le quita el control (para pruebas y depuración).</summary>
    public bool IsInputLocked => inputLocked;

    /// <summary>Collider que Kael pisa ahora (null en el aire). Lo usa <see cref="MovingPlatformCarrier"/>.</summary>
    public Collider GroundCollider => groundCollider;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Por defecto el CharacterController ignora movimientos menores a 1 mm. Con muchos FPS (o una
        // plataforma lenta) el desplazamiento de un fotograma es menor, se perdería y Kael resbalaría.
        controller.minMoveDistance = 0f;

        CreateInputActions();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        GameEvents.CinematicStarted += LockInput;
        GameEvents.CinematicEnded += UnlockInput;
        GameEvents.PlayerRespawned += TeleportTo;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
        GameEvents.CinematicStarted -= LockInput;
        GameEvents.CinematicEnded -= UnlockInput;
        GameEvents.PlayerRespawned -= TeleportTo;
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
        jumpAction.Dispose();
        sprintAction.Dispose();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        Vector2 input = Vector2.zero;
        bool jumpPressed = false;
        bool jumpReleased = false;
        if (!inputLocked)
        {
            input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            jumpPressed = jumpAction.WasPressedThisFrame();
            jumpReleased = jumpAction.WasReleasedThisFrame();
        }

        MoveInput = input;
        IsSprinting = canSprint && !inputLocked && sprintAction.IsPressed() && input.sqrMagnitude > 0.04f;

        // --- Suelo, coyote time y jump buffer ---
        isGrounded = CheckGrounded();
        if (isGrounded)
        {
            timeSinceGrounded = 0f;
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f; // pegado al suelo para que isGrounded sea estable
            }
        }
        else
        {
            timeSinceGrounded += dt;
        }

        timeSinceJumpPressed = jumpPressed ? 0f : timeSinceJumpPressed + dt;

        if (timeSinceGrounded <= coyoteTime && timeSinceJumpPressed <= jumpBufferTime)
        {
            verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight * JumpMultiplier); // v = raíz(2·g·h)
            timeSinceGrounded = 999f;      // el coyote time se gasta: no hay doble salto
            timeSinceJumpPressed = 999f;   // el buffer se gasta
            jumpCutAllowed = true;
        }

        // Salto variable: soltar el botón en la subida lo acorta (toque = salto bajo, mantener = salto completo).
        // No se aplica a los rebotes (Launch): soltar el botón no debe frenar un hongo de rebote.
        if (jumpReleased && verticalVelocity > 0f && jumpCutAllowed)
        {
            verticalVelocity *= jumpCutMultiplier;
        }

        // Llevado por la tirolina: solo se mueve con el empuje externo.
        if (IsCarried)
        {
            verticalVelocity = 0f;
            horizontalVelocity = Vector3.zero;
            IsSprinting = false;
            controller.Move(ExternalVelocity * dt);
            ExternalVelocity = Vector3.zero;
            return;
        }

        float gravityScale = verticalVelocity < 0f ? fallGravityMultiplier : 1f;
        verticalVelocity = Mathf.Max(verticalVelocity + gravity * gravityScale * GravityScale * dt, maxFallSpeed);

        // --- Movimiento horizontal relativo a la cámara, con aceleración suave ---
        Vector3 moveDirection = CameraRelativeDirection(input);
        MoveDirection = moveDirection;
        float acceleration = isGrounded ? groundAcceleration : airAcceleration;
        float sprint = IsSprinting ? sprintMultiplier : 1f;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, moveDirection * moveSpeed * SpeedMultiplier * sprint, acceleration * dt);
        Vector3 velocity = horizontalVelocity + ExternalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * dt);
        ExternalVelocity = Vector3.zero; // el empuje vale un fotograma: quien empuja lo vuelve a sumar

        RotateVisual(moveDirection, dt);
    }

    /// <summary>
    /// Lanza a Kael hacia arriba hasta "height" metros (hongos de rebote). Misma fórmula que el salto:
    /// v = raíz(2·g·h), con la gravedad normal.
    /// </summary>
    public void Launch(float height)
    {
        verticalVelocity = Mathf.Sqrt(2f * -gravity * height);
        timeSinceGrounded = 999f;   // en el aire: no se puede saltar encima del rebote
        timeSinceJumpPressed = 999f;
        jumpCutAllowed = false;
    }

    /// <summary>Detiene la caída (no la subida). Lo usan las zonas de trepar para que Kael no resbale hacia abajo.</summary>
    public void CancelFall()
    {
        if (verticalVelocity < 0f)
        {
            verticalVelocity = 0f;
        }
    }

    /// <summary>
    /// Coloca a Kael en una posición (respawn). Se apaga el CharacterController un instante porque
    /// si está activo, Unity ignora los cambios directos de transform.position.
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;

        verticalVelocity = 0f;
        horizontalVelocity = Vector3.zero;
        ExternalVelocity = Vector3.zero;
        GravityScale = 1f;
        IsCarried = false;
        timeSinceGrounded = 999f;
        timeSinceJumpPressed = 999f;
        isGrounded = false;
        groundCollider = null;
    }

    // CharacterController.isGrounded parpadea con framerates altos (el empuje hacia abajo de un fotograma
    // puede ser menor que el skin width), así que se refuerza con una esfera bajo los pies.
    // De paso se guarda qué collider se pisa, para que las plataformas móviles puedan llevar a Kael.
    private bool CheckGrounded()
    {
        groundCollider = null;
        if (verticalVelocity > 0.01f)
        {
            return false; // subiendo: todavía no ha aterrizado
        }

        // Esfera un poco más estrecha que el controlador, para no tocar paredes laterales.
        float radius = controller.radius - 0.05f;
        Vector3 center = transform.position + Vector3.up * (radius - groundCheckDistance);
        int count = Physics.OverlapSphereNonAlloc(center, radius, groundHits, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider hit = groundHits[i];
            if (hit == controller) // ignorar el propio collider de Kael
            {
                continue;
            }

            // Si se pisan varios colliders, se prefiere el que tiene Rigidbody (plataforma móvil).
            if (groundCollider == null || hit.attachedRigidbody != null)
            {
                groundCollider = hit;
            }
        }

        return controller.isGrounded || groundCollider != null;
    }

    // Convierte el input (x = derecha, y = adelante) en una dirección del mundo según hacia dónde mira la cámara.
    private Vector3 CameraRelativeDirection(Vector2 input)
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }

        return forward * input.y + right * input.x;
    }

    private void RotateVisual(Vector3 moveDirection, float dt)
    {
        if (visual == null || moveDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(moveDirection, Vector3.up);
        visual.rotation = Quaternion.RotateTowards(visual.rotation, target, turnSpeed * dt);
    }

    private void LockInput() => inputLocked = true;
    private void UnlockInput() => inputLocked = false;

    private void CreateInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/rightTrigger");
    }
}
