# CREDITS

Registro de todo lo que no escribió el equipo (código, assets, audio, paquetes). Es evidencia académica: cada fila debe tener la licencia verificada en la fuente.
Para añadir una fila usa la skill `add-credit`.

| Nombre | Autor | URL | Licencia | Qué se usó | Dónde (ruta en el proyecto) | Cambios hechos |
|---|---|---|---|---|---|---|
| Plantilla "3D URP" (`com.unity.template.urp-blank` 17.0.14) | Unity Technologies | Incluida en el instalador de Unity 6000.3.23f1 (`Editor/Data/Resources/PackageManager/ProjectTemplates`) | Unity Companion License for Unity-dependent projects (texto en el `LICENSE.md` de la plantilla) | Ajustes de URP (`PC_RPAsset`, `PC_Renderer`, `Mobile_*`, `DefaultVolumeProfile`, `UniversalRenderPipelineGlobalSettings`), `ProjectSettings/` base y `InputSystem_Actions.inputactions` | `Assets/Settings/`, `ProjectSettings/`, `Assets/InputSystem_Actions.inputactions` | Se quitaron la escena de ejemplo y el material tutorial; se renombró el producto a "Fragmento" |
| Input System (`com.unity.inputsystem` 1.20.0) | Unity Technologies | Unity Package Manager | Unity Companion License for Unity-dependent projects (verificada en el `LICENSE.md` del paquete) | Lectura de teclado, ratón y mando con `InputAction` creadas en código | `Packages/manifest.json` | Ninguno |
| Cinemachine (`com.unity.cinemachine` 3.1.7) | Unity Technologies | Unity Package Manager | Unity Companion License for Unity-dependent projects (verificada en el `LICENSE.md` del paquete) | Cámara en tercera persona (*Third Person Follow*) y cámaras de evento | `Packages/manifest.json` | Ninguno |
| Universal Render Pipeline (`com.unity.render-pipelines.universal` 17.3.0) | Unity Technologies | Incluido en el editor | Unity Companion License for Unity-dependent projects (verificada en el `LICENSE.md` del paquete) | Render URP, materiales emisivos y Bloom | `Packages/manifest.json` | Ninguno |
| Unity UI (`com.unity.ugui` 2.0.0, incluye TextMeshPro) | Unity Technologies | Unity Package Manager | Unity Companion License for Unity-dependent projects (verificada en el `LICENSE.md` del paquete) | HUD y menús | `Packages/manifest.json` | Ninguno |
| Unity Test Framework (`com.unity.test-framework` 1.6.0) | Unity Technologies | Unity Package Manager | Unity Companion License for Unity-dependent projects (verificada en el `LICENSE.md` del paquete) | Pruebas EditMode/PlayMode | `Packages/manifest.json` | Ninguno |
| TextMesh Pro Essential Resources (shaders, `TMP Settings`, fuente SDF) | Unity Technologies | `Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage` (incluido en el paquete `com.unity.ugui` 2.0.0) | Unity Companion License for Unity-dependent projects (paquete `com.unity.ugui`) | Dibujar el texto del HUD con TextMeshPro | `Assets/TextMesh Pro/` | Se extrajo con un script conservando los GUID; **no se incluyen los emojis de EmojiOne** (licencia sin verificar) y se quitó su referencia en `TMP Settings` |
| Liberation Sans (`LiberationSans.ttf`) | Digitized data © 2010 Google Corporation (Arimo, Tinos, Cousine); © 2012 Red Hat, Inc. | Incluida en el mismo paquete de TextMeshPro | SIL Open Font License 1.1 (texto en `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`). Es una licencia permisiva de fuentes que no figura en la lista del proyecto; se acepta por venir dentro del paquete oficial aprobado (ver `decisions.md`) | Fuente por defecto del HUD | `Assets/TextMesh Pro/Fonts/` | Ninguno |

## Excluido por licencia sin verificar
- EmojiOne (sprites de ejemplo de TextMeshPro): su archivo de atribución solo remite a su web; no se usa ni se distribuye.

## Pendientes de verificar antes de importar (no están en el proyecto todavía)
- Kenney Platformer Kit, Particle Pack e Impact Sounds (CC0): se registrarán al importarlos en H4.
- Kenney Sci-fi Sounds, Interface Sounds, Music Jingles y Skyboxes: licencia **no verificada**; comprobar la página del pack antes de importar.
- Música ambiente: sin elegir.
