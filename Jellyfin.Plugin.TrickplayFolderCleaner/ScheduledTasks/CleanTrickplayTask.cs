using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TrickplayFolderCleaner.ScheduledTasks;

/// <summary>
/// A scheduled task to clean up orphaned trickplay folders.
/// </summary>
public class CleanTrickplayTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<CleanTrickplayTask> _logger;
    private readonly string[] _mediaExtensions =
    [
        ".3gp",
        ".asf",
        ".avi",
        ".divx",
        ".flv",
        ".hevc",
        ".m2ts",
        ".m4v",
        ".mkv",
        ".mov",
        ".mp4",
        ".mpeg",
        ".mpg",
        ".mts",
        ".ogg",
        ".ogv",
        ".rm",
        ".rmvb",
        ".ts",
        ".webm",
        ".wmv"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanTrickplayTask"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="logger">The logger.</param>
    public CleanTrickplayTask(ILibraryManager libraryManager, IFileSystem fileSystem, ILogger<CleanTrickplayTask> logger)
    {
        _libraryManager = libraryManager;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Trickplay Folder Cleaner";

    /// <inheritdoc />
    public string Key => "TrickplayFolderCleaner";

    /// <inheritdoc />
    public string Description => "Deletes .trickplay folders that no longer have a corresponding media file.";

    /// <inheritdoc />
    public string Category => "Trickplay Folder Cleaner";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting trickplay folder cleanup.");

        var libraryFolders = _libraryManager.GetVirtualFolders()
            .SelectMany(f => f.Locations)
            .Distinct()
            .ToList();

        int totalDeleted = 0;

        foreach (var folder in libraryFolders.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
        {
            _logger.LogDebug("Scanning library folder: {Folder}", folder);
            totalDeleted += CleanDirectory(folder, cancellationToken);
        }

        _logger.LogInformation("Trickplay folder cleanup finished. Deleted {Count} folders.", totalDeleted);
        return Task.CompletedTask;
    }

    private int CleanDirectory(string path, CancellationToken cancellationToken)
    {
        int deletedCount = 0;
        try
        {
            // Get all directories recursively
            var directories = _fileSystem.GetDirectories(path, true).ToList();

            foreach (var dir in directories.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
            {
                if (!dir.Name.EndsWith(".trickplay", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check if parent is also a .trickplay folder (skip nested ones if any, based on script logic)
                var parentPath = Path.GetDirectoryName(dir.FullName);
                if (string.IsNullOrEmpty(parentPath))
                {
                    continue;
                }

                var parentName = Path.GetFileName(parentPath);
                if (parentName.EndsWith(".trickplay", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string trickplayBaseName = dir.Name[..^".trickplay".Length];

                // Check if any media file exists in parent with the same basename
                var files = _fileSystem.GetFiles(parentPath);
                bool mediaExists = files.Any(f =>
                    _mediaExtensions.Contains(Path.GetExtension(f.FullName).ToLowerInvariant()) &&
                    Path.GetFileNameWithoutExtension(f.FullName).Equals(trickplayBaseName, StringComparison.OrdinalIgnoreCase));

                if (mediaExists)
                {
                    continue;
                }

                _logger.LogInformation("Deleting orphaned trickplay folder: {Path}", dir.FullName);
                try
                {
                    Directory.Delete(dir.FullName, true);
                    deletedCount++;
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error deleting directory {Path}", dir.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory {Path}", path);
        }

        return deletedCount;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerWeekly,
                DayOfWeek = DayOfWeek.Sunday,
                TimeOfDayTicks = TimeSpan.FromHours(2).Ticks
            }
        ];
    }
}
