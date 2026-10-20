# Fragmento: El Restaurador de Sombras

Plataformas 3D de exploración en tercera persona para PC (Unity 6, URP). Kael, un pequeño espíritu guardián, recorre un santuario de islas flotantes y absorbe siete Fragmentos del Núcleo; cada uno reconstruye una parte del mundo hasta abrir el Portal de Salida.

Proyecto del curso **Desarrollo de Videojuegos** (equipo de 2 estudiantes). Historia: `historia_de_fragmento.md`. Plan y progreso por hitos: `PLAN_FRAGMENTO.md`.

## Cómo abrir el proyecto
1. Instala **Unity 6000.3.23f1** desde Unity Hub (la versión exacta está en `ProjectSettings/ProjectVersion.txt`).
2. Unity Hub → **Add** → selecciona esta carpeta (la raíz del repositorio, donde están `Assets/`, `Packages/` y `ProjectSettings/`).
3. Espera a que Unity importe el proyecto y descargue los paquetes (la primera vez tarda varios minutos).
4. Menú **Fragmento > Construir Escena** para generar la escena `Assets/Scenes/Nivel01_Santuario.unity`.
5. Abre esa escena y pulsa **Play**.

> La escena se **genera por script**. No la edites a mano: se sobrescribe cada vez que se ejecuta el menú. Los cambios compartidos van en prefabs (`Assets/Prefabs/`) o en el propio script `Assets/Editor/FragmentoSceneBuilder.cs`.

## Controles
| Acción | Teclado y ratón | Mando |
|---|---|---|
| Mover | `W` `A` `S` `D` o flechas | Stick izquierdo |
| Saltar | `Espacio` | Botón sur (A / ✕) |
| Mirar | Ratón | Stick derecho |
| Liberar el cursor | `Esc` (clic en la ventana para volver a capturarlo) | — |

## Estructura
- `Assets/Scripts/` — código del juego (`Core`, `Player`, `Fragments`, `WorldEvents`, `UI`).
- `Assets/Editor/` — `FragmentoSceneBuilder` (genera materiales, datos, prefabs y escena).
- `Assets/Data/` — ScriptableObjects (`FragmentData`).
- `Assets/Tests/` — pruebas de Unity Test Runner.
- `docs/SISTEMAS.md` — qué hace cada sistema, por qué existe y cómo se usa (para defenderlo en clase).
- `CREDITS.md` — todo lo ajeno con su licencia.

## Pruebas
Unity → **Window > General > Test Runner** → pestaña *PlayMode* → *Run All* (15 pruebas, ~1 min). Requiere haber ejecutado antes `Fragmento > Construir Escena`.
Por consola, con el Editor **cerrado**: `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults resultados.xml`.

## Compilar (build de Windows)
Aún no hay build (se hace en el hito H5). Pasos previstos: **File > Build Profiles** (o *Build Settings*) → Windows x86_64 → añadir las escenas del juego → *Build*. La carpeta `Build/` no se versiona.

## Créditos
Ver `CREDITS.md`.
