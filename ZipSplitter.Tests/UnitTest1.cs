using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZipSplitter.Core;

namespace ZipSplitter.Tests
{
    public class ZipSplitterWithProgressTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _sourceDirectory;
        private readonly string _destinationDirectory;

        public ZipSplitterWithProgressTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"ZipSplitterTests_{Guid.NewGuid()}");
            _sourceDirectory = Path.Combine(_testDirectory, "Source");
            _destinationDirectory = Path.Combine(_testDirectory, "Destination");

            Directory.CreateDirectory(_sourceDirectory);
            Directory.CreateDirectory(_destinationDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_EmptyDirectory_CompletesSuccessfully()
        {
            // Arrange
            var progress = new TestProgress<ProgressInfo>();

            // Act
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                1024 * 1024, // 1MB
                progress
            );

            // Assert
            Assert.Single(progress.Reports);
            Assert.Equal(100, progress.Reports[0].PercentageComplete);
            Assert.Equal("No files to compress", progress.Reports[0].CurrentOperation);
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_SingleSmallFile_CreatesSingleArchive()
        {
            // Arrange
            var testFile = Path.Combine(_sourceDirectory, "test.txt");
            File.WriteAllText(testFile, "Hello, World!");
            var progress = new TestProgress<ProgressInfo>();

            // Act
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                1024 * 1024, // 1MB
                progress
            );

            // Assert
            var zipFiles = Directory.GetFiles(_destinationDirectory, "*.zip");
            Assert.Single(zipFiles);
            Assert.Equal("archive001.zip", Path.GetFileName(zipFiles[0]));

            // Verify content
            using (var archive = ZipFile.OpenRead(zipFiles[0]))
            {
                Assert.Single(archive.Entries);
                Assert.Equal("test.txt", archive.Entries[0].Name);
            }
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_MultipleFiles_SplitsCorrectly()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "file1.txt"), 600 * 1024); // 600KB
            CreateTestFile(Path.Combine(_sourceDirectory, "file2.txt"), 600 * 1024); // 600KB
            CreateTestFile(Path.Combine(_sourceDirectory, "file3.txt"), 600 * 1024); // 600KB

            var progress = new TestProgress<ProgressInfo>();

            // Act
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                1024 * 1024, // 1MB max per archive
                progress
            );

            // Assert
            var zipFiles = Directory
                .GetFiles(_destinationDirectory, "*.zip")
                .OrderBy(f => f)
                .ToArray();
            Assert.True(zipFiles.Length >= 2, "Should create at least 2 archives");

            // Verify archives are reasonably sized
            foreach (var zipFile in zipFiles)
            {
                var fileInfo = new FileInfo(zipFile);
                Assert.True(
                    fileInfo.Length <= 1024 * 1024 * 1.1,
                    "Archive should not significantly exceed size limit"
                ); // Allow 10% compression variance
            }
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_NestedDirectories_PreservesStructure()
        {
            // Arrange
            var subDir = Path.Combine(_sourceDirectory, "SubDirectory");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(_sourceDirectory, "root.txt"), "Root file");
            File.WriteAllText(Path.Combine(subDir, "sub.txt"), "Sub file");

            var progress = new TestProgress<ProgressInfo>();

            // Act
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                1024 * 1024,
                progress
            ); // Assert
            var zipFiles = Directory.GetFiles(_destinationDirectory, "*.zip");
            Assert.Single(zipFiles);

            using (var archive = ZipFile.OpenRead(zipFiles[0]))
            {
                var entryNames = archive.Entries.Select(e => e.FullName).ToArray();
                Assert.Contains("root.txt", entryNames);
                // Check for the subdirectory entry - it could be either forward or backslash
                var hasSubDirectoryEntry = entryNames.Any(name =>
                    name == "SubDirectory/sub.txt" || name == "SubDirectory\\sub.txt"
                );
                Assert.True(
                    hasSubDirectoryEntry,
                    $"Should contain SubDirectory/sub.txt or SubDirectory\\sub.txt. Found: {string.Join(", ", entryNames)}"
                );
            }
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "large.txt"), 1024 * 1024); // 1MB
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    _sourceDirectory,
                    _destinationDirectory,
                    1024 * 1024,
                    null,
                    cancellationTokenSource.Token
                )
            );
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_NonExistentSourceDirectory_ThrowsArgumentException()
        {
            // Arrange
            var nonExistentDir = Path.Combine(_testDirectory, "NonExistent");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    nonExistentDir,
                    _destinationDirectory,
                    1024 * 1024
                )
            );

            Assert.Contains("Source directory does not exist", exception.Message);
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_MaxSizeTooSmall_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_sourceDirectory, "test.txt"), "test");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    _sourceDirectory,
                    _destinationDirectory,
                    1024
                )
            ); // 1KB - too small
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_FileLargerThanMaxSize_ThrowsInvalidOperationException()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "huge.txt"), 2 * 1024 * 1024); // 2MB

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    _sourceDirectory,
                    _destinationDirectory,
                    1024 * 1024
                )
            ); // 1MB max

            Assert.Contains("exceeds maximum archive size", exception.Message);
        }

        [Fact]
        public async Task CreateSplitArchivesWithProgressAsync_ProgressReporting_ReportsCorrectly()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "file.txt"), 1024); // 1KB
            var progress = new TestProgress<ProgressInfo>();

            // Act
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                1024 * 1024,
                progress
            );

            // Assert
            Assert.True(progress.Reports.Count > 0, "Should report progress");
            Assert.Equal(100, progress.Reports.Last().PercentageComplete);
            Assert.Equal("Compression completed", progress.Reports.Last().CurrentOperation);
        }

        [Fact]
        public void ProgressInfo_ToString_FormatsCorrectly()
        {
            // Arrange
            var progressInfo = new ProgressInfo(75.5, 2, 1024000, "Processing file.txt");

            // Act
            var result = progressInfo.ToString();

            // Assert
            Assert.Contains("75.5%", result);
            Assert.Contains("Archive 2", result);
            Assert.Contains("1,024,000 bytes", result);
            Assert.Contains("Processing file.txt", result);
        }

        private void CreateTestFile(string filePath, int sizeInBytes)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                var buffer = new byte[Math.Min(sizeInBytes, 4096)];
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)(i % 256);
                }

                int bytesWritten = 0;
                while (bytesWritten < sizeInBytes)
                {
                    int bytesToWrite = Math.Min(buffer.Length, sizeInBytes - bytesWritten);
                    stream.Write(buffer, 0, bytesToWrite);
                    bytesWritten += bytesToWrite;
                }
            }
        }
    }

    public class TestProgress<T> : IProgress<T>
    {
        public List<T> Reports { get; } = new List<T>();

        public void Report(T value)
        {
            Reports.Add(value);
        }
    }
}
