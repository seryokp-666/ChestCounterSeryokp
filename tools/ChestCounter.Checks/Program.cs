using System.Drawing;
using System.Reflection;
using System.Text.Json;
using ChestCounter;
using System.Windows.Forms;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); _checks++; }
    [STAThread]
    private static void Main(string[] args)
    {
        var output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        foreach (var p in new[] { "Playlist_DefaultSolo", "Playlist_DefaultSquad", "Playlist_NoBuildBR_Trio", "Playlist_Trios", "Playlist_HabaneroBR_Solo" })
            Check(Profiles.IsBattleRoyalePlaylist(p), "BR rejected: " + p);
        foreach (var p in new string?[] { null, "", "Playlist_Creative", "Playlist_BlastBerry_Squad", "Playlist_Reload_Solo", "Playlist_DefaultSquad_LTM", "Playlist_Delulu", "Playlist_Festival" })
            Check(!Profiles.IsBattleRoyalePlaylist(p), "Other mode accepted: " + p);
        var own = new TreasureFind { Item = "Own reward", RawItem = "own" };
        var team = new TreasureFind { Item = "Team reward", Scope = "team", Finder = "Mate", RawItem = "team" };
        var nearby = new TreasureFind { Item = "Pirate Shop Access", Scope = "nearby", Finder = "Opponent", RawItem = "PirateShopAccess", Evidence = "recipient-handler" };
        var match = new MatchRecord { Playlist = "Playlist_NoBuildBR_Squad", GameVersion = "++Fortnite+Release-43.10", PlayerName = "Test", EpicId = "test", ChestsOpened = 3,
            TreasureFinds = new() { own }, NearbyTreasureFinds = new() { team, nearby }, Teammates = new() { new() { Name = "Mate", ChestsSeen = 4 } } };
        var config = new Config();
        Check(Profiles.Adjust(match, config)!.TreasureFinds.Count == 1, "Team treasures default off");
        config.CountTeammateTreasures = true;
        var adjusted = Profiles.Adjust(match, config)!;
        Check(adjusted.TreasureFinds.Count == 2, "Shared treasures missing");
        Check(adjusted.ChestsOpened == 3, "Independent chest setting changed");
        Check(adjusted.AllTreasureFinds.Count() == 3, "Report duplicates shared treasures");
        Check(adjusted.NearbyTreasureFinds.Count == 1, "Nearby enemy included in own total");
        Check(match.TreasureFinds.Count == 1 && match.NearbyTreasureFinds.Count == 2, "Stored data changed");
        config.CountTeammateChests = true;
        Check(Profiles.Adjust(match, config)!.ChestsOpened == 7, "Squad chest regression");
        config.CountTeammateTreasures = false;
        Check(Profiles.Adjust(match, config)!.TreasureFinds.Count == 1, "Toggle not reversible");
        Check(!new TreasureFind { RawItem = "WID_PirateShopAccess", Evidence = "handler-export" }.IsPirateToken, "Class export mistaken for an obtained token");
        Check(new TreasureFind { RawItem = "WID_PirateShopAccess", Evidence = "recipient-handler" }.IsPirateToken, "Confirmed token receipt ignored");
        Check(match.PirateTokenSeen, "Enemy token not noticed");
        Check(!TreasureNames.IsPirateToken("DelilahPrize:dpid_item16"), "Guessed numeric prize");
        Check(!TreasureNames.IsPirateToken("WID_SmugglerExtractionKey"), "Extraction key mistaken for Pirate token");
        var json = JsonSerializer.Serialize(match);
        var loaded = JsonSerializer.Deserialize<MatchRecord>(json)!;
        Check(loaded.PirateTokenSeen && loaded.AllTreasureFinds.Count() == 3, "Persistence lost rewards");
        var other = new MatchRecord { Playlist = "Playlist_Creative", GameVersion = match.GameVersion, ChestsOpened = 999, EpicId = "test" };
        var data = new TrackerData { Matches = new() { match, other } };
        Check(Profiles.View(data, config).Matches.Count == 1, "Legacy other mode leaks into stats");
        Check(!ReportWriter.Csv(data).Contains("999"), "Other mode leaks into CSV");
        Check(ChestKinds.Classify("B_SpritePosters_Booty_Chest_C") == ChestKinds.PosterChest, "Poster chest not recognized");
        Check(!ChestKinds.IsChest(ChestKinds.Classify("B_SpritePosters_Poster_C")), "Map collection counted as a chest");
        Check(ChestKinds.Classify("Tiered_Ammo_Athena_C") == ChestKinds.AmmoBox, "Ammo classification changed");
        Check(ChestKinds.Classify("FoodBox_Large_C") == ChestKinds.FoodBox, "Food classification changed");
        config.CountTeammateTreasures = true;
        var html = ReportWriter.Report(config, Profiles.View(data, config), Profiles.List(data, config));
        Check(html.Contains("Opponent") && html.Contains("Mate") && html.Contains("Pirate Shop Access"), "Treasure report lost scopes");
        Check(html.Contains("TOKEN", StringComparison.OrdinalIgnoreCase), "Token alert missing in report");
        File.WriteAllText(Path.Combine(output, "report-test.html"), html);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        var store = new Store(Path.Combine(output, "isolated-data"));
        var tracker = new Tracker(store, config, data);
        var monitor = new ChestCounter.Monitor(store, config, data, tracker, store.Folder);
        using (var settings = new SettingsForm(monitor, store))
        {
            settings.StartPosition = FormStartPosition.Manual;
            settings.Location = new Point(-30000, -30000);
            settings.Show();
            Check(settings.FormBorderStyle == FormBorderStyle.Sizable && settings.MaximizeBox, "Settings cannot resize");
            var baseSize = settings.ClientSize;
            settings.ClientSize = new Size(baseSize.Width * 3 / 2, baseSize.Height * 3 / 2);
            Check(settings.Controls.Cast<Control>().Where(c => c.Visible).All(c => c.Right <= settings.ClientSize.Width + 2 && c.Bottom <= settings.ClientSize.Height + 2), "Resized settings overflow");
            using var bitmap = new Bitmap(settings.Width, settings.Height);
            settings.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(output, "settings-resized.png"));
            settings.ClientSize = baseSize;
            using var normal = new Bitmap(settings.Width, settings.Height);
            settings.DrawToBitmap(normal, new Rectangle(Point.Empty, normal.Size));
            normal.Save(Path.Combine(output, "settings-normal.png"));
        }
        using (var main = new MainForm(monitor, store))
        {
            main.ShowInTaskbar = false;
            main.StartPosition = FormStartPosition.Manual;
            main.Location = new Point(-30000, -30000);
            main.Show();
            var stats = ChestStats.Empty with { LastMatch = new(3, 0, 1, 1, 80, 2, true) };
            var state = new MonitorState(OverlayModel.Build(Profiles.View(data, config), null, false), Profiles.List(data, config), "test", null, null, false, stats);
            typeof(MainForm).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(main, new object[] { state });
            var buttons = main.Controls.Cast<Control>().Where(c => c is PillButton && (c.Text == "Ajustes" || c.Text == "Settings" || c.Text == "Informe" || c.Text == "Report")).ToList();
            Check(buttons.Count == 2 && buttons.All(c => c.Visible), "Token banner hides action buttons");
            using var bitmap = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(output, "main-token.png"));
            var teamState = state with { Team = new TeamSummary(false, 3, new[] { new TeamMember("Mate", 4) }, false) };
            typeof(MainForm).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(main, new object[] { teamState });
            Check(main.ClientSize.Height > 430 && buttons.All(c => c.Visible), "Team and token panels overlap actions");
            using var combined = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(combined, new Rectangle(Point.Empty, combined.Size));
            combined.Save(Path.Combine(output, "main-token-team.png"));
            var noToken = state with { Stats = ChestStats.Empty };
            typeof(MainForm).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(main, new object[] { noToken });
            Check(main.ClientSize.Height == 382, "Persistent token status is missing");
            using var emptyToken = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(emptyToken, new Rectangle(Point.Empty, emptyToken.Size));
            emptyToken.Save(Path.Combine(output, "main-token-empty.png"));
        }
        Console.WriteLine($"PASS: {_checks} regression checks; UI images and report saved in {output}");
    }
}
