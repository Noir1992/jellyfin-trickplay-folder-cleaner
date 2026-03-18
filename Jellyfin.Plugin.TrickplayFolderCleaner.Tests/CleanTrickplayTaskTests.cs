using Jellyfin.Plugin.TrickplayFolderCleaner.ScheduledTasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.TrickplayFolderCleaner.Tests;

public class CleanTrickplayTaskTests
{
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ILogger<CleanTrickplayTask>> _loggerMock;
    private readonly CleanTrickplayTask _task;

    public CleanTrickplayTaskTests()
    {
        _libraryManagerMock = new Mock<ILibraryManager>();
        _fileSystemMock = new Mock<IFileSystem>();
        _loggerMock = new Mock<ILogger<CleanTrickplayTask>>();
        _task = new CleanTrickplayTask(_libraryManagerMock.Object, _fileSystemMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteInternalAsync_OrphanedFolder_DeletesFolder()
    {
        const string libraryPath = "C:\\Media";
        const string trickplayPath = "C:\\Media\\Movie.trickplay";
        
        var virtualFolder = new VirtualFolderInfo
        {
            Locations = [libraryPath]
        };
        _libraryManagerMock.Setup(m => m.GetVirtualFolders()).Returns([virtualFolder]);

        var trickplayDir = new FileSystemMetadata
        {
            FullName = trickplayPath,
            Name = "Movie.trickplay",
            IsDirectory = true
        };

        _fileSystemMock.Setup(f => f.GetDirectories(libraryPath, true)).Returns([trickplayDir]);
        _fileSystemMock.Setup(f => f.GetFiles(libraryPath, It.IsAny<bool>())).Returns(Array.Empty<FileSystemMetadata>());

        await _task.ExecuteInternalAsync(false, new Progress<double>(), CancellationToken.None);

        VerifyLogContains("Deleting orphaned trickplay folder", LogLevel.Information);
    }

    [Fact]
    public async Task ExecuteInternalAsync_MediaExists_DoesNotDelete()
    {
        const string libraryPath = "C:\\Media";
        const string trickplayPath = "C:\\Media\\Movie.trickplay";
        const string mediaPath = "C:\\Media\\Movie.mkv";
        
        var virtualFolder = new VirtualFolderInfo
        {
            Locations = [libraryPath]
        };
        _libraryManagerMock.Setup(m => m.GetVirtualFolders()).Returns([virtualFolder]);

        var trickplayDir = new FileSystemMetadata
        {
            FullName = trickplayPath,
            Name = "Movie.trickplay",
            IsDirectory = true
        };

        var mediaFile = new FileSystemMetadata
        {
            FullName = mediaPath,
            IsDirectory = false
        };

        _fileSystemMock.Setup(f => f.GetDirectories(libraryPath, true)).Returns([trickplayDir]);
        _fileSystemMock.Setup(f => f.GetFiles(libraryPath, It.IsAny<bool>())).Returns([mediaFile]);

        await _task.ExecuteInternalAsync(false, new Progress<double>(), CancellationToken.None);

        VerifyLogNeverContains("Deleting orphaned trickplay folder", LogLevel.Information);
    }

    [Fact]
    public async Task ExecuteInternalAsync_DryRun_LogsWouldDelete()
    {
        const string libraryPath = "C:\\Media";
        const string trickplayPath = "C:\\Media\\Movie.trickplay";
        
        var virtualFolder = new VirtualFolderInfo
        {
            Locations = [libraryPath]
        };
        _libraryManagerMock.Setup(m => m.GetVirtualFolders()).Returns([virtualFolder]);

        var trickplayDir = new FileSystemMetadata
        {
            FullName = trickplayPath,
            Name = "Movie.trickplay",
            IsDirectory = true
        };

        _fileSystemMock.Setup(f => f.GetDirectories(libraryPath, true)).Returns([trickplayDir]);
        _fileSystemMock.Setup(f => f.GetFiles(libraryPath, It.IsAny<bool>())).Returns([]);

        await _task.ExecuteInternalAsync(true, new Progress<double>(), CancellationToken.None);

        VerifyLogContains("[Dry Run] Would delete orphaned trickplay folder", LogLevel.Information);
        VerifyLogNeverContains("Deleting orphaned trickplay folder", LogLevel.Information);
    }

    private void VerifyLogContains(string messagePart, LogLevel level)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyLogNeverContains(string messagePart, LogLevel level)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
