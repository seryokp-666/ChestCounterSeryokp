using System.Text.RegularExpressions;
using FortniteReplayReader;
using FortniteReplayReader.Models;
using FortniteReplayReader.Models.NetFieldExports;
using FortniteReplayReader.Models.NetFieldExports.Items;
using FortniteReplayReader.Models.NetFieldExports.Weapons;
using Unreal.Core;
using Unreal.Core.Contracts;
using Unreal.Core.Models;
using Unreal.Core.Models.Enums;

namespace ChestCounter;

/// <summary>
/// Reads one .replay file and extracts every container searched by the tracked player,
/// together with the items that were tossed out of it.
/// </summary>
public sealed class ReplayAnalyzer : FortniteReplayReader.ReplayReader
{
    private sealed record SearchEvent(float Time, string ActorKey, string Label, uint? SearcherPawn, bool Destroyed = false, bool ObservedTransition = false);
    private sealed record TossEvent(float Time, string Item);
    private sealed record EquipEvent(float Time, string Item, uint Pawn);
    private sealed record PrizeEvent(float Time, string PrizeId, uint? Pawn, string Container, bool? Success = null);

    private static readonly Regex PrizeIdPattern = new(@"[A-Za-z][A-Za-z0-9_]*:[A-Za-z0-9_.\-]+", RegexOptions.Compiled);

    // Treasure hunt items do not drop on the ground: they go straight into the inventory.
    // They only show up in the replay when the player equips them (a weapon/gizmo actor is spawned).
    private static readonly Regex TreasureActor = new(
        @"FlameCeleb|RelicExtractor|RelicFinder|LuckyLocator|SmugglerExtractionKey|ExtractionAccelerat|PirateShop|DelilahShop",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UaidSuffix = new(@"_UAID_.*$", RegexOptions.Compiled);

    // Items that are part of the treasure hunt "Standard" prize pool, or anything that looks like a treasure.
    private static readonly Regex TreasureItem = new(
        @"RelicExtractor|RelicFinder|LuckyLocator|FlameCeleb|SmugglerExtractionKey|ExtractionAccelerat|Treasure|PirateShop|DelilahShop",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static ReplayAnalyzer()
    {
        // Map every chest / container blueprint we do not know yet onto the generic container group.
        NetFieldParser.AliasResolver = path =>
        {
            if (path.Contains(':') || path.EndsWith("_ClassNetCache", StringComparison.Ordinal))
            {
                return null;
            }

            var last = path[(path.LastIndexOf('/') + 1)..];
            if (last.StartsWith("B_", StringComparison.Ordinal) && TreasureActor.IsMatch(last))
            {
                return GenericWeapon.GroupPath;
            }

            return path.Contains("/Containers/", StringComparison.OrdinalIgnoreCase)
                   || last.Contains("Chest", StringComparison.OrdinalIgnoreCase)
                || last.Contains("SpritePoster", StringComparison.OrdinalIgnoreCase)
                ? GenericContainer.GroupPath
                : null;
        };

        // Loot cannons: every cannon blueprint exposes the FireLootCannon RPC through its own ClassNetCache.
        NetFieldParser.ClassNetCacheAliasResolver = path =>
            path.Contains("LootCannon", StringComparison.OrdinalIgnoreCase) && path.EndsWith("_ClassNetCache", StringComparison.Ordinal)
                ? LootCannonCache.Path
                : null;
    }

    private sealed record PawnSample(float Time, FVector Position, bool Alive);
    private readonly Dictionary<uint, List<PawnSample>> _pawnSamples = new();
    private readonly Dictionary<uint, List<(float Time, bool Alive)>> _pawnLife = new();
    private readonly Dictionary<uint, List<(float Time, bool Alive)>> _stateLife = new();
    private readonly Dictionary<uint, uint> _stateReboots = new();
    private readonly Dictionary<uint, float> _pawnDeathTimes = new();
    public List<ReplayDistanceObservation> DistanceObservations { get; } = new();
    private readonly Dictionary<uint, string> _channelKey = new();
    private readonly Dictionary<uint, string> _channelLabel = new();
    private readonly Dictionary<uint, uint> _lastSearcher = new();
    private readonly HashSet<uint> _containersObserved = new();
    private readonly HashSet<string> _searched = new();
    private readonly List<SearchEvent> _searches = new();
    private readonly List<TossEvent> _tosses = new();
    private readonly List<EquipEvent> _equips = new();
    private readonly List<PrizeEvent> _prizes = new();
    private readonly Dictionary<string, float> _firstExport = new();
    private static readonly Regex TrackedExport = new(@"TreasureHandler|WID_(FlameCeleb|RelicExtractor|RelicFinder|LuckyLocator|SmugglerExtractionKey|PirateShop|DelilahShop)", RegexOptions.Compiled);
    private readonly Dictionary<uint, int> _prizeChannelsSeen = new();
    private readonly HashSet<uint> _equipChannelsSeen = new();
    private readonly Dictionary<uint, uint> _pawnToPlayerState = new();
    private readonly Dictionary<uint, int> _playerStateToPlayerId = new();

    // Treasure prize handler abilities ("GA_TreasureHandler_*"): class netguid -> name.
    private readonly Dictionary<uint, string> _treasureHandlerGuids = new();

    /// <summary>One treasure prize handler granted to a player (one per treasure obtained).</summary>
    private sealed record GrantEvent(float Time, uint PlayerStateActor, int ElementId, string Handler);
    private readonly List<GrantEvent> _grants = new();

    /// <summary>Bump when the analysis changes so that existing replays are re-analyzed.</summary>
    public const int CurrentVersion = 16;

    private static readonly bool ProbeDebug = Environment.GetEnvironmentVariable("CHESTCOUNTER_DEBUG") == "4";

    public ReplayAnalyzer() : base(Environment.GetEnvironmentVariable("CHESTCOUNTER_DEBUG") == "2" ? new StderrLogger() : null,
        Enum.TryParse<ParseMode>(Environment.GetEnvironmentVariable("CHESTCOUNTER_MODE"), out var mode) ? mode : ParseMode.Normal)
    {
        // The ability system component of the player state is not decoded by the library (its fields have no
        // exported names): its raw FastArray payloads are read here to find every treasure handler granted.
        Regex? debugFilter = null;
        if (ProbeDebug)
        {
            debugFilter = new Regex(Environment.GetEnvironmentVariable("CHESTCOUNTER_PROBE") is { Length: > 0 } p
                ? p : "Treasure|Prize|Magpie|Inventory|Quest|Reward|Hunt|PlayerController|PlayerState|Notif", RegexOptions.IgnoreCase);
            NetGuidCache.KeepOnCleanup = true;
        }
        RawProbe.Filter = path => path.Contains("FortAbilitySystemComponent", StringComparison.Ordinal) || debugFilter?.IsMatch(path) == true;
        RawProbe.Sink = (channel, group, field, data, bits) =>
        {
            if (group.EndsWith("AbilitySystemComponent_ClassNetCache", StringComparison.Ordinal) && _treasureHandlerGuids.Count > 0)
            {
                ReadAbilityGrants(channel, data, bits);
            }
            if (debugFilter?.IsMatch(group + "." + field) == true)
            {
                ProbeSink(channel, group, field, data, bits);
            }
        };
    }

    /// <summary>
    /// Decodes a FastArray delta (ActivatableAbilities) without knowing the field names: header, then for each
    /// changed element its id and its properties (handle, bit count, payload). An element whose object reference
    /// is a treasure handler class means a treasure was given to the owner of this ability system.
    /// </summary>
    private void ReadAbilityGrants(uint channel, byte[] data, int bits)
    {
        var pos = 1; // bSupportsFastArrayDeltaStructSerialization
        int Bit() => pos < bits ? (data[pos >> 3] >> (pos & 7)) & 1 : throw new IndexOutOfRangeException();
        uint Read(int n) { uint v = 0; for (var k = 0; k < n; k++) { v |= (uint)Bit() << k; pos++; } return v; }
        uint ReadPacked()
        {
            uint v = 0;
            for (var shift = 0; shift < 35; shift += 7)
            {
                var b = Read(8);
                v |= (b >> 1) << shift;
                if ((b & 1) == 0) return v;
            }
            throw new InvalidDataException();
        }

        try
        {
            _ = Read(32); _ = Read(32); // replication keys
            var deletes = Read(32);
            var changed = Read(32);
            if (deletes > 512 || changed > 512 || bits < 129)
            {
                return;
            }
            pos += (int)deletes * 32;
            for (var i = 0; i < changed; i++)
            {
                var elementId = (int)Read(32);
                while (true)
                {
                    var handle = ReadPacked();
                    if (handle == 0) break;
                    var numBits = (int)ReadPacked();
                    if (numBits < 0 || pos + numBits > bits) return;
                    if (numBits is 8 or 16 or 24 or 32 or 40)
                    {
                        var start = pos;
                        uint guid = 0;
                        for (var shift = 0; pos < start + numBits; shift += 7)
                        {
                            var b = Read(8);
                            guid |= (b >> 1) << shift;
                            if ((b & 1) == 0) break;
                        }
                        pos = start + numBits;
                        if (_treasureHandlerGuids.TryGetValue(guid, out var handler))
                        {
                            var actor = Channels[channel]?.ActorId ?? 0;
                            if (!_grants.Any(g => g.PlayerStateActor == actor && g.ElementId == elementId))
                            {
                                _grants.Add(new GrantEvent(Now, actor, elementId, handler));
                            }
                        }
                    }
                    else
                    {
                        pos += numBits;
                    }
                }
            }
        }
        catch (Exception e) when (e is IndexOutOfRangeException or InvalidDataException)
        {
            // Not a FastArray delta (or another layout): ignore.
            if (ProbeDebug) Console.Error.WriteLine($"GRANTFAIL {Now,7:F1}s ch{channel} bits={bits} pos={pos} {e.GetType().Name}");
        }
    }

    /// <summary>Debug 4: prints undecoded payloads with any readable text / object reference they contain.</summary>
    private void ProbeSink(uint channel, string group, string field, byte[] data, int bits)
    {
        var strings = new HashSet<string>();
        for (var shift = 0; shift < 8; shift++)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i + 1 <= (bits - shift) / 8; i++)
            {
                var bitPos = i * 8 + shift;
                var b = (data[bitPos / 8] >> (bitPos % 8)) | (bitPos / 8 + 1 < data.Length ? data[bitPos / 8 + 1] << (8 - bitPos % 8) : 0);
                var c = (char)(b & 0xFF);
                if (char.IsAsciiLetterOrDigit(c) || c is '_' or ':' or '.' or '/' or '-' or ' ')
                {
                    sb.Append(c);
                    continue;
                }
                if (sb.Length >= 5) strings.Add(sb.ToString().Trim());
                sb.Clear();
            }
            if (sb.Length >= 5) strings.Add(sb.ToString().Trim());
        }

        // Single object reference (packed int netguid)?
        var guidText = "";
        if (bits <= 48)
        {
            uint value = 0;
            var shiftBits = 0;
            foreach (var by in data)
            {
                value |= (uint)(by >> 1) << shiftBits;
                shiftBits += 7;
                if ((by & 1) == 0 || shiftBits > 28) break;
            }
            var path = PathOf(value);
            if (!string.IsNullOrEmpty(path)) guidText = $" guid={value}:{CleanName(path)}";
        }

        // Brute force: every bit offset decoded as a packed netguid that resolves to a known object path.
        if (bits > 48 && Environment.GetEnvironmentVariable("CHESTCOUNTER_PROBE_GUIDS") == "1")
        {
            var refs = new List<string>();
            int Bit(int pos) => pos < bits ? (data[pos >> 3] >> (pos & 7)) & 1 : 0;
            for (var start = 0; start + 8 <= bits; start++)
            {
                uint value = 0;
                var pos = start;
                for (var n = 0; n < 5 && pos + 8 <= bits; n++)
                {
                    var by = 0;
                    for (var k = 0; k < 8; k++) by |= Bit(pos++) << k;
                    value |= (uint)(by >> 1) << (7 * n);
                    if ((by & 1) == 0) break;
                }
                if (value > 64)
                {
                    var p = PathOf(value);
                    if (!string.IsNullOrEmpty(p)) refs.Add($"@{start}:{value}:{CleanName(p)}");
                }
            }
            if (refs.Count > 0) guidText += " refs=[" + string.Join(" ", refs) + "]";
        }

        var hex = bits <= (Environment.GetEnvironmentVariable("CHESTCOUNTER_PROBE_HEX") == "1" ? 100000 : 256) ? " hex=" + Convert.ToHexString(data) : "";
        var owner = _channelLabel.GetValueOrDefault(channel, "?");
        Console.Error.WriteLine($"PRB {Now,7:F1}s ch{channel} [{owner}] {CleanName(group)}.{field} bits={bits}{guidText}{hex}{(strings.Count > 0 ? " txt=[" + string.Join(" | ", strings.Where(s => s.Length >= 5)) + "]" : "")}");
    }

    private sealed class StderrLogger : Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => logLevel >= Microsoft.Extensions.Logging.LogLevel.Information;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Console.Error.WriteLine($"LOG {logLevel} {ReplayAnalyzer.Now,7:F1}s {formatter(state, exception)}");
    }

    protected override void OnNetDeltaRead(uint channelIndex, NetDeltaUpdate update)
    {
        base.OnNetDeltaRead(channelIndex, update);
        if (update.Export is GrantedAbility g && Environment.GetEnvironmentVariable("CHESTCOUNTER_DEBUG") is "1" or "2")
        {
            Console.Error.WriteLine($"DBG ability {Now,7:F1}s ch{channelIndex} idx={update.ElementIndex} del={update.Deleted} ability={CleanName(PathOf(g.Ability))} src={CleanName(PathOf(g.SourceObject))}");
        }
    }

    private static string Safe(Func<string> f) { try { return f(); } catch { return "!"; } }

    internal static float Now => Unreal.Core.ReplayReader<FortniteReplay>.CurrentTime;

    private string PathOf(uint? guid)
    {
        if (guid is null || guid == 0)
        {
            return "";
        }
        return _netGuidCache.TryGetPathName(guid.Value, out var path) ? path : "";
    }

    private static string CleanName(string path)
    {
        var name = path[(path.LastIndexOf('/') + 1)..];
        var dot = name.LastIndexOf('.');
        if (dot >= 0)
        {
            name = name[(dot + 1)..];
        }
        return UaidSuffix.Replace(name, "");
    }

    /// <summary>
    /// The prize handler ability class is sent the first time the recording player receives a treasure:
    /// that gives the exact moment of the first treasure of the match (even one never used).
    /// </summary>
    protected override void OnNetGuidExported(uint netGuid, string pathName)
    {
        if (Environment.GetEnvironmentVariable("CHESTCOUNTER_DEBUG") == "3")
        {
            Console.Error.WriteLine($"EXP {Now,7:F1}s {pathName}");
        }
        if (TrackedExport.IsMatch(pathName))
        {
            var name = CleanName(pathName);
            _firstExport.TryAdd(name, Now);
            // The ability spec references the class default object ("Default__GA_TreasureHandler_..._C").
            if (name.Contains("TreasureHandler", StringComparison.Ordinal))
            {
                _treasureHandlerGuids[netGuid] = name.Replace("Default__", "");
            }
        }
    }

    protected override void OnChannelOpened(uint channelIndex, NetworkGUID? actor)
    {
        base.OnChannelOpened(channelIndex, actor);

        var channel = Channels[channelIndex];
        var archetype = PathOf(channel?.ArchetypeId);
        var actorPath = PathOf(channel?.ActorId);

        // Static (level placed) actors have no archetype; their actor path is unique per chest.
        _channelKey[channelIndex] = !string.IsNullOrEmpty(actorPath) ? actorPath : $"guid:{channel?.ActorId}";
        _channelLabel[channelIndex] = CleanName(!string.IsNullOrEmpty(archetype) ? archetype : actorPath);
        _lastSearcher.Remove(channelIndex);
        _containersObserved.Remove(channelIndex);
        _equipChannelsSeen.Remove(channelIndex);
        _prizeChannelsSeen.Remove(channelIndex);
    }

    /// <summary>Extracts "DelilahPrize:dpid_item4" from the raw property bits (FString inside a struct).</summary>
    private static string? ReadPrizeId(DebuggingObject? raw)
    {
        if (raw == null || raw.TotalBits == 0)
        {
            return null;
        }
        var ascii = new string(raw.Bytes.Select(b => b is >= 32 and < 127 ? (char)b : ' ').ToArray());
        var match = PrizeIdPattern.Match(ascii);
        return match.Success ? match.Value : null;
    }

    protected override void OnExportRead(uint channelIndex, INetFieldExportGroup? exportGroup)
    {
        base.OnExportRead(channelIndex, exportGroup);

        if (exportGroup is PlayerPawn lifePawn && Channels[channelIndex]?.ActorId is uint lifeId
            && (lifePawn.bIsDying != null || lifePawn.bIsDBNO != null))
        {
            if (!_pawnLife.TryGetValue(lifeId, out var life)) _pawnLife[lifeId] = life = new();
            if (lifePawn.bIsDying == true) _pawnDeathTimes.TryAdd(lifeId, Now);
            var previous = life.Count == 0 || life[^1].Alive;
            life.Add((Now, lifePawn.bIsDying == true || lifePawn.bIsDBNO == true ? false
                : lifePawn.bIsDying == false || lifePawn.bIsDBNO == false ? true : previous));
        }
        if (exportGroup is FortPlayerState lifeState && Channels[channelIndex]?.ActorId is uint lifeStateId)
        {
            if (!_stateLife.TryGetValue(lifeStateId, out var life)) _stateLife[lifeStateId] = life = new();
            if (lifeState.DeathTags != null) life.Add((Now, false));
            else if (lifeState.RebootCounter is uint reboots && reboots > _stateReboots.GetValueOrDefault(lifeStateId))
            {
                _stateReboots[lifeStateId] = reboots;
                life.Add((Now, true));
            }
        }
        switch (exportGroup)
        {
            case BaseContainer container:
                var observedBefore = !_containersObserved.Add(channelIndex);
                if (container.SearchingPawn is uint searcher && searcher != 0)
                {
                    _lastSearcher[channelIndex] = searcher;
                }

                var containerKey = _channelKey.GetValueOrDefault(channelIndex, $"ch:{channelIndex}");
                if (container.bAlreadySearched)
                {
                    if (_searched.Add(containerKey))
                    {
                        _searches.Add(new SearchEvent(
                            Now,
                            containerKey,
                            _channelLabel.GetValueOrDefault(channelIndex, "?"),
                            _lastSearcher.TryGetValue(channelIndex, out var s) ? s : null, ObservedTransition: observedBefore));
                    }
                }
                else if (container.bDestroyed && container.SearchingPawn is uint destroyer && destroyer != 0)
                {
                    // Broken instead of opened (support destroyed, explosion...): the loot spills out and the
                    // server sets SearchingPawn to whoever destroyed it. Counted once, like an opened chest.
                    if (_searched.Add(containerKey))
                    {
                        _searches.Add(new SearchEvent(Now, containerKey, _channelLabel.GetValueOrDefault(channelIndex, "?"), destroyer, Destroyed: true, ObservedTransition: observedBefore));
                    }
                }
                break;

            case GrantedAbility ga when Environment.GetEnvironmentVariable("CHESTCOUNTER_DEBUG") is "1" or "2":
                Console.Error.WriteLine($"DBG abilityexp {Now,7:F1}s ch{channelIndex} owner={ga.OwnerActor} ability={CleanName(PathOf(ga.Ability))} src={CleanName(PathOf(ga.SourceObject))}");
                break;

            case LootCannonFired cannon when cannon.InteractingPawn is uint gunner && gunner != 0:
                var cannonKey = _channelKey.GetValueOrDefault(channelIndex, $"ch:{channelIndex}") + $"#fire{Now:F0}";
                if (_searched.Add(cannonKey))
                {
                    _searches.Add(new SearchEvent(Now, cannonKey, _channelLabel.GetValueOrDefault(channelIndex, "LootCannon"), gunner));
                }
                break;

            case TreasureHuntChest prize:
                var prizeId = ReadPrizeId(prize.PrizeTemplateId);
                if (_prizeChannelsSeen.TryGetValue(channelIndex, out var prizeIndex))
                {
                    var previous = _prizes[prizeIndex];
                    _prizes[prizeIndex] = previous with
                    {
                        Pawn = prize.OpeningPawn ?? prize.Instigator ?? previous.Pawn,
                        Container = prize.SourceContainer is > 0 ? PathOf(prize.SourceContainer) : previous.Container,
                        Success = prize.bSuccess ?? previous.Success,
                    };
                }
                else if (prizeId != null)
                {
                    _prizeChannelsSeen[channelIndex] = _prizes.Count;
                    _prizes.Add(new PrizeEvent(Now, prizeId, prize.OpeningPawn ?? prize.Instigator, PathOf(prize.SourceContainer), prize.bSuccess));
                }
                break;

            case GenericWeapon weapon:
                var holder = weapon.Instigator ?? weapon.Owner?.Value;
                if (holder is uint h && h != 0 && _equipChannelsSeen.Add(channelIndex))
                {
                    var itemName = weapon.WeaponData?.Name;
                    if (string.IsNullOrEmpty(itemName))
                    {
                        itemName = _channelLabel.GetValueOrDefault(channelIndex, "?");
                    }
                    _equips.Add(new EquipEvent(Now, itemName, h));
                }
                break;

            case FortPickup pickup when pickup.bTossedFromContainer == true && pickup.ItemDefinition != null:
                var item = pickup.ItemDefinition.Name;
                if (string.IsNullOrEmpty(item))
                {
                    item = CleanName(PathOf(pickup.ItemDefinition.Value));
                }
                if (!string.IsNullOrEmpty(item))
                {
                    _tosses.Add(new TossEvent(Now, item));
                }
                break;

            case PlayerPawn moving when moving.ReplicatedMovement is { } movement:
                if (Channels[channelIndex]?.ActorId is uint movingId && movement.Location != null)
                {
                    if (!_pawnSamples.TryGetValue(movingId, out var samples)) _pawnSamples[movingId] = samples = new();
                    samples.Add(new PawnSample(Now, movement.Location, moving.bIsDying != true && moving.bIsDBNO != true));
                }
                if (Channels[channelIndex]?.ActorId is uint mappedId && moving.PlayerState is uint mappedState && mappedState != 0)
                    _pawnToPlayerState[mappedId] = mappedState;
                break;

            case PlayerPawn pawn when pawn.PlayerState is uint ps && ps != 0:
                var pawnId = Channels[channelIndex]?.ActorId;
                if (pawnId != null)
                {
                    _pawnToPlayerState[pawnId.Value] = ps;
                }
                break;

            case FortPlayerState state:
                var psId = Channels[channelIndex]?.ActorId;
                var playerId = state.PlayerId is null ? state.PlayerID : (int?)state.PlayerId;
                if (psId != null && playerId != null)
                {
                    _playerStateToPlayerId[psId.Value] = playerId.Value;
                }
                break;
        }
    }

    /// <summary>
    /// Parses <paramref name="file"/> and returns what the tracked player did with chests.
    /// </summary>
    /// <param name="username">Epic display name to track. If null/empty or not found, the replay owner is used.</param>
    public MatchRecord Analyze(Stream stream, string file, string? username)
    {
        var replay = ReadReplay(stream);

        if (Environment.GetEnvironmentVariable("CHESTCOUNTER_DEBUG") == "4")
        {
            var groupFilter = new Regex(Environment.GetEnvironmentVariable("CHESTCOUNTER_PROBE_GROUPS") ?? "AbilitySpec", RegexOptions.IgnoreCase);
            foreach (var (path, group) in _netGuidCache.NetFieldExportGroupMap.Where(kv => groupFilter.IsMatch(kv.Key)))
            {
                Console.Error.WriteLine($"GRP {path} ({group.NetFieldExportsLength})");
                for (var i = 0; i < group.NetFieldExports.Length; i++)
                {
                    if (group.NetFieldExports[i] is { } e) Console.Error.WriteLine($"GRP   [{i}] {e.Name}");
                }
            }
        }

        // --- Who are we tracking? -------------------------------------------------------------
        var players = replay.PlayerData?.ToList() ?? new List<PlayerData>();
        PlayerData? target = null;
        var matchedByName = false;

        // One or several Epic names (several accounts), separated by commas. Strict: if names are configured,
        // only those players are counted; a match played by another account is not counted at all.
        var names = (username ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length > 0)
        {
            target = players.FirstOrDefault(p => names.Any(n => string.Equals(p.PlayerName?.Trim(), n, StringComparison.OrdinalIgnoreCase)));
            matchedByName = target != null;
        }

        uint? recorderPs = replay.GameData?.RecorderId;
        PlayerData? recorderPlayer = null;
        if (recorderPs != null && _playerStateToPlayerId.TryGetValue(recorderPs.Value, out var recorderPlayerId))
        {
            recorderPlayer = players.FirstOrDefault(p => p.Id == recorderPlayerId);
        }
        var notTracked = names.Length > 0 && target == null;
        if (target == null && !notTracked)
        {
            target = recorderPlayer;
        }

        // PlayerState actor(s) of the target -> pawn(s) of the target
        var targetPlayerStates = new HashSet<uint>();
        if (target?.Id != null)
        {
            foreach (var kv in _playerStateToPlayerId.Where(kv => kv.Value == target.Id))
            {
                targetPlayerStates.Add(kv.Key);
            }
        }
        if (targetPlayerStates.Count == 0 && recorderPs != null && !matchedByName && !notTracked)
        {
            targetPlayerStates.Add(recorderPs.Value);
        }

        var isRecorderTarget = recorderPs != null && targetPlayerStates.Contains(recorderPs.Value);
        var targetPawns = _pawnToPlayerState.Where(kv => targetPlayerStates.Contains(kv.Value)).Select(kv => kv.Key).ToHashSet();

        string? PlayerNameForPawn(uint? pawn) => pawn is uint p && _pawnToPlayerState.TryGetValue(p, out var ps) && _playerStateToPlayerId.TryGetValue(ps, out var pid)
            ? players.FirstOrDefault(x => x.Id == pid)?.PlayerName : null;
        string ScopeForPawn(uint? pawn)
        {
            if (pawn is uint p && targetPawns.Contains(p)) return "own";
            if (pawn is uint q && _pawnToPlayerState.TryGetValue(q, out var ps) && _playerStateToPlayerId.TryGetValue(ps, out var pid)
                && target?.TeamIndex is int ti && players.Any(x => x.Id == pid && x.TeamIndex == ti)) return "team";
            return pawn is uint knownPawn && _pawnToPlayerState.TryGetValue(knownPawn, out var knownState)
                && _playerStateToPlayerId.ContainsKey(knownState) ? "nearby" : "unknown";
        }
        bool AliveAt(uint pawn, float time)
        {
            var pawnAlive = !_pawnLife.TryGetValue(pawn, out var pl) || pl.LastOrDefault(x => x.Time <= time, (Time: 0f, Alive: true)).Alive;
            var stateAlive = !_pawnToPlayerState.TryGetValue(pawn, out var ps) || !_stateLife.TryGetValue(ps, out var sl)
                || sl.LastOrDefault(x => x.Time <= time, (Time: 0f, Alive: true)).Alive;
            return pawnAlive && stateAlive && (!_pawnDeathTimes.TryGetValue(pawn, out var death) || time < death);
        }
        PawnSample? Sample(uint pawn, float time) => _pawnSamples.TryGetValue(pawn, out var samples)
            ? samples.LastOrDefault(x => x.Time <= time) : null;
        foreach (var search in _searches.Where(x => x.SearcherPawn is uint p && ScopeForPawn(p) == "team" && ChestKinds.IsChest(ChestKinds.Classify(x.Label))))
        {
            var mateSample = Sample(search.SearcherPawn!.Value, search.Time);
            var ownSample = targetPawns.Select(p => Sample(p, search.Time)).Where(x => x != null).OrderByDescending(x => x!.Time).FirstOrDefault();
            var ownAge = ownSample == null ? (float?)null : search.Time - ownSample.Time;
            var mateAge = mateSample == null ? (float?)null : search.Time - mateSample.Time;
            var ownPawn = targetPawns.OrderByDescending(p => Sample(p, search.Time)?.Time ?? -1).FirstOrDefault();
            var alive = ownSample?.Alive == true && mateSample?.Alive == true && ownAge <= 1 && mateAge <= 1
                && AliveAt(ownPawn, search.Time) && AliveAt(search.SearcherPawn.Value, search.Time);
            DistanceObservations.Add(new(search.Time, PlayerNameForPawn(search.SearcherPawn) ?? "?", ChestKinds.Classify(search.Label),
                alive ? ownSample!.Position.DistanceTo(mateSample!.Position) / 100.0 : null, ownAge, mateAge, alive, search.ObservedTransition));
        }

        // --- Everything the target searched / interacted with ------------------------------------
        var interactions = _searches
            .Where(s => s.SearcherPawn is uint p && targetPawns.Contains(p))
            .OrderBy(s => s.Time)
            .Select(s => (s.Time, Kind: ChestKinds.Classify(s.Label), s.Label, s.ActorKey, s.Destroyed))
            .ToList();

        var chests = new List<ChestRecord>();
        var others = new Dictionary<string, int>();
        foreach (var search in interactions)
        {
            if (!ChestKinds.IsChest(search.Kind))
            {
                others[search.Kind] = others.GetValueOrDefault(search.Kind) + 1;
                continue;
            }

            var items = _tosses
                .Where(t => t.Time >= search.Time - 0.25f && t.Time <= search.Time + 0.6f)
                .Select(t => t.Item)
                .ToList();

            chests.Add(new ChestRecord
            {
                TimeSeconds = MathF.Round(search.Time, 1),
                Kind = search.Kind,
                Blueprint = search.Label,
                Destroyed = search.Destroyed,
                Items = items,
                TreasureItems = items.Where(i => TreasureItem.IsMatch(i)).ToList(),
            });
        }

        // --- Teammates: same team as the target, with what they were seen searching ----------------
        // The replay only holds what happened near the recording player: a teammate far away is not seen.
        var teammates = new List<TeammateRecord>();
        if (target?.TeamIndex is int team)
        {
            var mates = players
                .Where(p => p.TeamIndex == team && p.Id != target.Id && p.IsBot != true)
                .GroupBy(p => p.EpicId ?? p.PlayerName ?? $"pid{p.Id}")
                .Select(g => g.First())
                .ToList();
            foreach (var mate in mates)
            {
                var states = _playerStateToPlayerId.Where(kv => kv.Value == mate.Id).Select(kv => kv.Key).ToHashSet();
                var pawns = _pawnToPlayerState.Where(kv => states.Contains(kv.Value)).Select(kv => kv.Key).ToHashSet();
                var kinds = _searches
                    .Where(s => s.SearcherPawn is uint p && pawns.Contains(p))
                    .GroupBy(s => ChestKinds.Classify(s.Label))
                    .ToDictionary(g => g.Key, g => g.Count());
                teammates.Add(new TeammateRecord
                {
                    Name = !string.IsNullOrWhiteSpace(mate.PlayerName) ? mate.PlayerName.Trim() : L.T("Compañero", "Teammate"),
                    EpicId = mate.EpicId,
                    ChestsSeen = kinds.Where(k => ChestKinds.IsChest(k.Key)).Sum(k => k.Value),
                    Kinds = kinds,
                });
            }
            teammates.Sort((a, b) => b.ChestsSeen.CompareTo(a.ChestsSeen));
        }

        if (Environment.GetEnvironmentVariable("CHESTCOUNTER_DEBUG") == "1")
        {
            foreach (var t in teammates)
            {
                Console.Error.WriteLine($"DBG teammate {t.Name} chests={t.ChestsSeen}");
            }
            string NameOfPawn(uint pawn) =>
                _pawnToPlayerState.TryGetValue(pawn, out var ps) && _playerStateToPlayerId.TryGetValue(ps, out var pid)
                    ? players.FirstOrDefault(p => p.Id == pid)?.PlayerName ?? $"pid{pid}" : $"pawn{pawn}";
            foreach (var e in _equips)
            {
                Console.Error.WriteLine($"DBG equip {e.Time,7:F1}s {e.Item} by {NameOfPawn(e.Pawn)}{(targetPawns.Contains(e.Pawn) ? " (TARGET)" : "")}");
            }
            foreach (var ex in _firstExport)
            {
                Console.Error.WriteLine($"DBG export {ex.Value,7:F1}s {ex.Key}");
            }
            foreach (var hg in _treasureHandlerGuids)
            {
                Console.Error.WriteLine($"DBG handlerguid {hg.Key} {hg.Value}");
            }
            foreach (var gr in _grants)
            {
                var psName = _playerStateToPlayerId.TryGetValue(gr.PlayerStateActor, out var gpid) ? players.FirstOrDefault(p => p.Id == gpid)?.PlayerName ?? $"pid{gpid}" : $"ps{gr.PlayerStateActor}";
                Console.Error.WriteLine($"DBG grant {gr.Time,7:F1}s {gr.Handler} elem={gr.ElementId} to {psName}{(targetPlayerStates.Contains(gr.PlayerStateActor) ? " (TARGET)" : "")}");
                foreach (var se in _searches.Where(x => x.Time >= gr.Time - 3 && x.Time <= gr.Time + 2).OrderBy(x => Math.Abs(x.Time - gr.Time)).Take(2))
                {
                    Console.Error.WriteLine($"DBG   near {se.Time,7:F1}s {CleanName(se.Label)} by {(se.SearcherPawn is uint sp2 ? NameOfPawn(sp2) : "?")}");
                }
            }
            foreach (var ex in _firstExport.Where(kv => kv.Key.Contains("TreasureHandler")).Take(1))
            {
                foreach (var se in _searches.Where(x => x.Time >= ex.Value - 6 && x.Time <= ex.Value + 3).OrderBy(x => x.Time))
                {
                    Console.Error.WriteLine($"DBG nearhandler {se.Time,7:F1}s (Δ{se.Time - ex.Value:+0.0;-0.0}) {CleanName(se.Label)}{(se.Destroyed ? " DESTROYED" : "")} by {(se.SearcherPawn is uint sp ? NameOfPawn(sp) : "?")}{(se.SearcherPawn is uint tp2 && targetPawns.Contains(tp2) ? " (ME)" : "")}");
                }
            }
            foreach (var pz in _prizes)
            {
                Console.Error.WriteLine($"DBG prize {pz.Time,7:F1}s {pz.PrizeId} by {(pz.Pawn is uint pp ? NameOfPawn(pp) : "?")}{(pz.Pawn is uint tp && targetPawns.Contains(tp) ? " (TARGET)" : "")} chest={CleanName(pz.Container)}");
            }
            foreach (var s in _searches.Where(s => s.Label.Contains("Cannon", StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine($"DBG cannon {s.Time,7:F1}s {s.Label} by {(s.SearcherPawn is uint cp ? NameOfPawn(cp) : "?")}");
            }
            foreach (var i in interactions)
            {
                Console.Error.WriteLine($"DBG interact {i.Time,7:F1}s {i.Kind} {i.Label}{(i.Destroyed ? " (DESTROYED)" : "")}");
            }
        }

        // --- Treasures obtained by the target ----------------------------------------------------
        var finds = new List<TreasureFind>();

        // 1) Golden treasure chest: the server spawns an animated chest carrying the prize id and its source container. Certain.
        foreach (var pz in _prizes.Where(p => p.Success != false))
        {
            var label = CleanName(pz.Container);
            var kind = string.IsNullOrEmpty(label) ? "unknown" : ChestKinds.Classify(label);
            finds.Add(new TreasureFind
            {
                Scope = ScopeForPawn(pz.Pawn),
                Finder = PlayerNameForPawn(pz.Pawn),
                Evidence = "golden-prize",
                IsPirateToken = TreasureNames.IsPirateToken(pz.PrizeId),
                Item = TreasureNames.Friendly(pz.PrizeId),
                RawItem = pz.PrizeId,
                PrizeId = pz.PrizeId,
                FromGoldenChest = true,
                TimeExact = true,
                SourceDestroyed = interactions.Any(i => i.Destroyed && i.ActorKey == pz.Container),
                ObtainedAtSeconds = MathF.Round(pz.Time, 1),
                SourceKind = kind,
                SourceBlueprint = label,
                SourceIsChest = ChestKinds.IsChest(kind),
                SourceCertain = !string.IsNullOrEmpty(label),
            });
        }

        // 2) Items seen in the target's hands are NOT counted on their own (they can come from a teammate,
        //    since treasures are shared by team, or from elsewhere). They are only used to name a treasure below.
        var equippedByMe = _equips.Where(e => targetPawns.Contains(e.Pawn)).OrderBy(e => e.Time).ToList();

        // 3) Treasure item thrown out of a container as a pickup (not observed so far, kept just in case).
        foreach (var chest in chests.Where(c => c.TreasureItems.Count > 0))
        {
            foreach (var item in chest.TreasureItems.Select(TreasureNames.Friendly).Distinct())
            {
                if (finds.All(f => f.Item != item))
                {
                    finds.Add(new TreasureFind
                    {
                        Item = item, RawItem = item, ObtainedAtSeconds = chest.TimeSeconds,
                        SourceKind = chest.Kind, SourceBlueprint = chest.Blueprint, SourceIsChest = true, SourceCertain = true,
                    });
                }
            }
        }
        // 4) Prize handlers: every treasure received grants a "GA_TreasureHandler_*" ability on the player state of
        //    the recording player (exact receipt time). The source is only a temporal inference; team rewards
        //    are kept separately so settings can include them without modifying the saved own count.
        //    Fallback when no grant could be decoded: first export of the handler class (first treasure only).
        var handlerTimes = _grants
            .Where(g => targetPlayerStates.Contains(g.PlayerStateActor))
            .Select(g => g.Time)
            .OrderBy(t => t)
            .ToList();
        if (handlerTimes.Count == 0 && isRecorderTarget
            && _firstExport.Where(kv => kv.Key.Contains("TreasureHandler")).Select(kv => (float?)kv.Value).FirstOrDefault() is float firstHandler)
        {
            handlerTimes.Add(firstHandler);
        }

        for (var hi = 0; hi < handlerTimes.Count; hi++)
        {
            var h = handlerTimes[hi];
            var nextHandler = hi + 1 < handlerTimes.Count ? handlerTimes[hi + 1] : float.MaxValue;
            var closest = _searches
                .Where(x => x.Time >= h - 3 && x.Time <= h + 2 && (ScopeForPawn(x.SearcherPawn) is "own" or "team"))
                .OrderBy(x => Math.Abs(x.Time - h))
                .FirstOrDefault();
            var golden = finds.FirstOrDefault(f => (f.Scope is "own" or "team") && f.FromGoldenChest && f.ObtainedAtSeconds is float g && Math.Abs(g - h) <= 5);

            if (golden == null && (closest != null || _grants.Any(g => targetPlayerStates.Contains(g.PlayerStateActor) && g.Time == h)))
            {
                var kind = closest == null ? "unknown" : ChestKinds.Classify(closest.Label);

                // Item identity: an item first sent at that exact moment, otherwise the only treasure item the
                // target later had in hand (if there is exactly one kind).
                var item = _firstExport.Where(kv => kv.Key.StartsWith("WID_") && Math.Abs(kv.Value - h) <= 1).Select(kv => kv.Key).FirstOrDefault();
                if (item == null)
                {
                    var later = equippedByMe.Where(e => e.Time >= h && e.Time < nextHandler).Select(e => TreasureNames.Friendly(e.Item)).Where(n => !n.StartsWith("B_")).Distinct().ToList();
                    if (later.Count == 1)
                    {
                        item = equippedByMe.First(e => e.Time >= h && e.Time < nextHandler && TreasureNames.Friendly(e.Item) == later[0]).Item;
                    }
                }
                var itemName = item != null ? TreasureNames.Friendly(item) : null;

                // No id in the replay: give it a unique key so the player can name it in treasures.json.
                var matchStart = replay.GameData?.UtcTimeStartedMatch ?? replay.Info?.Timestamp ?? DateTime.UtcNow;
                var localStart = DateTime.SpecifyKind(matchStart, DateTimeKind.Utc).ToLocalTime();
                var hr = MathF.Round(h, 1);
                var unknownKey = item == null ? $"{TreasureNames.UnknownPrefix}{localStart:yyyy-MM-dd_HH.mm}@{(int)hr / 60}m{(int)hr % 60:00}s" : null;

                finds.Add(new TreasureFind
                {
                    Scope = closest == null ? "team" : ScopeForPawn(closest.SearcherPawn),
                    Finder = PlayerNameForPawn(closest?.SearcherPawn),
                    Evidence = _grants.Any(g => targetPlayerStates.Contains(g.PlayerStateActor) && g.Time == h) ? "recipient-handler" : "handler-export",
                    IsPirateToken = item != null && TreasureNames.IsPirateToken(item)
                        && _grants.Any(g => targetPlayerStates.Contains(g.PlayerStateActor) && g.Time == h),
                    Item = itemName ?? TreasureNames.UnknownGizmo,

                    RawItem = item ?? "GA_TreasureHandler",
                    PrizeId = unknownKey,
                    ObtainedAtSeconds = hr,
                    TimeExact = true,
                    SourceKind = kind,
                    SourceBlueprint = closest?.Label ?? "",
                    SourceIsChest = ChestKinds.IsChest(kind),
                    SourceCertain = false,
                    SourceDestroyed = closest?.Destroyed == true,
                    FromPrizeHandler = true,
                });
            }
        }

        foreach (var grant in _grants.Where(g => !targetPlayerStates.Contains(g.PlayerStateActor)))
        {
            if (!_playerStateToPlayerId.TryGetValue(grant.PlayerStateActor, out var pid)) continue;
            var recipient = players.FirstOrDefault(p => p.Id == pid);
            if (recipient == null) continue;
            var ownTeam = target?.TeamIndex is int teamIndex && recipient.TeamIndex == teamIndex;
            if (ownTeam) continue; // shared grants are already read on the target's player state
            var recipientPawns = _pawnToPlayerState.Where(k => k.Value == grant.PlayerStateActor).Select(k => k.Key).ToHashSet();
            if (finds.Any(f => f.FromGoldenChest && f.Finder == recipient.PlayerName && Math.Abs((f.ObtainedAtSeconds ?? -100) - grant.Time) <= 5)) continue;
            var source = _searches.Where(x => x.SearcherPawn is uint p && recipientPawns.Contains(p) && Math.Abs(x.Time - grant.Time) <= 2)
                .OrderBy(x => Math.Abs(x.Time - grant.Time)).FirstOrDefault();
            var kind = source == null ? "unknown" : ChestKinds.Classify(source.Label);
            finds.Add(new TreasureFind
            {
                Scope = "nearby", Finder = recipient.PlayerName, Evidence = "recipient-handler",
                Item = TreasureNames.UnknownGizmo, RawItem = grant.Handler, ObtainedAtSeconds = grant.Time,
                IsPirateToken = TreasureNames.IsPirateToken(grant.Handler),
                TimeExact = true, SourceKind = kind, SourceBlueprint = source?.Label ?? "",
                SourceIsChest = ChestKinds.IsChest(kind), SourceCertain = false, FromPrizeHandler = true,
            });
        }

        finds.Sort((a, b) => (a.ObtainedAtSeconds ?? float.MaxValue).CompareTo(b.ObtainedAtSeconds ?? float.MaxValue));

        var info = replay.Info;
        var header = replay.Header;
        var isRecorder = recorderPs != null && targetPlayerStates.Contains(recorderPs.Value);
        var placement = target?.Placement ?? (isRecorder ? (int?)replay.TeamStats?.Position : null);

        return new MatchRecord
        {
            SessionId = replay.GameData?.GameSessionId ?? $"{info?.Timestamp:O}-{Path.GetFileName(file)}",
            ReplayFile = Path.GetFileName(file),
            AnalyzedAtUtc = DateTime.UtcNow,
            MatchStartUtc = replay.GameData?.UtcTimeStartedMatch ?? info?.Timestamp,
            DurationSeconds = info != null ? (int)(info.LengthInMs / 1000) : null,
            Playlist = replay.GameData?.CurrentPlaylist,
            GameVersion = header?.Branch,
            PlayerName = target?.PlayerName,
            EpicId = target?.EpicId,
            TrackedByUsername = matchedByName,
            NotTrackedOwner = notTracked ? (recorderPlayer?.PlayerName ?? "unknown") : null,
            Placement = placement,
            TotalPlayers = (int?)replay.TeamStats?.TotalPlayers,
            IsReplayOwner = isRecorder,
            Eliminations = isRecorder ? (int?)replay.Stats?.Eliminations : null,
            ChestsOpened = chests.Count,
            AmmoBoxesOpened = others.GetValueOrDefault(ChestKinds.AmmoBox),
            OtherContainersOpened = others.Where(kv => kv.Key != ChestKinds.AmmoBox).Sum(kv => kv.Value),
            OtherInteractions = others,
            AnalyzerVersion = CurrentVersion,
            IsBattleRoyale = Profiles.IsBattleRoyalePlaylist(replay.GameData?.CurrentPlaylist),
            Chests = chests,
            TreasureFinds = finds.Where(f => f.Scope == "own").ToList(),
            NearbyTreasureFinds = finds.Where(f => f.Scope != "own").ToList(),
            DistanceObservations = DistanceObservations,
            Teammates = teammates,
        };
    }
}

internal static class SafeExt { }

public static class TreasureNames
{
    /// <summary>Prize id -> display name, loaded from treasures.json (user editable).</summary>
    public static Dictionary<string, string> PrizeNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Key prefix of a treasure whose prize id is not in the replay (another prefix before version 1.1).</summary>
    public const string UnknownPrefix = "Unknown:";
    public const string UnknownGizmo = "Treasure gizmo";
    private const string OldUnknownPrefix = "Inconnu:";

    /// <summary>Display name of a prize id (from treasures.json), or of a treasure item blueprint.</summary>
    // Only explicit semantic identifiers; an unknown numeric prize id is never guessed.
    public static bool IsPirateToken(string item) => Regex.IsMatch(item, @"Pirate[ _-]*Shop|DelilahShop|(?:token|acceso)(?: de| a| la| tienda|[ _-])*pirata", RegexOptions.IgnoreCase);

    public static string Friendly(string item)
    {
        if (item.Contains(':'))
        {
            var unknown = item.StartsWith(UnknownPrefix, StringComparison.OrdinalIgnoreCase) || item.StartsWith(OldUnknownPrefix, StringComparison.OrdinalIgnoreCase);
            if (PrizeNames.TryGetValue(item, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            if (unknown)
            {
                // Same treasure under the other prefix (named by the player with an older version, or re-analyzed).
                var rest = item[(item.IndexOf(':') + 1)..];
                foreach (var prefix in new[] { UnknownPrefix, OldUnknownPrefix })
                {
                    if (PrizeNames.TryGetValue(prefix + rest, out var other) && !string.IsNullOrWhiteSpace(other))
                    {
                        return other;
                    }
                }
                // Not a golden chest prize (those carry their id): one of the treasure hunt gizmos (golden
                // firework, portable extractor, locators, extraction accelerator), exact one not in the replay.
                return L.Name(UnknownGizmo);
            }
            return L.F("Tesoro desconocido ({0})", "Unknown treasure ({0})", item[(item.IndexOf(':') + 1)..]);
        }


        var i = item.ToLowerInvariant();
        if (i.Contains("flameceleb")) return "Golden firework";
        if (i.Contains("relicextractor")) return "Portable extractor";
        if (i.Contains("relicfinder")) return "Spirit locator";
        if (i.Contains("luckylocator")) return "Lucky locator";
        if (i.Contains("smugglerextractionkey") || i.Contains("extractionaccelerat")) return "Extraction accelerator";
        return item;
    }
}

public static class ChestKinds
{
    // Chests (counted in the total)
    public const string Normal = "Chest";
    public const string Rare = "Rare chest";
    public const string Buried = "Buried chest";
    public const string Extraction = "Extraction / relic chest";
    public const string PosterChest = "Spirit poster chest";
    public const string Other = "Other chest";

    // Other interactions (tracked separately, not in the chest total)
    public const string Cannon = "Loot cannon";
    public const string AmmoBox = "Ammo box";
    public const string FoodBox = "Food box";
    public const string OtherContainer = "Other container";
    public const string Poster = "Spirit wanted poster";

    private static readonly HashSet<string> ChestSet = new() { Normal, Rare, Buried, Extraction, PosterChest, Other };

    public static bool IsChest(string kind) => ChestSet.Contains(kind);

    public static string Classify(string blueprint)
    {
        var b = blueprint.ToLowerInvariant();
        if (b.Contains("lootcannon") || b.Contains("cannon")) return Cannon;
        if (b.Contains("ammo")) return AmmoBox;
        if (b.Contains("foodbox") || b.Contains("food_box")) return FoodBox;
        if (b.Contains("rarechest") || b.Contains("legendary") || b.Contains("rare_chest")) return Rare;
        if (b.Contains("buried")) return Buried;
        if (b.Contains("spriteposter") && !b.Contains("chest")) return Poster;
        if (b.Contains("spriteposter") || b.Contains("booty")) return PosterChest;
        if (b.Contains("relic") || b.Contains("extraction")) return Extraction;
        if (b.Contains("tiered_chest") || b.Contains("normalchest") || b.Contains("standardchest")) return Normal;
        // Coolers, barrels, etc. are not chests.
        return b.Contains("chest") ? Other : OtherContainer;
    }
}
