using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

/// <remarks>
/// This took me literal hours of jumping through hoops to get right because
/// Roblox's log file system is an indeterministic mess.
/// Any future contributors who touch this code should be very careful to understand the
/// nuances of how Roblox creates, rotates, and decides to write to its log files.
/// </remarks>
public sealed class RobloxLogMonitor : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly string _logDir;

    /*
     * Roblox does not reliably use one log file per running session.
     *
     * It can:
     *
     *   1. Create a newer *_Player_*.log file.
     *   2. Continue writing to an older *_Player_*.log file.
     *   3. Create small ~1 KB log files that may never receive meaningful
     *      runtime output.
     *
     * Therefore, we MUST NOT assume that the newest log filename is the
     * currently active log.
     *
     * Instead, every discovered log gets its own read position.
     *
     * Example:
     *
     *   Player_A.log -> 60,123
     *   Player_B.log ->  1,024
     *   Player_C.log -> 48,912
     *
     * If Roblox writes to Player_A after creating Player_C, we continue
     * reading Player_A from position 60,123.
     */
    private readonly Dictionary<string, long> _filePositions =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _running;

    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    /*
     * FileSystemWatcher and the polling loop can both discover the same
     * file at roughly the same time.
     *
     * This lock protects the file-position dictionary and prevents two
     * readers from processing the same appended data simultaneously.
     */
    private readonly object _processLock = new();

    // Maps compiled Regex patterns to their callback handlers.
    private Dictionary<Regex, Action<Match>> _handlers = new();

    public bool IsRunning => _running;

    public RobloxLogMonitor()
    {
        _logDir = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Roblox",
            "logs"
        );

        _watcher = new FileSystemWatcher
        {
            Path = Directory.Exists(_logDir)
                ? _logDir
                : AppContext.BaseDirectory,

            Filter = "*.log",

            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size
        };

        _watcher.Changed += OnDirChange;
        _watcher.Created += OnDirChange;
        _watcher.Renamed += OnDirChange;
    }

    public void Start(Dictionary<string, Action<Match>> patternsAndCallbacks)
    {
        if (_running)
            return;

        _handlers = patternsAndCallbacks.ToDictionary(
            kvp => new Regex(
                kvp.Key,
                RegexOptions.Compiled),
            kvp => kvp.Value
        );

        _running = true;

        if (Directory.Exists(_logDir))
        {
            _watcher.Path = _logDir;
            _watcher.EnableRaisingEvents = true;
        }

        /*
         * Do an initial scan before starting the polling loop.
         *
         * Existing files are initialized at their current length so that
         * starting the monitor does not replay an entire old Roblox log.
         */
        DiscoverAndProcessLogs();

        _pollCts = new CancellationTokenSource();
        _pollTask = Task.Run(
            () => InternalPollAsync(_pollCts.Token));
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        _watcher.EnableRaisingEvents = false;

        _pollCts?.Cancel();

        try
        {
            _pollTask?.Wait(1000);
        }
        catch (AggregateException)
        {
            // Ignore cancellation exceptions during shutdown.
        }

        _pollCts?.Dispose();
        _pollCts = null;
        _pollTask = null;
    }

    public void Close()
    {
        _pollTask = null;
        _handlers.Clear();
        _filePositions.Clear();
    }

    public void Dispose()
    {
        Stop();

        _watcher.Changed -= OnDirChange;
        _watcher.Created -= OnDirChange;
        _watcher.Renamed -= OnDirChange;
        _watcher.Dispose();

        Close();
    }

    private async Task InternalPollAsync(CancellationToken token)
    {
        while (_running && !token.IsCancellationRequested)
        {
            try
            {
                /*
                 * FileSystemWatcher is intentionally NOT our only source
                 * of file changes.
                 *
                 * Windows can coalesce/drop filesystem events, and Roblox
                 * can behave unusually when creating/rotating its logs.
                 *
                 * The one-second scan is therefore the reliability
                 * mechanism; FileSystemWatcher is merely the fast path.
                 */
                DiscoverAndProcessLogs();
            }
            catch
            {
                // Ignore transient monitoring failures.
            }

            try
            {
                await Task.Delay(1000, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void OnDirChange(object sender, FileSystemEventArgs e)
    {
        if (!_running)
            return;

        string fileName = Path.GetFileName(e.FullPath);

        if (!IsRobloxPlayerLog(fileName))
            return;

        /*
         * Do not simply start watching e.FullPath as "the current log".
         *
         * Roblox may have created a newer file while still writing to an
         * older one. All matching files are potentially active.
         *
         * Process the file that caused the event, and let the periodic
         * discovery scan find any other files that are also being written.
         */
        ProcessLogFile(e.FullPath);
    }

    private void DiscoverAndProcessLogs()
    {
        if (!Directory.Exists(_logDir))
            return;

        try
        {
            FileInfo[] files = new DirectoryInfo(_logDir)
                .GetFiles("*.log");

            foreach (FileInfo file in files)
            {
                if (!IsRobloxPlayerLog(file.Name))
                    continue;

                ProcessLogFile(file.FullName);
            }
        }
        catch
        {
            /*
             * The directory can temporarily be unavailable while Roblox
             * is creating/rotating files. The next poll or filesystem event
             * will retry it.
             */
        }
    }

    private void ProcessLogFile(string path)
    {
        path = Path.GetFullPath(path);

        lock (_processLock)
        {
            if (!_running)
                return;

            if (!File.Exists(path))
                return;

            try
            {
                using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete
                );

                /*
                 * A file can theoretically be truncated/reset.
                 *
                 * If that happens, our previous byte position is no longer
                 * valid, so start reading from the beginning again.
                 */
                if (!_filePositions.TryGetValue(
                        path,
                        out long position))
                {
                    /*
                     * This is a newly discovered file.
                     *
                     * We intentionally start at the current end rather
                     * than replaying potentially thousands of old lines.
                     *
                     * A file that was already known retains its exact
                     * position and therefore continues from where we left.
                     */
                    _filePositions[path] = fs.Length;
                    return;
                }

                if (fs.Length < position)
                    position = 0;

                if (fs.Length == position)
                {
                    _filePositions[path] = position;
                    return;
                }

                fs.Seek(position, SeekOrigin.Begin);

                using var reader = new StreamReader(fs);

                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    string cleanLine = line.Trim();

                    if (string.IsNullOrEmpty(cleanLine))
                        continue;

                    ParseLine(cleanLine);
                }

                /*
                 * StreamReader has consumed everything available in the
                 * stream. At EOF, fs.Position represents the new point from
                 * which the next invocation should continue.
                 */
                _filePositions[path] = fs.Position;
            }
            catch (IOException)
            {
                /*
                 * Roblox may be writing to the file at this exact moment.
                 *
                 * Do not discard the saved position. The next filesystem
                 * event or one-second poll will retry the read.
                 */
            }
            catch (UnauthorizedAccessException)
            {
                /*
                 * Same idea: transient access failure. Leave the existing
                 * position untouched so the next attempt can recover.
                 */
            }
        }
    }

    private static bool IsRobloxPlayerLog(string fileName)
    {
        return fileName.Contains(
                   "_Player_",
                   StringComparison.OrdinalIgnoreCase) &&
               fileName.EndsWith(
                   ".log",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetLogTimestamp(
        string fileName,
        out DateTime timestamp)
    {
        timestamp = default;

        /*
         * Roblox filenames look like:
         *
         * 0.739.0.7390687_20260918T000043Z_Player_55CF2_last.log
         *
         * The timestamp in the filename identifies when Roblox created
         * that particular log file.
         */
        int playerIndex = fileName.IndexOf(
            "_Player_",
            StringComparison.OrdinalIgnoreCase);

        if (playerIndex <= 0)
            return false;

        string prefix = fileName[..playerIndex];

        int separator = prefix.LastIndexOf('_');

        if (separator < 0)
            return false;

        string timestampText =
            prefix[(separator + 1)..];

        return DateTime.TryParseExact(
            timestampText,
            "yyyyMMdd'T'HHmmss'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal,
            out timestamp);
    }

    private static bool TryGetLineTimestamp(
        string line,
        out DateTime timestamp)
    {
        timestamp = default;

        /*
         * Roblox log lines begin with an ISO-8601 UTC timestamp:
         *
         * 2026-09-18T00:02:45.951Z,122.951820,...
         *
         * Only the timestamp before the first comma is relevant.
         */
        int commaIndex = line.IndexOf(',');

        if (commaIndex <= 0)
            return false;

        string timestampText = line[..commaIndex];

        return DateTime.TryParseExact(
            timestampText,
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal,
            out timestamp);
    }

    private void ParseLine(string line)
    {
        foreach (var (pattern, callback) in _handlers)
        {
            Match match = pattern.Match(line);

            if (!match.Success)
                continue;

            callback(match);
        }
    }
}