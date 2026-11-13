using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// Ventana principal, horizontal 16:9 (592×332): total y partida actual, última partida, gráfico de 7 días,
/// estadísticas en tarjetas y los botones de acción.
/// Todo lo demás (escaneo, overlay, actualizaciones) lo hace <see cref="Monitor"/> en segundo plano.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class MainForm : Form
{
    private readonly Monitor _monitor;
    private readonly Store _store;
    private readonly Card _badgeCard;
    private readonly Label _badge;
    private readonly Label _profileName;
    private readonly ComboBox _profile;
    private readonly Label _total;
    private readonly Label _totalCap;
    private readonly Label _current;
    private readonly Label _currentCap;
    private readonly Label _session;
    private readonly Label _statToday, _statBestDay, _statAverage, _statBestMatch, _statMatches, _statTreasures;
    private readonly Label _statTodayCap, _statBestDayCap, _statAverageCap, _statBestMatchCap, _statMatchesCap, _statTreasuresCap;
    private readonly Card _noticeCard;
    private readonly Label _notice;
    private readonly PillButton _update;
    private readonly PillButton _overlayButton, _reportButton, _settingsButton;
    private readonly DailyChart _daily;
    private readonly Label _lastValue, _lastDetail;
    private readonly Card _teamCard, _pirateCard;
    private readonly Label _pirateLabel;
    private bool _teamShown, _pirateShown;
    private readonly Label _teamTitle, _teamTotal, _teamMessage;
    private readonly Label[] _teamNames = new Label[TeamColumns], _teamValues = new Label[TeamColumns];
    private const int TeamColumns = 4; // tú + hasta 3 compañeros (escuadrón)
    private MonitorState? _last;
    private string? _profileSig;
    private bool _ignoreProfileChange;
    private bool _autoCloseOffer;

    // Disposición: 592×332 (16:9). Columna izquierda (total, última partida, gráfico) y derecha (estadísticas y botones).
    private const int LeftX = 15, LeftW = 267, RightX = 297, RightW = 278;

    public MainForm(Monitor monitor, Store store)
    {
        _monitor = monitor;
        _store = store;

        Text = L.T("Contador de cofres", "Chest Counter") + " v" + AppInfo.VersionText;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(420, 236);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(592, 332);
        Icon = AppIcon.Load();

        // Cabecera: título, perfil y distintivo.
        Lbl(this, L.T("CONTADOR DE COFRES", "CHEST COUNTER"), 8.5f, FontStyle.Bold, Theme.Cyan, new Point(LeftX, 14), new Size(202, 18));
        _badgeCard = new Card { Location = new Point(484, 9), Size = new Size(96, 22), Radius = 11, BackColor = Theme.Panel, Visible = false };
        _badge = Lbl(_badgeCard, "", 8.0f, FontStyle.Bold, Theme.Mint, new Point(0, 0), new Size(96, 22), ContentAlignment.MiddleCenter);
        Controls.Add(_badgeCard);

        _profileName = Lbl(this, "", 8.0f, FontStyle.Regular, Theme.Muted, new Point(230, 11), new Size(240, 18), ContentAlignment.TopRight);
        _profile = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Panel,
            ForeColor = Theme.Ink,
            Font = Theme.Text(),
            Location = new Point(306, 8),
            Size = new Size(164, 24),
            Visible = false,
        };
        _profile.SelectedIndexChanged += (_, _) =>
        {
            if (!_ignoreProfileChange && _profile.SelectedItem is ProfileItem item)
            {
                _monitor.SetProfile(item.Key);
            }
        };
        Controls.Add(_profile);

        // Tarjeta principal: total y partida actual.
        var hero = new Card { Location = new Point(LeftX, 40), Size = new Size(LeftW, 100), Radius = 16 };
        Controls.Add(hero);
        _total = Lbl(hero, "0", 28f, FontStyle.Bold, Theme.Cyan, new Point(15, 6), new Size(120, 50));
        _totalCap = Lbl(hero, L.T("cofres en total", "chests in total"), 8.0f, FontStyle.Regular, Theme.Muted, new Point(16, 59), new Size(120, 16));
        hero.Controls.Add(new Panel { BackColor = Theme.CardEdge, Location = new Point(138, 15), Size = new Size(1, 70) });
        _current = Lbl(hero, "—", 28f, FontStyle.Bold, Theme.Amber, new Point(151, 6), new Size(110, 50));
        _currentCap = Lbl(hero, L.T("última partida", "last match"), 8.0f, FontStyle.Regular, Theme.Muted, new Point(153, 59), new Size(110, 16));
        _session = Lbl(hero, "", 8.0f, FontStyle.Regular, Theme.Ink, new Point(15, 77), new Size(243, 16));

        // Última partida: cofres abiertos, con los detalles (rotos, tesoros, puesto, eliminaciones).
        var last = new Card { Location = new Point(LeftX, 149), Size = new Size(LeftW, 50), Radius = 12 };
        Lbl(last, L.T("Última partida", "Last match"), 7.5f, FontStyle.Regular, Theme.Muted, new Point(13, 6), new Size(101, 13));
        _lastDetail = Lbl(last, "", 7.5f, FontStyle.Regular, Theme.Muted, new Point(101, 5), new Size(153, 15), ContentAlignment.MiddleRight);
        _lastValue = Lbl(last, "—", 11.5f, FontStyle.Bold, Theme.Amber, new Point(13, 22), new Size(243, 22));
        Controls.Add(last);

        // Gráfico de los últimos 7 días.
        var daily = new Card { Location = new Point(LeftX, 206), Size = new Size(LeftW, 120), Radius = 14 };
        Lbl(daily, L.T("Últimos 7 días", "Last 7 days"), 8.0f, FontStyle.Regular, Theme.Muted, new Point(13, 7), new Size(184, 13));
        _daily = new DailyChart { Location = new Point(9, 24), Size = new Size(246, 90), BackColor = Theme.Card };
        daily.Controls.Add(_daily);
        Controls.Add(daily);

        _pirateCard = new Card { Location = new Point(LeftX, 334), Size = new Size(RightX + RightW - LeftX, 42), Radius = 14, BackColor = Theme.Panel, Visible = false };
        _pirateLabel = new Label
        {
            Bounds = new Rectangle(13, 0, _pirateCard.Width - 26, 42), Font = Theme.Text(8.5f, FontStyle.Bold),
            ForeColor = Theme.Amber, BackColor = _pirateCard.BackColor, TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
        };
        _pirateCard.Controls.Add(_pirateLabel);
        _pirateCard.Cursor = _pirateLabel.Cursor = Cursors.Hand;
        _pirateCard.Click += (_, _) => Open(new Uri(_store.ReportPath).AbsoluteUri + "#tesoros");
        _pirateLabel.Click += (_, _) => Open(new Uri(_store.ReportPath).AbsoluteUri + "#tesoros");
        Controls.Add(_pirateCard);

        // Equipo (con «Compañeros» activado): tus cofres y los vistos de cada compañero en la partida actual o la última.
        // Solo se ve con la opción activada; entonces la ventana crece para hacerle sitio.
        _teamCard = new Card { Location = new Point(LeftX, 334), Size = new Size(RightX + RightW - LeftX, 74), Radius = 14, Visible = false };
        _teamTitle = Lbl(_teamCard, L.T("Equipo", "Team"), 7.5f, FontStyle.Regular, Theme.Muted, new Point(13, 7), new Size(250, 14));
        _teamTotal = Lbl(_teamCard, "", 8.0f, FontStyle.Bold, Theme.Ink, new Point(_teamCard.Width - 13 - 300, 6), new Size(300, 15), ContentAlignment.TopRight);
        var colW = (_teamCard.Width - 26) / TeamColumns;
        for (var i = 0; i < TeamColumns; i++)
        {
            _teamNames[i] = Lbl(_teamCard, "", 7.5f, FontStyle.Regular, Theme.Muted, new Point(13 + i * colW, 28), new Size(colW - 10, 14));
            _teamValues[i] = Lbl(_teamCard, "", 14f, FontStyle.Bold, i == 0 ? Theme.Amber : Theme.Ink, new Point(12 + i * colW, 42), new Size(colW - 10, 26));
        }
        _teamMessage = Lbl(_teamCard, "", 8.0f, FontStyle.Regular, Theme.Muted, new Point(13 + colW, 28), new Size(_teamCard.Width - 26 - colW, 40), ContentAlignment.MiddleLeft);
        Controls.Add(_teamCard);

        // Rejilla de estadísticas: dos columnas, tres filas (columna derecha).
        _statToday = Stat(L.T("Hoy", "Today"), RightX, 40, Theme.Cyan, out _statTodayCap);
        _statBestDay = Stat(L.T("Mejor día", "Best day"), RightX + 142, 40, Theme.Amber, out _statBestDayCap);
        _statAverage = Stat(L.T("Media por partida", "Average per match"), RightX, 117, Theme.Ink, out _statAverageCap);
        _statBestMatch = Stat(L.T("Mejor partida", "Best match"), RightX + 142, 117, Theme.Ink, out _statBestMatchCap);
        _statMatches = Stat(L.T("Histórico", "History"), RightX, 196, Theme.Ink, out _statMatchesCap);
        _statTreasures = Stat(L.T("Tesoros", "Treasures"), RightX + 142, 196, Theme.Ink, out _statTreasuresCap);

        // Aviso (versión nueva, cierre con Fortnite) o botón de actualizar: ocupa el sitio de los botones.
        _noticeCard = new Card { Location = new Point(RightX, 277), Size = new Size(RightW, 40), Radius = 12, BackColor = Theme.Card, Visible = false };
        _notice = Lbl(_noticeCard, "", 8.5f, FontStyle.Regular, Theme.AccentSoft, new Point(13, 0), new Size(RightW - 26, 40), ContentAlignment.MiddleLeft);
        _noticeCard.Click += (_, _) => { if (_autoCloseOffer) _monitor.KeepOpen(); };
        _notice.Click += (_, _) => { if (_autoCloseOffer) _monitor.KeepOpen(); };
        Controls.Add(_noticeCard);

        _update = new PillButton { Location = new Point(RightX, 277), Size = new Size(RightW, 40), Radius = 12, Visible = false, BackColor = Theme.Accent, HoverColor = Theme.AccentHover, ForeColor = Theme.AccentInk };
        _update.Click += (_, _) => _monitor.InstallUpdate();
        Controls.Add(_update);

        // Botones de acción.
        _overlayButton = Action(L.T("Overlay", "Overlay"), RightX);
        _overlayButton.Click += (_, _) => _monitor.ToggleOverlay();
        _reportButton = Action(L.T("Informe", "Report"), RightX + 95);
        _reportButton.Click += (_, _) => Open(_store.ReportPath);
        _settingsButton = Action(L.T("Ajustes", "Settings"), RightX + 190);
        _settingsButton.Click += (_, _) =>
        {
            using var dialog = new SettingsForm(_monitor, _store);
            dialog.ShowDialog(this);
        };

        // Diseño base (592×332): al redimensionar o maximizar, todos los controles y fuentes escalan proporcionalmente.
        CaptureLayout();
        _scaleTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _scaleTimer.Tick += (_, _) => { _scaleTimer.Stop(); ApplyLayoutScale(); };

        monitor.Changed += OnChanged;
        Shown += (_, _) =>
        {
            if (_monitor.AutoMode)
            {
                WindowState = FormWindowState.Minimized; // abierto con Fortnite: el juego conserva la pantalla
            }
        };
    }

    private const int DesignW = 592, DesignHBase = 332, DesignHTeam = 416;
    private int _designH = DesignHBase;
    private readonly Dictionary<Control, LayoutEntry> _layout = new();
    private System.Windows.Forms.Timer? _scaleTimer;
    private float _lastFx = 1, _lastFy = 1;

    private sealed class LayoutEntry
    {
        public Point Location;
        public Size Size;
        public Font? Font;
        public Font? Scaled;
    }

    private void CaptureLayout()
    {
        Capture(this);
        void Capture(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                _layout[c] = new LayoutEntry { Location = c.Location, Size = c.Size, Font = c.Font };
                Capture(c);
            }
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            return;
        }
        _scaleTimer?.Stop();
        _scaleTimer?.Start();
    }

    /// <summary>Escala posiciones, tamaños y fuentes desde el diseño base. Las fuentes van en puntos: se dividen por el DPI para no contarlas dos veces.</summary>
    private void ApplyLayoutScale()
    {
        if (_layout.Count == 0 || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }
        var fx = ClientSize.Width / (float)DesignW;
        var fy = ClientSize.Height / (float)_designH;
        if (Math.Abs(fx - _lastFx) < 0.005 && Math.Abs(fy - _lastFy) < 0.005)
        {
            return;
        }
        _lastFx = fx;
        _lastFy = fy;
        // Durante restaurar/maximizar el tamaño puede ser transitorio: sin factor válido no se tocan las fuentes.
        var k = Math.Min(fx / (DeviceDpi / 96f), fy / (DeviceDpi / 96f));
        var scaleFonts = float.IsFinite(k) && k > 0.2 && k < 10;
        foreach (var (c, l) in _layout)
        {
            c.Location = new Point((int)Math.Round(l.Location.X * fx), (int)Math.Round(l.Location.Y * fy));
            c.Size = new Size((int)Math.Round(l.Size.Width * fx), (int)Math.Round(l.Size.Height * fy));
            if (scaleFonts && l.Font != null)
            {
                var nf = new Font(l.Font.FontFamily, l.Font.SizeInPoints * k, l.Font.Style);
                l.Scaled?.Dispose();
                l.Scaled = nf;
                c.Font = nf;
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle); // esquinas redondeadas de Windows 11 (el sistema las dibuja sin tocar botones ni bordes)
        if (_last != null)
        {
            Render(_last);
        }
    }

    private void OnChanged(MonitorState state)
    {
        _last = state;
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }
        try
        {
            BeginInvoke(new Action(() => Render(state)));
        }
        catch (ObjectDisposedException)
        {
            // la ventana se está cerrando
        }
        catch (InvalidOperationException)
        {
            // todavía sin ventana
        }
    }

    private void Render(MonitorState s)
    {
        var v = s.View;
        _total.Text = v.Total;
        _current.Text = v.Current ?? "—";
        _current.ForeColor = v.CurrentLive ? Theme.Mint : v.Current == null ? Theme.Muted : Theme.Amber;
        // El overlay usa textos cortos ("anterior" / "actual"); aquí hay sitio para los largos.
        _currentCap.Text = v.CurrentCaption == OverlayModel.LastMatchCaption ? L.T("última partida", "last match")
            : v.CurrentLive && v.Current != "…" ? L.T("partida actual", "current match")
            : v.CurrentCaption;
        _badge.Text = v.Badge ?? "";
        _badgeCard.Visible = !string.IsNullOrEmpty(v.Badge);

        // Línea de sesión: la primera fila del modelo (cofres y, detrás, partidas y media).
        var session = v.Rows.FirstOrDefault();
        var sessionChests = session != null && int.TryParse(session.Value, System.Globalization.NumberStyles.None, L.Culture, out var n)
            ? L.Count(n, "cofre", "cofres", "chest", "chests")
            : session?.Value;
        _session.Text = session == null ? "" : $"{L.T("Sesión", "Session")}: {sessionChests}" + (session.Detail != null ? $"  ·  {session.Detail}" : "");

        var st = s.Stats;
        _statToday.Text = L.N(st.Today);
        // La fecha va en la línea de arriba: "1.409 · 06/11" a 15 pt no cabe en la tarjeta y se cortaba.
        _statBestDay.Text = st.BestDay is { } bestDay ? L.N(st.BestDayChests) : "—";
        _statBestDayCap.Text = st.BestDay is { } bd
            ? $"{L.T("Mejor día", "Best day")} · {bd:dd/MM}"
            : L.T("Mejor día", "Best day");
        _statAverage.Text = st.Matches > 0 ? st.Average.ToString("0.0", L.Culture) : "—";
        _statBestMatch.Text = st.Matches > 0 ? L.N(st.BestMatch) : "—";
        _statMatches.Text = L.Count(st.HistoryMatches, "partida", "partidas", "match", "matches");
        _statMatchesCap.Text = st.HistorySince is { } since
            ? L.F("Desde {0:dd/MM/yy}", "Since {0:dd/MM/yy}", since)
            : L.T("Histórico", "History");
        _statTreasures.Text = $"{OverlayModel.TreasureIcon} {L.N(st.Treasures)}";
        _overlayButton.Enabled = _monitor.OverlayEnabled;
        _overlayButton.Indicator = _monitor.OverlayVisible ? Theme.Green : Theme.Red;

        if (st.LastMatch is { } lm)
        {
            _lastValue.Text = L.Count(lm.Chests, "cofre abierto", "cofres abiertos", "chest opened", "chests opened");
            var parts = new List<string>();
            if (lm.Destroyed > 0) parts.Add(L.F("{0} roto(s)", "{0} destroyed", lm.Destroyed));
            if (lm.Treasures > 0) parts.Add(L.Count(lm.Treasures, "tesoro", "tesoros", "treasure", "treasures"));
            if (lm.Placement is { } place) parts.Add(lm.Players is { } players ? $"{L.Ordinal(place)} {L.T("de", "of")} {players}" : L.Ordinal(place));
            if (lm.Eliminations is { } elim) parts.Add(L.F("{0} elim.", "{0} elim.", elim));
            _lastDetail.Text = lm.PirateToken
                ? L.T("★ Token pirata", "★ Pirate token")
                : string.Join("  ·  ", parts);
            _lastDetail.ForeColor = lm.PirateToken ? Theme.Amber : Theme.Muted;
            _lastValue.ForeColor = Theme.Amber;
        }
        else
        {
            _lastValue.Text = "—";
            _lastDetail.Text = "";
        }
        _daily.SetDays(st.LastDays);
        var pirateAlert = s.PirateTokenLive || st.LastMatch?.PirateToken == true;
        _pirateLabel.Text = pirateAlert
            ? (s.PirateTokenLive ? L.T("★ TOKEN PIRATA · Detectado en esta partida", "★ PIRATE TOKEN · Detected in this match")
                : L.T("★ TOKEN PIRATA · Detectado en la partida anterior", "★ PIRATE TOKEN · Detected in the previous match"))
                + "\n" + (s.TokenDetails ?? L.T("Ver detalles en Tesoros →", "View details in Treasures →"))
            : L.T("TOKEN DE TIENDA PIRATA · Sin detecciones\nTu replay y equipo · Ver Tesoros →", "PIRATE SHOP TOKEN · No detections\nYour replay and team · View Treasures →");
        _pirateLabel.ForeColor = pirateAlert ? Theme.Amber : Theme.Muted;
        SetBottomPanels(s.Team != null, true);
        RenderTeam(s.Team);

        // Perfil: nombre fijo con una sola cuenta, selector con varias.
        var signature = string.Join("|", s.Profiles.Select(p => p.Key));
        var several = s.Profiles.Count >= 2;
        _profile.Visible = several;
        _profileName.Visible = !several;
        _profileName.Text = s.Profiles.FirstOrDefault(p => p.Key == s.CurrentKey) is { } current
            ? $"{L.T("Perfil", "Profile")}: {current.Name}"
            : "";
        if (signature != _profileSig)
        {
            _profileSig = signature;
            _ignoreProfileChange = true;
            _profile.Items.Clear();
            foreach (var p in s.Profiles)
            {
                _profile.Items.Add(new ProfileItem(p.Key, Profiles.Describe(p)));
            }
            _ignoreProfileChange = false;
        }
        _ignoreProfileChange = true;
        for (var i = 0; i < _profile.Items.Count; i++)
        {
            if (_profile.Items[i] is ProfileItem item && item.Key == s.CurrentKey)
            {
                _profile.SelectedIndex = i;
                break;
            }
        }
        _ignoreProfileChange = false;

        // Aviso o actualización: uno u otro, en el mismo sitio.
        _autoCloseOffer = s.AutoCloseOffer;
        _update.Visible = s.Update != null;
        if (s.Update != null)
        {
            _update.Text = L.F("Instalar la versión {0} (tienes la {1})", "Install version {0} (you have {1})",
                s.Update.Version.ToString(3), AppInfo.VersionText);
        }
        _notice.Text = s.Notice ?? "";
        _notice.Cursor = s.AutoCloseOffer ? Cursors.Hand : Cursors.Default;
        _noticeCard.Visible = s.Update == null && !string.IsNullOrEmpty(s.Notice);

        // Los botones ocupan el mismo sitio que el aviso o la actualización: cuando hay aviso, se ocultan.
        var notice = _update.Visible || _noticeCard.Visible;
        _overlayButton.Visible = _reportButton.Visible = _settingsButton.Visible = !notice;
    }

    /// <summary>
    /// Tarjeta «Equipo»: tus cofres y los vistos de cada compañero (un mínimo: el replay solo tiene lo que pasa cerca
    /// de ti). Sin datos de equipo (opción desactivada) la tarjeta se oculta y la ventana vuelve a su tamaño.
    /// </summary>
    private void RenderTeam(TeamSummary? team)
    {
        if (team == null)
        {
            return;
        }
        _teamTitle.Text = team.Live ? L.T("Equipo · partida actual", "Team · current match") : L.T("Equipo · última partida", "Team · last match");
        _teamTitle.ForeColor = team.Live ? Theme.Mint : Theme.Muted;
        _teamNames[0].Text = L.T("Tú", "You");
        _teamValues[0].Text = L.N(team.Mine);

        var message = team.Unknown
            ? L.T("Partida analizada antes de guardar compañeros.", "Match analyzed before teammates were saved.")
            : team.Mates.Count == 0 ? L.T("Partida en solitario: sin compañeros.", "Solo match: no teammates.") : null;
        _teamMessage.Text = message ?? "";
        _teamMessage.Visible = message != null;
        _teamTotal.Text = team.Mates.Count == 0 ? ""
            : team.Total > 0
                ? L.F("Equipo ≥ {0} · compañeros ≥ {1} %", "Team ≥ {0} · teammates ≥ {1} %", L.N(team.Total), (100.0 * team.MatesTotal / team.Total).ToString("0", L.Culture))
                : L.F("Equipo ≥ {0}", "Team ≥ {0}", L.N(team.Total));
        for (var i = 1; i < TeamColumns; i++)
        {
            var mate = message == null && i - 1 < team.Mates.Count ? team.Mates[i - 1] : null;
            _teamNames[i].Text = mate?.Name ?? "";
            _teamValues[i].Text = mate != null ? "≥ " + L.N(mate.Chests) : "";
            _teamNames[i].Visible = _teamValues[i].Visible = message == null;
        }
    }

    /// <summary>Muestra u oculta la tarjeta «Equipo»; la ventana crece o encoge con ella, a la misma escala.</summary>
    private void SetBottomPanels(bool team, bool pirate)
    {
        if (_teamShown == team && _pirateShown == pirate) return;
        var scale = ClientSize.Height / (float)_designH;
        _teamShown = team;
        _pirateShown = pirate;
        _teamCard.Visible = team;
        _pirateCard.Visible = pirate;
        _layout[_teamCard].Location = new Point(LeftX, pirate ? 384 : 334);
        _designH = DesignHBase + (team ? DesignHTeam - DesignHBase : 0) + (pirate ? 50 : 0);
        _lastFx = _lastFy = -1;
        if (WindowState == FormWindowState.Normal)
            ClientSize = new Size(ClientSize.Width, (int)Math.Round(_designH * scale));
        ApplyLayoutScale();
    }

    /// <summary>Tarjeta de estadística: título arriba y cifra debajo. Devuelve la etiqueta de la cifra.</summary>
    private Label Stat(string caption, int x, int y, Color valueColor, out Label captionLabel)
    {
        var card = new Card { Location = new Point(x, y), Size = new Size(136, 70), Radius = 14 };
        Controls.Add(card);
        captionLabel = Lbl(card, caption, 8.0f, FontStyle.Regular, Theme.Muted, new Point(12, 9), new Size(113, 15));
        return Lbl(card, "—", 15f, FontStyle.Bold, valueColor, new Point(12, 31), new Size(117, 28));
    }

    private PillButton Action(string text, int x)
    {
        var b = new PillButton { Text = text, Location = new Point(x, 277), Size = new Size(88, 40), Radius = 11 };
        Controls.Add(b);
        return b;
    }

    private static Label Lbl(Control parent, string text, float size, FontStyle style, Color color, Point at, Size sz, ContentAlignment align = ContentAlignment.TopLeft)
    {
        var l = new FitLabel
        {
            Text = text,
            Font = Theme.Text(size, style),
            ForeColor = color,
            BackColor = parent.BackColor,
            Location = at,
            Size = sz,
            TextAlign = align,
        };
        parent.Controls.Add(l);
        return l;
    }

    private static void Open(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // el informe aún no existe o no hay navegador: nada que hacer
        }
    }

    private sealed record ProfileItem(string Key, string Text)
    {
        public override string ToString() => Text;
    }
}
