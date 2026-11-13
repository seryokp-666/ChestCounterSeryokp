using System.Text.Json;
using ChestCounter;
Environment.SetEnvironmentVariable("CHESTCOUNTER_MODE", "Full");
var folder = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
Directory.SetCurrentDirectory(output); // Full parsing writes optional parser dumps here, not in the repository root.
foreach (var file in Directory.GetFiles(folder, "*.replay").Order())
{
    var result = Path.Combine(output, Path.GetFileNameWithoutExtension(file) + ".json");
    if (File.Exists(result)) continue;
    try
    {
        using var input = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var analyzer = new ReplayAnalyzer();
        var match = analyzer.Analyze(input, file, null);
        File.WriteAllText(result, JsonSerializer.Serialize(new { Match = match, Observations = analyzer.DistanceObservations }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{Path.GetFileName(file)}: {match.Playlist}, mates={match.Teammates?.Count}, observations={analyzer.DistanceObservations.Count}, valid={analyzer.DistanceObservations.Count(x => x.BothAlive)}");
    }
    catch (Exception e)
    {
        File.WriteAllText(result + ".error", e.ToString());
        Console.WriteLine($"ERROR {Path.GetFileName(file)}: {e.Message}");
    }
    GC.Collect();
}
