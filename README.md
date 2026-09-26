# Museo Autista VR

Aplicación de realidad virtual para **Android + Google Cardboard**, hecha en Unity 6 (6000.6.0f1).
Es un museo de "intereses especiales" con estilo cartoon: **aviones de combate, dinosaurios, hongos y carros**.

## Escenas (4)

| Escena | Contenido |
|---|---|
| **Inicio** | Plaza frente al museo, menú con Tutorial / Recorrido / Salir y paneles de instrucciones. |
| **Tutorial** | 6 pasos guiados: puntero Gaze, teletransporte, agarrar, colocar y animar un modelo. |
| **Recorrido** | Pasillo largo con 4 entradas enormes a las salas (Aviones, Dinosaurios, Hongos, Carros), 13 puntos de salto y una misión: devolver 4 piezas perdidas a su sala. |
| **Felicitaciones** | Trofeo, confeti, resumen (tiempo, piezas y salas visitadas) y botones Inicio / Repetir / Salir. |

## Mecánicas

- **Gaze**: retículo blanco con borde oscuro (alto contraste sobre cualquier fondo). Al mirar un objeto interactivo aparece un anillo que se llena; al completarse, el objeto se activa. También se activa con el botón del visor Cardboard (toque de pantalla).
- **Teletransporte**: círculos azules en el suelo con un fundido corto a negro para evitar mareo.
- **Agarre y posicionamiento**: al mirar una pieza, esta flota delante de la cámara. Se coloca mirando el pedestal con el aro amarillo, que solo acepta la pieza correcta y avisa si no lo es.
- **Animaciones**: los aviones encienden la postcombustión y despegan, los dinosaurios rugen, los fósiles se desarman y se vuelven a armar, y los carros encienden luces y giran las ruedas. Todo con sonidos sintetizados.
- **Contemplar**: cada placa tiene un botón *Contemplar*. Al usarlo, lo que está detrás de la pieza se oscurece como un foco de museo, la pieza gira lentamente, suena el audio de contemplación y la música baja. Termina a los 20 s o con *Dejar de contemplar*.
- **Reproductor de música**: ícono abajo a la izquierda de la vista (sigue al cuerpo, no a la cabeza). Al mirarlo se abre con la portada de la canción, anterior / play-pausa / siguiente y *música del celular*, que lee las carpetas Music y Download. Se esconde solo al dejar de mirarlo.
- **Cardboard**: escaneo del QR del visor, botón ⚙ (configurar), botón X (volver al inicio o salir desde el inicio) y recentrado manteniendo pulsado el visor.

## Usabilidad / confort visual

- Paleta pastel desaturada y sombreado toon con contorno. No hay colores saturados ni texturas ruidosas.
- Sin sombras dinámicas, MSAA 2x y mallas combinadas para mantener 60 FPS en móviles.
- Textos grandes sobre paneles claros con texto oscuro.

## Cómo generar y compilar

El contenido se genera por código desde `Assets/Editor/SceneBuilder.cs`. En el menú **Museo Autista** del editor:

1. **Configurar proyecto (Android + Cardboard)**: IL2CPP, ARM64, API 26+, OpenGL ES 3 y el loader Cardboard.
2. **Construir escenas**: regenera las 4 escenas en `Assets/Scenes`.
3. **Compilar APK**: genera `Builds/MuseoAutistaVR.apk`.
4. **Capturas de verificación**: guarda imágenes en `Screenshots/`.

Por línea de comandos:

```
Unity.exe -batchmode -quit -buildTarget Android -projectPath . -executeMethod MuseoAutista.EditorTools.SceneBuilder.BatchAll
```

Para probar en el editor sin visor, pulsa Play en la escena `Inicio`. Mantén el clic derecho y mueve el ratón para mirar; el clic izquierdo o Espacio hacen de gatillo.

## Modelos 3D, fuentes y sonido

| Contenido | Origen |
|---|---|
| Aviones | Asset Store (gratis): *Fighter Jet Low Poly* (Raptor3D) y *POLYGRUNT – Low Poly Rafale Jet*. También hay un F-22 y un avión acrobático cartoon hechos por código. |
| Carros | Asset Store (gratis): *FREE CAR PACK – Low Poly* (Quint). |
| Dinosaurios | Asset Store (gratis): *PBR Animated Dinosaurs* (Ferocious Industries): velociraptor, estegosaurio y paquicefalosaurio con animación de reposo y rugido. |
| Fósiles | Esqueletos y cráneo modelados en **Blender** con el script `Blender/dinos_cartoon.py`. |
| Hongos | Asset Store (gratis): *Low Poly Mushrooms Pack* (Papersy). |
| Sonidos | Asset Store (gratis): *Car and transportation sounds collection* (Juggernaut Realm) y los rugidos del pack de dinosaurios. |
| Fuentes | Títulos: **AmazDooM** (Amazingmax, gratuita de dafont.com). Textos: **Black Ops One** (licencia SIL OFL, Google Fonts). |

**Música de fondo:** coloca un archivo `.mp3`, `.ogg` o `.wav` en `Assets/Audio/Musica/` y reconstruye las escenas. Suena en bucle a volumen bajo en todas las escenas. Usa música con licencia libre, como la biblioteca de audio de YouTube (YouTube Studio > Biblioteca de audio), Pixabay Music o un pack gratuito de la Asset Store.

> Por la licencia EULA de la Asset Store, los paquetes **no se suben al repositorio**. Para tenerlos,
> impórtalos desde *Window > Package Manager > My Assets* y ejecuta **Museo Autista > 2. Construir escenas**.
> Si faltan, el proyecto usa modelos hechos por código y sonidos sintetizados.

Para regenerar los fósiles desde Blender:

```
blender --background --python Blender/dinos_cartoon.py -- Assets/Models/Blender
```

Para cambiar cualquier modelo por otro prefab, edita `Assets/Config/ModelLibrary.asset`. Cada entrada tiene un id, un prefab, un tamaño y un giro.

## Estructura

```
Assets/
  Scripts/Gaze      GazePointer, GazeInteractable, GazeButton, TeleportPoint
  Scripts/Core      PlayerRig, ScreenFader, SceneFlow, CardboardManager, Sfx, GameState
  Scripts/Museum    Grabbable, PlacementSocket, ExhibitAnimator, MuseumProgress, TutorialController
  Editor            SceneBuilder, ModelFactory, Kit, ModelLibrary, Screenshots
  Shaders           Toon, ToonOutline, GazeOverlay
  Scenes            Inicio, Tutorial, Recorrido, Felicitaciones
```
