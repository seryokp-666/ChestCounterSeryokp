# Auditoría local de replays

Lee todos los archivos disponibles, con `ParseMode.Full`, y guarda un JSON por replay. No modifica los replays ni el historial. Las posiciones y observaciones se guardan solo en la auditoría, no en `data.json`. Full puede producir dumps del parser en la carpeta de resultados.

```powershell
dotnet run --project tools/ReplayAudit -- "C:/ruta/Demos" "C:/ruta/resultados"
python tools/ReplayAudit/summarize.py "C:/ruta/resultados"
```

El resumen separa la recepción de un estado ya abierto de una transición observada. El máximo observado no es un límite garantizado. El filtro de vida es conservador y requiere posiciones de antigüedad máxima de un segundo. Si se reanuda una ejecución, los JSON existentes se conservan: usar una carpeta nueva al cambiar las reglas de análisis.

Comprobaciones de regresión y capturas, con datos aislados:

```powershell
dotnet run --project tools/ChestCounter.Checks -c Release -- "C:/ruta/verificacion"
```
