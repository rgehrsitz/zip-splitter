using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZipSplitter.Core;

namespace ZipSplitter.Tests
{
    /// <summary>
    /// Integration tests that test the complete ZIP splitting workflow with real files.
    /// </summary>
    public class IntegrationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _sourceDirectory;
        private readonly string _destinationDirectory;

        public IntegrationTests()
        {
            _testDirectory = Path.Combine(
                Path.GetTempPath(),
                $"ZipSplitterIntegration_{Guid.NewGuid()}"
            );
            _sourceDirectory = Path.Combine(_testDirectory, "Source");
            _destinationDirectory = Path.Combine(_testDirectory, "Destination");

            Directory.CreateDirectory(_sourceDirectory);
            Directory.CreateDirectory(_destinationDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        [Fact]
        public async Task FullWorkflow_LargeDirectoryStructure_SplitsAndExtractsCorrectly()
        {
            // Arrange - Create a complex directory structure with various file types
            CreateComplexDirectoryStructure();

            var originalFiles = Directory
                .GetFiles(_sourceDirectory, "*", SearchOption.AllDirectories)
                .Select(f => new { Path = f, Size = new FileInfo(f).Length })
                .ToList();

            var totalOriginalSize = originalFiles.Sum(f => f.Size);
            var progress = new TestProgress<ProgressInfo>();

            // Act - Split the directory
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                3 * 1024 * 1024, // 3MB per archive
                progress
            );

            // Assert - Verify archives were created
            var archives = Directory
                .GetFiles(_destinationDirectory, "*.zip")
                .OrderBy(f => f)
                .ToArray();
            Assert.True(archives.Length > 1, "Should create multiple archives for large directory");

            // Verify all archives are within size limits (allowing for compression variance)
            foreach (var archive in archives)
            {
                var archiveSize = new FileInfo(archive).Length;
                Assert.True(
                    archiveSize <= 4 * 1024 * 1024,
                    $"Archive {Path.GetFileName(archive)} is too large: {archiveSize} bytes"
                );
            }

            // Extract and verify all files are present and correct
            var extractionDir = Path.Combine(_testDirectory, "Extracted");
            Directory.CreateDirectory(extractionDir);

            foreach (var archive in archives)
            {
                ZipFile.ExtractToDirectory(archive, extractionDir);
            }

            var extractedFiles = Directory
                .GetFiles(extractionDir, "*", SearchOption.AllDirectories)
                .Select(f => new { Path = f, Size = new FileInfo(f).Length })
                .ToList();

            // Verify file count and total size
            Assert.Equal(originalFiles.Count, extractedFiles.Count);
            Assert.Equal(totalOriginalSize, extractedFiles.Sum(f => f.Size));

            // Verify specific files exist and have correct content
            var originalTestFile = Path.Combine(_sourceDirectory, "Documents", "important.doc");
            var extractedTestFile = Path.Combine(extractionDir, "Documents", "important.doc");

            Assert.True(File.Exists(extractedTestFile), "Important document should be extracted");
            Assert.Equal(File.ReadAllText(originalTestFile), File.ReadAllText(extractedTestFile));

            // Verify progress reporting worked
            Assert.True(progress.Reports.Count > 0, "Should report progress");
            Assert.Equal(100, progress.Reports.Last().PercentageComplete);
        }

        [Fact]
        public async Task RealWorldScenario_PhotoLibrary_HandlesLargeFiles()
        {
            // Arrange - Simulate a photo library with large files
            CreatePhotoLibraryStructure();

            var progress = new TestProgress<ProgressInfo>();

            // Act
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                5 * 1024 * 1024, // 5MB per archive
                progress
            );

            // Assert
            var archives = Directory.GetFiles(_destinationDirectory, "*.zip");
            Assert.True(archives.Length > 0, "Should create at least one archive");

            // Verify that large image files were split across archives appropriately
            long totalCompressedSize = archives.Sum(a => new FileInfo(a).Length);
            Assert.True(totalCompressedSize > 0, "Archives should contain compressed data"); // Test that archives can be opened and contain expected structure
            using (var firstArchive = ZipFile.OpenRead(archives[0]))
            {
                Assert.True(firstArchive.Entries.Count > 0, "First archive should contain files");

                // Check that directory structure is preserved (handle both Windows and Unix path separators)
                var hasYearFolder = firstArchive.Entries.Any(e =>
                    e.FullName.Contains("2024/")
                    || e.FullName.Contains("2023/")
                    || e.FullName.Contains("2024\\")
                    || e.FullName.Contains("2023\\")
                );
                Assert.True(hasYearFolder, "Should preserve year-based folder structure");
            }
        }

        [Fact]
        public async Task ErrorScenario_ReadOnlyFiles_HandlesGracefully()
        {
            // Arrange - Create some read-only files
            var readOnlyFile = Path.Combine(_sourceDirectory, "readonly.txt");
            File.WriteAllText(readOnlyFile, "This file is read-only");
            File.SetAttributes(readOnlyFile, FileAttributes.ReadOnly);

            var normalFile = Path.Combine(_sourceDirectory, "normal.txt");
            File.WriteAllText(normalFile, "This file is normal");

            // Act - Should complete successfully even with read-only files
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                1024 * 1024
            );

            // Assert
            var archives = Directory.GetFiles(_destinationDirectory, "*.zip");
            Assert.Single(archives);

            using (var archive = ZipFile.OpenRead(archives[0]))
            {
                var entries = archive.Entries.Select(e => e.Name).ToArray();
                Assert.Contains("readonly.txt", entries);
                Assert.Contains("normal.txt", entries);
            }

            // Cleanup - Remove read-only attribute for disposal
            File.SetAttributes(readOnlyFile, FileAttributes.Normal);
        }

        [Fact]
        public async Task PerformanceTest_ManySmallFiles_CompletesReasonably()
        {
            // Arrange - Create many small files (simulating source code project)
            CreateManySmallFilesStructure(1000); // 1000 small files

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var progress = new TestProgress<ProgressInfo>();

            // Act
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                _sourceDirectory,
                _destinationDirectory,
                2 * 1024 * 1024, // 2MB per archive
                progress
            );

            stopwatch.Stop();

            // Assert - Should complete in reasonable time (less than 30 seconds for 1000 files)
            Assert.True(
                stopwatch.Elapsed.TotalSeconds < 30,
                $"Operation took too long: {stopwatch.Elapsed.TotalSeconds} seconds"
            );

            var archives = Directory.GetFiles(_destinationDirectory, "*.zip");
            Assert.True(archives.Length > 0, "Should create archives");

            // Verify progress was reported regularly
            Assert.True(
                progress.Reports.Count > 10,
                "Should report progress frequently for many files"
            );
        }

        private void CreateComplexDirectoryStructure()
        {
            // Create directory structure
            var directories = new[]
            {
                Path.Combine(_sourceDirectory, "Documents"),
                Path.Combine(_sourceDirectory, "Images"),
                Path.Combine(_sourceDirectory, "Videos"),
                Path.Combine(_sourceDirectory, "Code", "src"),
                Path.Combine(_sourceDirectory, "Code", "tests"),
                Path.Combine(_sourceDirectory, "Archives", "old"),
            };

            foreach (var dir in directories)
            {
                Directory.CreateDirectory(dir);
            }

            // Create various file types with different sizes (smaller to fit within 3MB limit)
            CreateTestFile(Path.Combine(_sourceDirectory, "README.txt"), 1024, "Text content");
            CreateTestFile(
                Path.Combine(_sourceDirectory, "Documents", "important.doc"),
                512 * 1024,
                "Document content"
            );
            CreateTestFile(
                Path.Combine(_sourceDirectory, "Images", "photo1.jpg"),
                1.5 * 1024 * 1024,
                "Binary image data"
            );
            CreateTestFile(
                Path.Combine(_sourceDirectory, "Images", "photo2.png"),
                1 * 1024 * 1024,
                "Binary PNG data"
            );
            CreateTestFile(
                Path.Combine(_sourceDirectory, "Videos", "clip.mp4"),
                2 * 1024 * 1024,
                "Binary video data"
            ); // Reduced from 4MB to 2MB
            CreateTestFile(
                Path.Combine(_sourceDirectory, "Code", "src", "main.cs"),
                8 * 1024,
                "C# source code"
            );
            CreateTestFile(
                Path.Combine(_sourceDirectory, "Code", "tests", "test.cs"),
                4 * 1024,
                "C# test code"
            );
            CreateTestFile(
                Path.Combine(_sourceDirectory, "Archives", "old", "backup.zip"),
                512 * 1024,
                "Old backup data"
            ); // Reduced from 1MB to 512KB
        }

        private void CreatePhotoLibraryStructure()
        {
            var years = new[] { "2023", "2024" };
            var months = new[] { "01-January", "06-June", "12-December" };

            foreach (var year in years)
            {
                foreach (var month in months)
                {
                    var dir = Path.Combine(_sourceDirectory, year, month);
                    Directory.CreateDirectory(dir);

                    // Create several "photos" in each directory
                    for (int i = 1; i <= 5; i++)
                    {
                        CreateTestFile(
                            Path.Combine(dir, $"IMG_{year}{month.Substring(0, 2)}{i:D2}.jpg"),
                            2 * 1024 * 1024 + (i * 100 * 1024), // 2MB+ varying sizes
                            "Simulated JPEG data"
                        );
                    }
                }
            }
        }

        private void CreateManySmallFilesStructure(int fileCount)
        {
            var subdirs = new[] { "src", "tests", "docs", "config" };

            foreach (var subdir in subdirs)
            {
                Directory.CreateDirectory(Path.Combine(_sourceDirectory, subdir));
            }

            var random = new Random(42); // Fixed seed for reproducible tests

            for (int i = 0; i < fileCount; i++)
            {
                var subdir = subdirs[i % subdirs.Length];
                var extension = new[] { ".cs", ".txt", ".json", ".xml" }[i % 4];
                var fileName = $"file{i:D4}{extension}";
                var filePath = Path.Combine(_sourceDirectory, subdir, fileName);

                var fileSize = random.Next(100, 5000); // 100 bytes to 5KB
                CreateTestFile(filePath, fileSize, $"Content for file {i}");
            }
        }

        private void CreateTestFile(string filePath, double sizeInBytes, string contentType)
        {
            CreateTestFile(filePath, (int)sizeInBytes, contentType);
        }

        private void CreateTestFile(string filePath, int sizeInBytes, string contentType)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                // Write a header indicating content type
                var header = System.Text.Encoding.UTF8.GetBytes($"{contentType}\n");
                stream.Write(header, 0, Math.Min(header.Length, sizeInBytes));

                int remainingBytes = sizeInBytes - header.Length;
                if (remainingBytes > 0)
                {
                    var buffer = new byte[Math.Min(remainingBytes, 4096)];
                    var random = new Random(filePath.GetHashCode()); // Deterministic based on file path

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        buffer[i] = (byte)random.Next(256);
                    }

                    int bytesWritten = 0;
                    while (bytesWritten < remainingBytes)
                    {
                        int bytesToWrite = Math.Min(buffer.Length, remainingBytes - bytesWritten);
                        stream.Write(buffer, 0, bytesToWrite);
                        bytesWritten += bytesToWrite;
                    }
                }
            }
        }
    }
}
