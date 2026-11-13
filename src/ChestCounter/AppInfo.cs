namespace ChestCounter;

/// <summary>Version, GitHub repository and names shared by the tool's processes.</summary>
public static class AppInfo
{
    public static Version Version { get; } = typeof(AppInfo).Assembly.GetName().Version ?? new Version(1, 0, 0);

    public static string VersionText => $"{Version.Major}.{Version.Minor}.{Math.Max(0, Version.Build)}";

    /// <summary>
    /// GitHub repository ("user/repo") whose releases the updater uses, written into the exe when it is built:
    /// GitHub Actions passes its own repository (-p:UpdateRepo=...), so an exe only ever updates itself from the
    /// repository that built it. Null for a local build (build.bat): no update check and no network access at all.
    /// </summary>
    public static string? Repo { get; } = ReadRepo();
    public static string? RepoUrl => Repo == null ? null : "https://github.com/" + Repo;
    public static string? IssuesUrl => RepoUrl == null ? null : RepoUrl + "/issues";

    private static string? ReadRepo()
    {
        foreach (var attribute in typeof(AppInfo).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false))
        {
            if (attribute is System.Reflection.AssemblyMetadataAttribute { Key: "UpdateRepo", Value: { } value }
                && System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9-]{1,39}/[A-Za-z0-9._-]{1,100}$"))
            {
                return value; // only a plain "owner/name" can end up in the update URL
            }
        }
        return null;
    }

    /// <summary>Path of ChestCounter.exe when running as the published exe (null under "dotnet ChestCounter.dll").</summary>
    public static string? ExePath
    {
        get
        {
            var p = Environment.ProcessPath;
            return p != null && Path.GetFileName(p).Equals("ChestCounter.exe", StringComparison.OrdinalIgnoreCase) ? p : null;
        }
    }

    // Names of the per-user kernel objects that let the processes find each other.
    /// <summary>One instance per data folder (the normal folder, or a test folder given with --data).</summary>
    public static string InstanceMutexFor(string dataDir)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(dataDir).TrimEnd('\\', '/').ToLowerInvariant()));
        return @"Local\ChestCounter.Instance." + Convert.ToHexString(hash, 0, 8);
    }
    public const string StandbyMutex = @"Local\ChestCounter.Standby";
    public const string StandbyStopEvent = @"Local\ChestCounter.StandbyStop";
}
