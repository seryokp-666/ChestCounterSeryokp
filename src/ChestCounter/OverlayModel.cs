namespace ChestCounter;

/// <summary>Construye lo que muestran la ventana, el overlay y la página OBS a partir de las partidas guardadas y la partida en curso.</summary>
public static class OverlayModel
{
    /// <summary>Texto del contador derecho entre dos partidas (también sirve para mantener el ancho del overlay).</summary>
    public static string LastMatchCaption => L.T("anterior", "previous");

    /// <summary>Icono del contador de tesoros (una gema).</summary>
    public const string TreasureIcon = "💎";

    /// <param name="live">Cifras de la partida en curso, cuando el replay se pudo leer (null si no).</param>
    /// <param name="inProgress">Hay una partida grabándose (aunque sus cifras aún no se puedan leer).</param>
    /// <param name="initialChests">Cofres estimados de antes de usar la herramienta (solo se suman al total).</param>
    public static OverlayView Build(TrackerData d, MatchRecord? live, bool inProgress, int initialChests = 0)
    {
        var liveChests = live?.ChestsOpened ?? 0;
        var liveTreasures = live?.TreasureFinds.Count ?? 0;

        var total = d.Matches.Sum(m => m.ChestsOpened) + liveChests + Math.Max(0, initialChests);
        var session = d.Matches.Where(ReportWriter.IsThisSession).ToList();
        var rows = new List<OverlayRow>();

        // Sesión: cofres, y debajo "n partidas · media x" (partidas terminadas de esta sesión).
        // (el detalle solo lo usa la ventana principal; el overlay muestra la media en su propia columna).
        string? sessionDetail = null;
        var avgText = "—";
        if (session.Count > 0)
        {
            var avg = (double)session.Sum(m => m.ChestsOpened) / session.Count;
            avgText = Math.Round(avg, MidpointRounding.AwayFromZero).ToString("0", L.Culture);
            sessionDetail = $"{L.Count(session.Count, "partida", "partidas", "match", "matches")} · {L.T("media", "avg.")} {avgText}";
        }
        // Solo números en las columnas: la etiqueta ya dice qué son, y un número largo no choca con la columna de al lado.
        rows.Add(new OverlayRow(L.T("Sesión", "Session"), L.N(session.Sum(m => m.ChestsOpened) + liveChests), Detail: sessionDetail));
        rows.Add(new OverlayRow(L.T("Media", "Avg."), avgText));

        var last = d.Matches.OrderByDescending(m => m.MatchStartUtc ?? m.AnalyzedAtUtc).FirstOrDefault();
        // Entre partidas el contador derecho ya muestra la última partida: la fila solo aparece durante una partida.
        if (last != null && (live != null || inProgress))
        {
            rows.Add(new OverlayRow(L.T("Anterior", "Previous"), L.N(last.ChestsOpened)));
        }

        var treasures = d.Matches.Sum(m => m.TreasureFinds.Count) + liveTreasures;
        rows.Add(new OverlayRow(L.T("Tesoros", "Treasures"), $"{TreasureIcon} {L.N(treasures)}", Treasure: true));

        // Contador derecho: la partida en curso si se puede leer, "…" mientras no se pueda, si no la última partida.
        if (live != null)
        {
            return new OverlayView(L.N(total), L.N(live.ChestsOpened), L.T("actual", "current"), true,
                L.T("● EN DIRECTO", "● LIVE"), rows);
        }
        // Sin etiqueta en la cabecera: el contador derecho ya dice "en curso".
        if (inProgress)
        {
            return new OverlayView(L.N(total), "…", L.T("en curso", "in progress"), true, null, rows);
        }
        return new OverlayView(L.N(total), last != null ? L.N(last.ChestsOpened) : null, LastMatchCaption, false, null, rows);
    }
}
