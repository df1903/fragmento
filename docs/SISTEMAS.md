# Sistemas de Fragmento

Guía para **entender y defender** cada sistema en clase. Cada uno sigue el esquema *Qué hace / Por qué existe / Cómo se usa* y dice qué concepto del curso demuestra. Los scripts repiten este bloque en su cabecera. Se amplía en cada hito.

Estado: **H2 (vertical slice)**. Pendientes de documentar: menús, audio, eventos restantes (fragmentos 4–7), Núcleo y portal.

## Mapa general

```
Fragment ──FragmentCollected──► FragmentManager ──WorldEventRequested(eventId)──► WorldEventBase
                                       │                                            ├ BridgeAppear (F1)
                                       │                                            ├ StairsMaterialize (F2)
                                       │                                            └ MovingPlatformActivate (F3)
                                       ├──FragmentCountChanged──► HUDController
                                       └──MessageRequested──────► HUDController
KillZone ──PlayerFell──► RespawnManager ──PlayerRespawned(pos)──► PlayerController
CheckpointZone ──CheckpointReached(pos)──► RespawnManager
WorldEventBase ──CinematicStarted/Ended──► PlayerController, ThirdPersonCamera
```

### Mapa del nivel (vista desde arriba, Z+ = norte, X+ = este)

```
   G Cima (y=12.5)  x -10..14, z 167..187   ★ pedestal del Núcleo (F7) y Portal de Salida
        ▲  4 plataformas giratorias (F6), de y=6.5 a 11
   F (y=5)  x -11..15, z 114..136  ◄── puente de luz (F5), 15 m ──  E (y=5)  x 30..56, z 114..136
                                                                         ▲  pasillo + compuerta (F4)
   D Mirador (y=5)  x 30..56, z 76..100        ← plataforma móvil (F3) cruza 20 m desde C
        ▲
   C Terraza (y=5)  x 30..56, z 34..56   ◄── escalera (F2), 20 escalones desde B
   B Plaza (y=0)    x -15..15, z 30..60
        ▲  puente de luz (F1), 15 m
   A Isla inicial (y=0)  x -15..15, z -15..15   ★ Kael empieza en (0, 0, -10)
```

Fragmentos: F1 en A (-5, 1.3, 9) · F2 en B tras las ruinas (-10, 1.3, 55) · F3 en C (48, 6.3, 50) · F4 en D (43, 6.3, 93) · F5 en E (48, 6.3, 126) · F6 en F (2, 6.3, 122) · F7 en la cima (2, 13.8, 172). Zona de caída en y = -40.

Los sistemas **no se llaman entre sí**: se avisan por `GameEvents`. Si quitas el puente de la escena, el fragmento sigue funcionando.

Código en `Assets/Scripts/` con su propio ensamblado (`Fragmento.asmdef`): compila más rápido y permite que las pruebas lo usen. Los scripts de `Assets/Editor/` no entran en el juego final.

---

## GameEvents (`Core/GameEvents.cs`)
- **Qué hace:** lista de eventos C# estáticos (`FragmentCollected`, `FragmentCountChanged`, `WorldEventRequested`, `WorldEventFinished`, `CinematicStarted`, `CinematicEnded`, `MessageRequested`, `PlayerFell`, `CheckpointReached`, `PlayerRespawned`).
- **Por qué existe:** desacopla los sistemas (patrón Observer). Cada script se explica y se prueba por separado.
- **Cómo se usa:** avisar con `GameEvents.RaiseXxx(...)`; escuchar con `+=` en `OnEnable` y **siempre** `-=` en `OnDisable`. Se vacían al arrancar para que no queden suscriptores de una partida anterior.
- **Demuestra:** eventos / patrón Observer.

## PlayerController (`Player/PlayerController.cs`)
- **Qué hace:** mueve a Kael relativo a la cámara, salto con *coyote time* (0.12 s) y *jump buffer* (0.12 s), gravedad y detección de suelo. También se teletransporta al recibir `PlayerRespawned`.
- **Por qué existe:** es el control del juego. `CharacterController` (sin Rigidbody) es predecible y fácil de explicar. El suelo se detecta con `CharacterController.isGrounded` **más** una esfera bajo los pies (`Physics.OverlapSphereNonAlloc`) porque `isGrounded` parpadea con framerates altos. `minMoveDistance = 0` evita que el `CharacterController` descarte movimientos de menos de 1 mm (con muchos FPS Kael perdía el arrastre de las plataformas).
- **Cómo se usa:** vive en `Prefabs/Kael.prefab`. Las acciones de input se crean en código (teclado y mando). Se bloquea con `CinematicStarted`.
- **Demuestra:** física, input (Input System).
- **Fórmula del salto:** velocidad inicial `v = √(2·g·h)` con g = 25 y h = 1.8 m.

## ThirdPersonCamera + Cinemachine (`Player/ThirdPersonCamera.cs`)
- **Qué hace:** lee ratón / stick derecho y gira el pivote `CameraTarget` (hijo de Kael); si el pivote salta más de 6 m en un fotograma (respawn) avisa a Cinemachine (`OnTargetObjectWarped`) para que la cámara salte con él. Una `CinemachineCamera` con *Third Person Follow* sigue ese pivote y copia su rotación (*Rotate With Follow Target*); evita atravesar el suelo con *Avoid Obstacles*.
- **Por qué existe:** separa lo que quiere mirar el jugador (este script) de cómo se coloca la cámara (Cinemachine). Las cámaras de evento (H4) solo subirán su prioridad.
- **Cómo se usa:** la escena la monta el builder. Clic en la ventana de juego captura el cursor; `Esc` lo libera.
- **Demuestra:** cámara, input.

## FragmentData / Fragment / FragmentManager (`Fragments/`)
- **Qué hace:** `FragmentData` es un archivo (ScriptableObject) con id, nombre, mensaje, `eventId` y color. `Fragment` es el coleccionable (gira, flota, se encoge hacia Kael al tocarlo) y avisa `FragmentCollected`. `FragmentManager` cuenta y, por cada id nuevo, lanza `FragmentCountChanged` y `WorldEventRequested(eventId)`.
- **Por qué existe:** datos separados del código (se editan en el Inspector) y una sola autoridad que decide qué cuenta: un fragmento nunca dispara su evento dos veces (idempotencia con `HashSet`).
- **Cómo se usa:** datos en `Assets/Data/`; el prefab `Fragmento` se coloca en la escena y se le asigna su `FragmentData`.
- **Demuestra:** ScriptableObjects, triggers (física), eventos.

## WorldEventBase y BridgeAppear (`WorldEvents/`)
- **Qué hace:** `WorldEventBase` escucha `WorldEventRequested`, comprueba su `eventId`, ejecuta `Run()` **una sola vez** y avisa `WorldEventFinished`. `BridgeAppear` hace crecer el puente escalando un pivote en Z con una `AnimationCurve` (2 s).
- **Por qué existe:** toda variante (escalera, plataforma, compuerta…) reutiliza la suscripción y la idempotencia; solo escribe su animación. Versión barata del efecto: escala + material emisivo.
- **Cómo se usa:** hijas de `WorldEventBase` sobrescriben `Prepare()` (estado inicial) y `Run()` (corrutina). El componente va en un objeto **activo**; lo que se oculta son sus hijos.
- **Demuestra:** animación (curvas, corrutinas), herencia, eventos.

## StairsMaterialize (`WorldEvents/StairsMaterialize.cs`)
- **Qué hace:** evento del Fragmento 2. Los 20 escalones (rocas) están ocultos; uno tras otro (cada 0.12 s) salen desde abajo, girando y encogidos, hasta su sitio (0.5 s, `AnimationCurve`) y se vuelven sólidos al llegar.
- **Por qué existe:** versión barata de "las rocas flotantes se reordenan": solo posición, giro y escala. Cada escalón sube 0.25 m (menos que el *Step Offset* de Kael, 0.3) para subir caminando sin saltar.
- **Cómo se usa:** el objeto raíz queda activo; los escalones son hijos y se asignan en `Steps` de abajo hacia arriba.
- **Demuestra:** animación (curvas, corrutinas en paralelo), herencia.

## MovingPlatformActivate + MovingPlatformCarrier (`WorldEvents/`, `Player/`)
- **Qué hace:** `MovingPlatformActivate` (Fragmento 3) enciende una plataforma apagada y la mueve de ida y vuelta 16 m (3.5 m/s, 2 s de pausa en cada extremo) con `Rigidbody.MovePosition` en `FixedUpdate` y suavizado `SmoothStep`. `MovingPlatformCarrier` (en Kael) lo desplaza lo mismo que la plataforma que pisa.
- **Por qué existe:** el `CharacterController` no viaja con el suelo. Truco explicable: se guarda la posición de Kael **en coordenadas locales de la plataforma** al final del fotograma; al siguiente se convierte de vuelta a coordenadas del mundo y la diferencia es lo que se movió la plataforma (vale también si gira).
- **Cómo se usa:** la plataforma necesita Rigidbody cinemático + collider; el carrier detecta cualquier collider con Rigidbody cinemático bajo los pies (`PlayerController.GroundCollider`).
- **Demuestra:** física (cuerpos cinemáticos, interpolación), animación.

## DoorOpen, RotatingPlatformsAlign y CoreRestoration (`WorldEvents/`)
- **Qué hacen:** eventos de los fragmentos 4, 6 y 7, todos hijos de `WorldEventBase`. `DoorOpen`: la losa tiembla y se hunde (F4). `RotatingPlatformsAlign`: cuatro plataformas giran 180° en cadena hasta formar la escalera de saltos a la cima (F6). `CoreRestoration`: siete esquirlas se ensamblan en el Núcleo, una onda de luz aclara el ambiente y se avisa con `GameEvents.CoreRestored` (F7). El fragmento 5 reutiliza `BridgeAppear`.
- **Por qué existen:** cada fragmento = un evento, una sola vez (regla del juego). La base ya hace la suscripción y la idempotencia.
- **Cómo se usan:** los arma el builder. Toda subida entre plataformas ≤ 1.5 m y brecha ≤ 3 m (Kael salta 1.8 m): las pruebas lo comprueban.

## ExitPortal y EndScreen (`Core/`, `UI/`)
- **Qué hacen:** `ExitPortal` está sellado (disco con collider, trigger apagado) hasta `CoreRestored`; entonces se enciende y, al cruzarlo, lanza `LevelCompleted`. `EndScreen` muestra "Región restaurada" y quita el control.
- **Nota:** el menú principal y la pausa llegan en el siguiente bloque de H3.

## CameraEventShot y GroundMarker (`Player/`)
- **Qué hacen:** `CameraEventShot` sube una segunda cámara virtual (prioridad 20; la de Kael tiene 10) para enseñar el objeto del evento unos segundos y bloquea el control. `GroundMarker` dibuja un disco bajo Kael para juzgar dónde aterriza.
- **Por qué existen:** regla "cada evento tiene feedback visible" y legibilidad de Kael frente al entorno (cian/blanco = solo Kael; luz del mundo = violeta; fragmentos = ámbar).

## KillZone, CheckpointZone y RespawnManager (`Core/`)
- **Qué hace:** `KillZone` (caja plana en y = -40) avisa `PlayerFell`; `CheckpointZone` (una caja sobre cada isla) avisa `CheckpointReached(posición)`; `RespawnManager` guarda el último checkpoint (en memoria) y, al caer, lanza `PlayerRespawned(posición)`, que `PlayerController` atiende.
- **Por qué existe:** Kael no tiene vida ni combate: el único fallo es caer. Tres piezas pequeñas que no se conocen entre sí, unidas por eventos.
- **Cómo se usa:** las coloca `Fragmento > Construir Escena`; cada checkpoint tiene un objeto `PuntoRespawn` hijo, lejos del borde.
- **Demuestra:** triggers (física), eventos.

## HUDController (`UI/HUDController.cs`)
- **Qué hace:** muestra "Fragmentos n / 7" arriba a la izquierda y mensajes cortos abajo (aparecen, esperan 4 s y se desvanecen con un `CanvasGroup`; uno nuevo reemplaza al anterior).
- **Por qué existe:** es el único script que dibuja información de juego y solo escucha `FragmentCountChanged` y `MessageRequested`.
- **Cómo se usa:** mostrar un texto desde cualquier script: `GameEvents.RaiseMessageRequested("...")`. El texto de cada fragmento sale de su `FragmentData`.
- **Demuestra:** UI (Canvas, TextMeshPro, escalado por resolución).
- **Nota:** TextMeshPro necesita sus recursos esenciales en `Assets/TextMesh Pro/` (fuente Liberation Sans, licencia SIL OFL; ya incluidos en el repositorio). Se quitaron los emojis de EmojiOne porque su licencia no está verificada.

## FragmentoSceneBuilder (`Editor/FragmentoSceneBuilder.cs`)
- **Qué hace:** menú `Fragmento > Construir Escena`: crea materiales URP emisivos, datos, prefabs, perfil de Bloom y la escena `Nivel01_Santuario` (islas A–G, jugador, cámara, los 7 fragmentos y sus eventos, núcleo, portal, checkpoints, zona de caída, respawn, HUD y pantalla final).
- **Por qué existe:** la escena es reproducible y no genera conflictos de Git entre las dos personas. Sin ver el Editor, también se puede ejecutar por consola (`-executeMethod FragmentoSceneBuilder.BuildFromCommandLine`).
- **Cómo se usa:** se vuelve a ejecutar cuando cambia el nivel. **Sobrescribe la escena**: los cambios van al builder o a los prefabs.
- **Demuestra:** iluminación (materiales emisivos, Bloom, niebla), herramientas de Editor.

## Pruebas (`Assets/Tests/PlayMode/`)
- **Qué hace:** 21 pruebas con la escena real y un teclado simulado (a 60 FPS). `PrototipoH1Tests` (8): caer al suelo, caminar, saltar (≈ 1.8 m), coyote time, cámara, F1 → puente una sola vez, caída sin puente, cruce con puente. `VerticalSliceH2Tests` (7): HUD (contador y mensaje), F2 → escalera y subida a C, F3 → plataforma y viaje sin resbalar hasta D, respawn en el último checkpoint (y en el inicio), ruta completa A → B → C → D y que ningún fragmento flote sin suelo. `CompleteLevelH3Tests` (6): compuerta, puente 5, plataformas giratorias (geometría y recorrido real saltando), portal sellado, y ruta de los 7 fragmentos hasta la pantalla final con los 7 eventos una sola vez. `LevelTestBase` carga la escena y ofrece ayudas comunes.
- **Cómo se usa:** *Window > General > Test Runner > PlayMode*, o por consola (`-runTests -testPlatform PlayMode`, **con el Editor cerrado**). Ejecutar antes `Fragmento > Construir Escena`.
- **Demuestra:** pruebas automáticas (Unity Test Framework + Input System Test Fixture).
