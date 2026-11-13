<div align="center">

**Español** · [English](README.en.md)

# Contador de cofres de Fortnite

![Windows 10 | 11](https://img.shields.io/badge/Windows-10%20%7C%2011-3a7bd5?style=flat-square) [![licencia MIT](https://img.shields.io/badge/licencia-MIT-555?style=flat-square)](LICENSE)

**Cuenta cada cofre que abres en Fortnite, partida tras partida: ventana, informe, overlay sobre el juego y los cofres de tu equipo.**

<img src="docs/main.png" alt="Ventana principal" width="49%">&nbsp;<img src="docs/report-hero.png" alt="Informe" width="49%">

[**⬇ Descargar ChestCounter.exe**](../../releases/latest) · Windows 10 / 11 · gratis · sin instalación · 10 idiomas

</div>

---

ChestCounter lee las repeticiones (replays) que Fortnite guarda de cada partida y lleva la cuenta de los cofres que abres, de los **tesoros de la caza del tesoro** y, si quieres, de los cofres de tus **compañeros de equipo**. Todo se guarda en tu PC: aunque Fortnite borre los replays, tu historial se conserva.

- **Ventana principal**: total, última partida, hoy, mejor día, media, récord, tesoros, gráfico de 7 días y tu equipo.
- **Informe** en el navegador, con pestañas: resumen, cofres, tesoros, partidas y compañeros.
- **Overlay** sobre el juego y **página para OBS**.
- **Compañeros**: con quién juegas, victorias juntos, cofres de cada uno por partida y un top.
- **Seguro**: solo lee archivos de replay; no toca el juego ni su memoria.

> Cuenta los cofres que **tú** abriste (y los que se vio abrir a tu equipo). No muestra dónde están los cofres en el mapa.

## Primeros pasos

1. Descarga **[`ChestCounter.exe`](../../releases/latest)**. Ese único archivo es todo lo que necesitas.
2. En Fortnite activa *Ajustes → Juego → Repeticiones → **Grabar repeticiones*** (y *Modos de equipos grandes* si los juegas). Sin repeticiones no hay nada que leer.
3. Ejecuta `ChestCounter.exe`.
   > Windows puede mostrar *"Windows protegió tu PC"* porque el exe no está firmado. Haz clic en *Más información → Ejecutar de todas formas*.
4. La bienvenida te recuerda lo de las repeticiones y, si ya tienes replays en el PC, te deja **contarlos** o empezar **solo con las próximas partidas**.

<p align="center"><img src="docs/setup.png" alt="Bienvenida del primer inicio" width="520"></p>

5. Deja la ventana abierta (puedes minimizarla) mientras juegas. Durante la partida el contador lee el replay que se está grabando y suma los cofres **en directo**; al terminar, se confirman.

> [!TIP]
> Las partidas que juegas con el contador cerrado se cuentan la próxima vez que lo abras, mientras Fortnite no haya borrado esos replays (guarda un número limitado). Para no tener que pensar en ello, activa **Abrir con Fortnite** en Ajustes.

## La ventana principal

<p align="center"><img src="docs/main.png" alt="Ventana principal" width="680"></p>

- **Arriba a la izquierda**: tu total de cofres, la última partida (o la actual, en verde, mientras juegas) y la sesión.
- **A la derecha**: hoy, el mejor día, la media, la mejor partida, el histórico (partidas guardadas y desde cuándo) y los tesoros.
- **Últimos 7 días**: un gráfico con los cofres de cada día.
- **Equipo** (con *Compañeros* activado): tus cofres y los de cada compañero en la partida actual o la última, el total del equipo y lo que aportan ellos.
- **Botones**: **Overlay** (punto verde = activado), **Informe** y **Ajustes**. Si hay varias cuentas en el PC, arriba aparece un selector de perfil.

La ventana se puede redimensionar y maximizar: todo crece en proporción.

Las estadísticas incluyen únicamente las playlists reconocidas de Battle Royale y Cero construcción (v43 o posterior). Ajustes también se puede redimensionar y maximizar.

## El informe

`report.html` se abre en tu navegador (botón **Informe**) y se actualiza solo. Empieza siempre en **Resumen**; arriba tienes las pestañas:

| Pestaña | Qué muestra |
|---|---|
| **Resumen** | Total de cofres, sesión, victorias (y %), top 10, partidas, media, récord, mejor día, tesoros, gráfico de 30 días y las últimas partidas. |
| **Cofres** | Por tipo de cofre, otros contenedores (cajas de munición, de comida, cañones; indica si suman al total) y una tabla **por modo de juego**. |
| **Tesoros** | Cuántos, de cofres dorados, con origen seguro, % de partidas con tesoro, de dónde vienen y la lista con el momento exacto. |
| **Partidas** | Las 100 últimas: modo, puesto (🏆 victoria, top 10 en verde), eliminaciones, cofres, tesoros y, con compañeros, el equipo. |
| **Compañeros** | Cofres del equipo, **top de compañeros**, la tabla **por partida** y todos tus compañeros (ver abajo). |

<p align="center"><img src="docs/report-companeros.png" alt="Pestaña Compañeros del informe" width="820"></p>

<details>
<summary><b>Ver las demás pestañas</b></summary>

<p align="center"><img src="docs/report-resumen.png" alt="Resumen" width="820"></p>
<p align="center"><img src="docs/report-cofres.png" alt="Cofres" width="820"></p>
<p align="center"><img src="docs/report-tesoros.png" alt="Tesoros" width="820"></p>
<p align="center"><img src="docs/report-partidas.png" alt="Partidas" width="820"></p>

</details>

Usa los colores de la ventana (oscuro, o claro si Windows está en modo claro) y se adapta al móvil.

## Compañeros y equipo

Actívalo en **Ajustes → Compañeros en el informe** (desactivado por defecto). Verás:

- **Exacto**: con quién juegas, partidas juntos y victorias juntos.
- **Aproximado**: los cofres de cada compañero. Salen de **tu** replay, y solo incluyen las aperturas que Fortnite envió al grabador. No hay un radio máximo garantizado: son un **mínimo (≥)**.
- **Cofres del equipo**: tú + tus compañeros, en total y por partida, con el % que aporta cada uno; útil si buscáis un objeto aleatorio que sale de los cofres.
- **Top de compañeros**: más cofres vistos, mejor media, récord en una partida y más victorias juntos.

Con **Sumar tesoros del equipo** puedes incluir los premios compartidos en tus totales de tesoros. El informe distingue los propios, los del equipo y los de otros jugadores observados; un origen por coincidencia temporal se marca como probable. La tarjeta permanente **Token de Tienda Pirata**, debajo del gráfico y los botones, muestra el estado incluso sin detecciones. Si se identifica uno en la partida actual o anterior, se resalta en dorado e indica quién lo recibió; al pulsarla abre Tesoros. En Ajustes, **Equipo y tesoros compartidos** agrupa mostrar compañeros y sumar sus cofres o tesoros. Los premios de ID numérico desconocido se conservan sin inventar su identidad.

Con **Sumar sus cofres a tu total** (también desactivado), tu total, media, récords y overlay incluyen los cofres de tu equipo. Tu historial no cambia: al desactivarlo vuelves a ver solo los tuyos.

## El overlay

<p align="center"><img src="docs/overlay-espera.png" alt="Overlay entre dos partidas" width="300">&nbsp;&nbsp;<img src="docs/overlay-en-vivo.png" alt="Overlay en directo durante una partida" width="300"></p>

- **Cifras grandes**: tu **total** de cofres y, en el recuadro, la partida **anterior** (o la **actual**, en directo, mientras juegas).
- **Sesión**, **media** de la sesión, **anterior** (durante una partida) y **tesoros**.

Para verlo sobre el juego, pon Fortnite en **Pantalla completa en ventana** (*Ajustes → Vídeo → Modo de ventana*): en pantalla completa exclusiva, Windows no puede dibujar nada encima. El overlay viene **desactivado**: actívalo con el botón **Overlay** o con Ctrl+Mayús+O.

¿Transmites en directo? Mientras el contador está abierto, el mismo overlay está en **http://localhost:7373/** (cópialo desde **Ajustes**). En OBS, añade una **fuente de navegador** con esa dirección: tiene fondo transparente y se actualiza sola.

### Atajos

| Atajo | Qué hace |
|---|---|
| **Ctrl+Mayús+O** | Mostrar / ocultar el overlay. Funciona en cualquier sitio, incluso en el juego. |
| **Ctrl+Mayús+L** | Bloquear / desbloquear el overlay. **Bloqueado**: el ratón lo atraviesa y no se mueve. **Desbloqueado**: arrástralo con el botón izquierdo. |
| Clic derecho en el overlay | Ocultarlo. |

## Ajustes

<p align="center"><img src="docs/settings.png" alt="Ajustes" width="620"></p>

- **Idioma**: por defecto el de Windows; arriba puedes elegir otro (español, inglés, portugués, ruso, francés, alemán, italiano, polaco, turco o japonés).
- **Abrir con Fortnite**: un pequeño proceso invisible abre el contador (minimizado) cuando arranca Fortnite y lo cierra 30 s después de cerrarlo. No lee nada del juego: solo mira la lista de programas en marcha.
- **Abrir el informe al iniciar** (desactivado por defecto).
- **Compañeros en el informe** y **Sumar sus cofres a tu total** (ver arriba).
- **Contar también como cofre**: cajas de munición, cajas de comida y cañones de botín (desactivado; no se borra nada al cambiarlo). Los cofres que **rompes** se cuentan siempre.
- **Cofres antes del contador**: un estimado que se suma a tu total.
- **Actualizaciones**, **overlay para OBS**, **carpeta de replays** y **Restablecer estadísticas** (con copia de seguridad).

## Cómo funciona (y por qué es seguro)

Fortnite puede grabar un **archivo de repetición** de cada partida en tu PC. ChestCounter lee esos archivos y nada más.

- ✅ **Solo lee archivos de repetición**: los `.replay` de `%LOCALAPPDATA%\FortniteGame\Saved\Demos`, en modo solo lectura.
- ✅ **Sin modificar el juego**: no toca la carpeta de instalación de Fortnite.
- ✅ **Sin acceso a la memoria del juego**: nunca lee, escribe ni inyecta nada en el proceso de Fortnite.
- ✅ **No envía datos**: sin inicio de sesión, sin cuenta de Epic, sin telemetría. El único acceso a la red es buscar versiones nuevas en las releases de **este** repositorio de GitHub (se puede desactivar).
- ✅ **Ligero**: prioridad baja ("modo de eficiencia" en Windows 11) y duerme entre partidas.

## Tesoros

- **★★ Cofre dorado (seguro).** Cuando un cofre da un premio de la caza del tesoro (cosmético, pista musical, vehículo…), el servidor genera un cofre dorado con el **id del premio**, que la herramienta lee del replay.
- **★ Cada tesoro recibido (momento exacto)**, con el cofre del que salió; solo cuenta si ese cofre es **tuyo**. Si el objeto no se puede identificar, aparece como **"Artilugio de tesoro"**.
- Un tesoro que sale de algo que no es un cofre (cañón de botín, caja de munición…) se marca con ⚑.

El juego solo envía ids de premios: `treasures.json` les pone nombre y se puede editar con el Bloc de notas.

## Perfiles (varias cuentas en un PC)

No hay nombre que escribir: se cuenta al jugador que grabó cada replay. Si hay replays de varias cuentas, cada una tiene su perfil y arriba a la derecha aparece un **desplegable** para elegir cuál se muestra.

## Actualizaciones

Al iniciarse, el contador busca una versión nueva en las releases del repositorio de GitHub que compiló el exe y, por defecto, la **instala sola** entre dos partidas (tu historial y tus ajustes se conservan). En Ajustes puedes elegir *Preguntar* o *Nunca* (ningún acceso a la red).

El repositorio se graba en el exe al compilarlo con GitHub Actions (`-p:UpdateRepo=usuario/repo`), así que cada exe solo se actualiza desde el repositorio del que salió. Un exe compilado en tu PC con `build.bat` no busca actualizaciones.

## Tus datos

Todo se guarda en `%APPDATA%\ChestCounter`. **Tu historial no depende de los replays**: cada partida (con sus cofres, tesoros y compañeros) se guarda en `data.json` en cuanto se lee, y todas las estadísticas salen de ahí.

| Archivo | Contenido |
|---|---|
| `data.json` | Tu historial completo. **Haz copia de seguridad** si cambias de PC. |
| `report.html` | El informe. |
| `matches.csv` | Una línea por partida, para Excel o Google Sheets. |
| `config.json` | Ajustes, perfil, carpeta de replays y posición del overlay. |
| `treasures.json` | Nombres de los premios de la caza del tesoro. |
| `overlay.html` | Copia del overlay (para OBS es mejor http://localhost:7373/). |
| `errors.log` | Replays que no se pudieron leer, para adjuntar a un informe de error. |

## Preguntas frecuentes

**No se cuenta nada.** Comprueba que las repeticiones están activadas en Fortnite. La ventana te avisa si falta la carpeta de replays o está vacía.

**¿Por qué los cofres de mis compañeros salen bajos?** Porque tu replay puede omitir aperturas lejanas. No hay un radio fijo garantizado. Es un mínimo, no su total.

**¿Cómo empiezo de cero?** **Ajustes → Restablecer estadísticas…** (con confirmación y copia de seguridad `data-backup-….json`).

**¿Modos creativos?** Solo se cuentan las partidas de Battle Royale.

**"No se puede leer esta repetición".** Una actualización de Fortnite puede cambiar el formato. Las partidas ya contadas se conservan. Abre un [issue](../../issues) y adjunta `errors.log`.

**¿Una partida puede contarse dos veces?** No, aunque renombres o guardes el replay.

## Opciones de línea de comandos

```
ChestCounter.exe --demos <carpeta>   otra carpeta de repeticiones
ChestCounter.exe --data <carpeta>    otra carpeta de datos
ChestCounter.exe --once              analiza las repeticiones terminadas y se cierra
ChestCounter.exe --reset-config      vuelve a hacer la configuración inicial
ChestCounter.exe --overlay-preview <archivo.png>   guarda vistas previas del overlay y se cierra
```

## Compilar desde el código

Instala el [SDK de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) (Windows x64) y haz doble clic en **`build.bat`**: el exe se genera en `dist\ChestCounter.exe` (sin actualizaciones automáticas). GitHub Actions compila el exe en cada push y lo publica como release cuando se sube una etiqueta `v*` (por ejemplo `v2.0.25`); ese exe se actualiza desde las releases de este repositorio.

## Licencia y créditos

[MIT](LICENSE). Basado en [ChestCounter](https://github.com/xdoofy92/ChestCounter) de LED-N (MIT); esta copia apunta las actualizaciones a su propio repositorio y no incluye los enlaces de redes del autor original. El análisis de los replays se basa en [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) de Shiqan (MIT), ligeramente modificado: ver [`lib/PATCHES.md`](lib/PATCHES.md).

Este proyecto no está afiliado a Epic Games, ni cuenta con su respaldo ni patrocinio. Fortnite es una marca registrada de Epic Games, Inc.
