# Investigación de replays · 12 de noviembre de 2026

Analizados los 100 archivos disponibles en `C:/Users/Doofy/AppData/Local/FortniteGame/Saved/Demos`, sin modificar los replays ni el historial de la aplicación. 97 partidas con compañeros y 3 en solitario; cero errores de lectura. Son los replays disponibles del 9 al 12 de noviembre: el historial conserva partidas anteriores, pero no contiene sus posiciones ni sustituye los archivos que Fortnite ya borró.

## Distancia de las aperturas de compañeros

El máximo observado que cumple los filtros es **596,5648 m**, en `UnsavedReplay-2026.11.09-20.27.33.replay`, segundo **679,7034** (11:19,7), compañero **Vinimporto**. La posición del grabador tiene 0 s de antigüedad y la del compañero 0,0174 s. Es una distancia calculada con posiciones muestreadas; no representa una medida continua con precisión milimétrica.

Se extrajeron 2.837 eventos de cofres atribuidos a compañeros. 719 cumplen simultáneamente: compañero del mismo equipo; posiciones de ambos anteriores al evento y de antigüedad máxima de 1 s; sin estado de muerte o derribo registrado para ninguno; y contenedor observado antes de su cambio a abierto/destruido. Los estados de muerte se siguen en el tiempo, incluidos reinicios, y se excluyen conservadoramente los intervalos de vida dudosos.

Los cofres que aparecen ya abiertos al entrar en relevancia no prueban dónde estaban ambos jugadores cuando se abrieron. Hay registros recibidos a 1.183,1 m, pero no pasan todos los filtros para atribuirles una apertura en ese instante. No deben usarse como radio de aperturas.

| Replay | Segundo | Distancia 3D | Antigüedad de posición del compañero |
|---|---:|---:|---:|
| UnsavedReplay-2026.11.09-20.27.33.replay | 679.703 | 596.6 m | 0.0174 s |
| UnsavedReplay-2026.11.09-00.44.16.replay | 166.970 | 406.1 m | 0.0083 s |
| UnsavedReplay-2026.11.11-19.13.11.replay | 303.049 | 363.6 m | 0.0508 s |
| UnsavedReplay-2026.11.09-19.38.54.replay | 405.165 | 348.4 m | 0.0356 s |
| UnsavedReplay-2026.11.12-13.41.53.replay | 459.934 | 327.0 m | 0.0750 s |

**Conclusión:** se ha observado registro de aperturas hasta aproximadamente 597 m en estas condiciones. No se ha demostrado que 597 m, 600 m, 1.000 m o 1.087 m sean el límite del juego, ni que todas las aperturas por debajo de una distancia se registren. Un replay del grabador no permite contar las aperturas que faltan. Para acotar un límite máximo hacen falta replays sincronizados del compañero o una prueba controlada con aperturas conocidas a distintas distancias y ambos jugadores vivos. La aplicación no aplica un corte artificial de distancia.

Epic explica la región de replay, pero no publica un radio numérico en su [documentación del sistema de replays](https://www.fortnite.com/news/fortnite-battle-royale-replay-system). La distancia se calcula como `sqrt(dx² + dy² + dz²) / 100` a partir de `ReplicatedMovement.Location`; Unreal usa centímetros por defecto ([unidades de Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/units-of-measurement-in-unreal-engine)).

## Cajas de munición y de frutas

En los replays disponibles hay 554 interacciones propias con cajas de munición y 64 con cajas de comida. Hay dos coincidencias temporales entre un premio y una caja de munición:

- `UnsavedReplay-2026.11.09-22.50.55.replay`, segundo 147,3.
- `UnsavedReplay-2026.11.10-14.02.01.replay`, segundo 177,8.

En ambos casos el origen es **probable**, no está enlazado explícitamente por el servidor. No se encontró un premio de origen confirmado en cajas de comida. Por tanto, estos replays no bastan para demostrar ni descartar que ambas clases den tesoros. [Games.gg](https://games.gg/fortnite/guides/fortnite-all-wanted-poster-locations/) afirma que cajas de munición y de productos pueden dar tokens, pero es una fuente secundaria; no se encontró confirmación oficial específica. No se modificó la clasificación ni las opciones de recuento de munición/comida.

## Carteles Se Busca Espíritus

Se reconocieron **19 cofres propios** `B_SpritePosters_Booty_Chest_C` en la muestra. La categoría de cofres de cartel ya existía. Se amplió el reconocimiento de actores de carteles y se distingue recoger un mapa de abrir el cofre que señala: tomar un cartel por sí solo no se convierte en un premio. Los premios efectivos se reconocen mediante los mismos eventos de premio y concesiones que los demás contenedores.

## Tesoros compartidos y token pirata

El analizador conserva por separado los premios propios, de equipo, de otros jugadores observados y sin atribuir. Las concesiones recibidas en tu estado de jugador permiten conservar premios del equipo aunque no se vea el cofre lejano. Las fuentes inferidas por proximidad temporal se presentan como probables. No se atribuye un premio propio a un enemigo solo por abrir un contenedor al mismo tiempo.

La opción **Sumar tesoros del equipo**, desactivada por defecto, modifica únicamente las vistas y los totales; los eventos guardados se conservan. El informe Tesoros incluye recuentos por ámbito y por premio, además de la tabla de eventos y sus fuentes.

La alerta de token exige un identificador semántico explícito de Pirate Shop/Delilah Shop o un ID con nombre verificado en `treasures.json`. **No hubo tokens piratas identificables en estos 100 replays.** No se ha podido validar la detección con una obtención real del token. Los IDs numéricos desconocidos (`DelilahPrize:dpid_itemN`) se conservan como desconocidos: no se adivina su correspondencia con el token. Una clase exportada o un cofre cercano, por sí solos, no disparan la alerta.

La tarjeta dorada de la ventana principal muestra el token de la partida actual o anterior cuando existe evidencia identificable; permanece independiente de la opción de sumar tesoros del equipo. La cobertura está limitada a lo que contiene el replay, incluida la observación de rivales cerca del grabador; no cubre automáticamente todo lo que sucede alrededor de compañeros alejados.

## Verificación y archivos

- Build Release correcta, sin advertencias ni errores.
- 40 comprobaciones de regresión: filtros BR e historial antiguo, persistencia, reversibilidad de la opción de tesoros, separación de rivales, no duplicación de eventos al ajustar las vistas, IDs desconocidos, clasificación de cajas y cofres de cartel, informe, ventanas redimensionadas y convivencia de equipo/alerta/botones.
- Inspección visual de Ajustes a tamaño normal y 150 %, y tarjeta de token.
- Datos locales completos: `_Release/replay-audit-final/*.json`.
- Tabla reproducible: `_Release/replay-audit-final/observations.csv`.
- Resumen: `_Release/replay-audit-final/summary.json`.
- Capturas y ejemplo de informe: `_Release/verification/`.
- Herramientas reproducibles: `tools/ReplayAudit` y `tools/ChestCounter.Checks`.

Los JSON y logs de la auditoría son locales y no se añaden al repositorio. La auditoría final se ejecutó sobre los 100 replays con los binarios Release verificados, tras los últimos cambios de código.
