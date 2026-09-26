# PA3 – Matriz de avance y evidencias (Unity)

Actualizado: 22/09/2026. Estado: primer bloque de implementación Unity. Esta matriz registra presencia estructural en el proyecto; “implementado” no equivale a criterio aprobado ni a prueba visual/play mode.

## Auditoría inicial

- Proyecto: PRODUCTO ACADEMICO 2 DESARROLLO DE VIDEOJUEGOS.
- Unity 6000.7.0b1 (Beta); URP 17.7.0; Input System 6.7.0; Cinemachine 3.1.7; Timeline 6.7.0; AI Navigation 2.0.14.
- Escena original `Assets/Scenes/SampleScene.unity` preservada y todavía incluida en Build Settings. Nueva escena `Assets/Scenes/PA3_VerticalSlice.unity` también incluida.
- Git existe; el proyecto ya tenía cambios locales previos. No se restablecieron ni eliminaron esos cambios.
- ProBuilder se probó, pero su paquete no compila en esta versión beta de Unity (errores de API obsoleta en TreeView/BinaryFormatter); se retiró del manifest para recuperar compilación limpia. No afirmar uso de ProBuilder.
- Registro de construcción: `Logs/Editor.log` contiene `PA3_BUILD_OK`; al cierre, 0 errores C# y 0 excepciones recientes. En Console visual, contadores de logs/avisos/errores en 0.
- Incidencias reales encontradas y corregidas en la revisión visual: referencias de scripts faltantes por agrupar varios MonoBehaviours en `PA3Runtime.cs`; el controlador usaba `UnityEngine.Input` aunque el proyecto solo admite Input System; el modelo inicial era una cápsula sin silueta humana.
- Correcciones aplicadas: un archivo `.cs` por componente; el jugador usa `Keyboard.current` del Input System; modelo humanoide low-poly con cabeza, torso y extremidades; cámara apuntada al jugador desde el inicio y seguimiento más cercano; HUD con UI Text integrada, sin exigir importar TMP Essentials.

## Matriz de estado

| Bloque / requisito | Estado | Evidencia en proyecto | Pendiente de validación |
|---|---|---|---|
| Auditoría, URP y organización | Implementado | Packages/manifest.json; Assets/PA3/{Editor,Scripts,Shaders,Materials,Prefabs,Timeline,Textures}; escena PA3 en EditorBuildSettings | Abrir GUI, Console visual y confirmar resolución de paquetes en Package Manager |
| Terreno, escenario 3D y verticalidad | Implementado parcialmente | PA3_VerticalSlice.unity; terreno heightmap, plataformas/estructuras y props generados por PA3SceneBuilder | Captura de escena; revisar composición, navegación y que terreno/texturas se vean correctamente |
| Control 3D y cámara | Parcial, corregido y visible | Prefabs/Player_3D.prefab ahora tiene referencias válidas; PlayerController3D usa Input System; modelo humanoide; cámara follow inicial. Game view muestra jugador y HUD en Play Mode | Cero errores al iniciar; falta confirmar contigo recorrido continuo WASD, colisiones y recoger objetos |
| Interacción Trigger/Collider y objetivo | Implementado | Collectible, HazardTrigger, ExitTrigger; seis coleccionables; HUD y beacon | Probar recoger, reinicio por peligro y condición de salida en Play Mode |
| VFX / partículas | Implementado | Prefabs/EnergyMistVFX.prefab y escena | Verificar emisión/visibilidad en Game view |
| Shader dinámico | Parcial | Shaders/PA3DynamicPulse.shader (pulso temporal URP); material asignado al beacon | Confirmar cambio visual en Game view. `DynamicWorld.shadergraph` es un recurso adicional aún no comprobado/limpiado como Shader Graph URP válido; no dar por cumplido requisito Shader Graph |
| Cinemachine + Timeline 5–10 s | Estructura implementada | Timeline/PA3_Cinematic_7_5s.playable (7,5 s); director y cámara en escena | Reproducir y verificar encuadre/clip desde Play Mode y Timeline window |
| Iluminación mixta + Post Processing | Implementado estructuralmente | Luz Baked y Realtime; URP Volume/Profile con Bloom y Color Adjustments | Validar bake real, iluminación y efecto en Game view; capturar evidencia |
| Prefabs y colaboración Git | Parcial | Prefabs Player_3D, EnergyCrystal, EnergyMistVFX, GuardianEnemy y NarrativeBeacon; repo Git existente | Revisar vínculos/overrides y coordinar ramas/commits con el equipo; no se creó commit |
| Console y QA | Estructura recompilada; revisión visual limpia | `Logs/Editor.log`: PA3_BUILD_OK; cinco prefabs sin `m_Script` nulo; Console observada con contadores 0/0/0 | Console estaba limpia durante la prueba; queda una pasada manual completa de movimiento e interacciones |

## Evidencias que faltan para cerrar criterios

1. Guardar captura final del Game view y de Scene con terreno, verticalidad, jugador, props, iluminación y VFX.
2. Prueba Play Mode completa: mantener WASD, colisiones, seis coleccionables, peligro/reinicio y salida. El inicio/HUD/cinemática ya se vieron; el control continuo quedó interrumpido por entrada del usuario durante la inspección.
3. Reproducción comprobada de la cinemática completa (7,5 s) en Timeline/Cinemachine.
4. Verificación visual del Shader Graph personalizado URP y su lógica dinámica (hoy existe un shader HLSL equivalente como implementación provisional).
5. Captura de iluminación con bake y del Post Processing activo; Console sin errores/warnings relevantes.
6. Video-defensa conforme al tiempo requerido por el documento oficial y entrega Git coordinada.

No se asigna puntaje ni se declara ningún criterio de rúbrica como cumplido todavía: el jugador y HUD ya son visibles y Console quedó limpia, pero faltan pruebas completas de gameplay y varios requisitos visuales/técnicos.
