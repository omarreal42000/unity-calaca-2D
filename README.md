# Calavera Memorama (Unity 2022.3.62f3 · 2D)

Memorama 2D con una **calavera negra** como protagonista sobre una paleta cálida (ámbar, magenta, cian y crema) que contrasta con ella.
Proyecto escolar de *Desarrollo de Videojuegos*.

## 1. Instalación (3 pasos)

1. Abre tu proyecto (`unitymemoo`) en **Unity 2022.3.62f3** (o crea uno nuevo "2D (Built-In Render Pipeline)").
2. Copia la carpeta **`Assets/Calavera`** dentro de la carpeta `Assets` de tu proyecto y espera a que compile.
3. En el menú superior elige **Calavera → Construir proyecto completo**. Se generan sprites, animaciones, prefab y las 4 escenas, y se registran en *Build Settings*. Presiona **Play**.

> Si tu proyecto usa **solo** el nuevo Input System, en *Edit → Project Settings → Player → Active Input Handling* elige **Both** (o Input Manager (Old)).
> Para entregar como aplicación de **escritorio**: *File → Build Settings → PC, Mac & Linux Standalone → Switch Platform → Build*.

## 2. Escenas (interacción en todas)

| Escena | Qué hace | Interacción |
|---|---|---|
| `MenuPrincipal` | Menú de inicio profesional | Jugar, cambiar dificultad, instrucciones, salir, clic en la calavera |
| `Instrucciones` | Reglas del juego | Volver, clic en la calavera, tecla Esc |
| `Juego` | Memorama (6, 8 o 10 pares) | Clic en cartas, Reiniciar, Menú, teclas R / Esc |
| `Resultados` | Movimientos, tiempo, puntuación, estrellas y récord | Jugar de nuevo, Menú principal |

## 3. Estados de animación (Animator)

**Carta** (`Card.controller`): `Idle → Flip → Revealed → Match → Matched` o `Revealed → Mismatch → UnFlip → Idle`.
**Calavera** (`Mascot.controller`): `Idle` y, desde cualquier estado, `Saludo`, `Acierto`, `Error`.

Todas las transiciones se disparan con **Triggers** desde código cuando ocurre un evento. En `Idle` no hay movimiento: **la animación se reproduce al seleccionar el objeto y no antes**.

## 4. Pruebas de resultados

*Window → General → Test Runner → EditMode → Run All* (12 pruebas en `Assets/Calavera/Tests`): mazo con pares correctos, mezcla reproducible, aciertos, errores, cartas repetidas, juego completo, puntuación y estrellas.
Prueba manual sugerida: jugar una partida completa en cada dificultad y confirmar que la pantalla de resultados coincide con lo jugado.

## 5. Guion para el video (máx. 2 minutos)

- **0:00–0:15** Presenta el proyecto y muestra el menú de inicio; haz clic en la calavera (animación *Saludo*).
- **0:15–0:35** Instrucciones, regresa y cambia la dificultad.
- **0:35–1:15** Partida: voltea una carta (*Flip*), forma una pareja (*Match*) y falla una (*Mismatch → UnFlip*). Señala que antes del clic no hay animación.
- **1:15–1:35** Muestra la ventana *Animator* de la carta con sus estados y triggers.
- **1:35–1:50** Pantalla de resultados y ejecución de las pruebas en el Test Runner.
- **1:50–2:00** Cierre y mención del repositorio.

## 6. Repositorio público

```
git init
git add .
git commit -m "Calavera Memorama - proyecto final"
git branch -M main
git remote add origin https://github.com/TU_USUARIO/calavera-memorama.git
git push -u origin main
```
Crea el repositorio en GitHub como **Public** y entrega ese enlace junto con el del video. El `.gitignore` ya excluye `Library/`, `Temp/` y `Logs/`.
