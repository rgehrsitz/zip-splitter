using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZipSplitter.Core;

namespace ZipSplitter.Tests
{
    /// <summary>
    /// Comprehensive unit tests to achieve maximum code coverage
    /// </summary>
    public class ComprehensiveUnitTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _sourceDirectory;
        private readonly string _destinationDirectory;

        public ComprehensiveUnitTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"ZipSplitterComprehensive_{Guid.NewGuid()}");
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
                    // Ignore cleanup errors
                }
            }
        }

        #region FileHandlingInfo Tests

        [Fact]
        public void FileHandlingInfo_Constructor_SetsAllProperties()
        {
            // Arrange & Act
            var fileHandlingInfo = new FileHandlingInfo(
                "test/path/file.txt",
                1024L,
                LargeFileHandling.SkipFile,
                "output/path/file.txt",
                "File too large"
            );

            // Assert
            Assert.Equal("test/path/file.txt", fileHandlingInfo.FilePath);
            Assert.Equal(1024L, fileHandlingInfo.FileSizeBytes);
            Assert.Equal(LargeFileHandling.SkipFile, fileHandlingInfo.HandlingMethod);
            Assert.Equal("output/path/file.txt", fileHandlingInfo.OutputPath);
            Assert.Equal("File too large", fileHandlingInfo.Reason);
        }

        [Fact]
        public void FileHandlingInfo_Constructor_WithNullOutputPath_HandlesCorrectly()
        {
            // Arrange & Act
            var fileHandlingInfo = new FileHandlingInfo(
                "test/path/file.txt",
                1024L,
                LargeFileHandling.SkipFile,
                null,
                "File too large"
            );

            // Assert
            Assert.Null(fileHandlingInfo.OutputPath);
        }

        #endregion

        #region SplitResult Tests

        [Fact]
        public void SplitResult_DefaultConstructor_InitializesCollections()
        {
            // Arrange & Act
            var result = new SplitResult();

            // Assert
            Assert.NotNull(result.CreatedArchives);
            Assert.NotNull(result.SpeciallyHandledFiles);
            Assert.Empty(result.CreatedArchives);
            Assert.Empty(result.SpeciallyHandledFiles);
            Assert.Equal(0, result.TotalBytesProcessed);
            Assert.Equal(TimeSpan.Zero, result.Duration);
            Assert.False(result.HasWarnings);
        }

        [Fact]
        public void SplitResult_HasWarnings_WithSpeciallyHandledFiles_ReturnsTrue()
        {
            // Arrange
            var result = new SplitResult();
            result.SpeciallyHandledFiles.Add(new FileHandlingInfo(
                "test.txt", 1024, LargeFileHandling.SkipFile, null, "Test"));

            // Act & Assert
            Assert.True(result.HasWarnings);
        }

        [Fact]
        public void SplitResult_HasWarnings_WithoutSpeciallyHandledFiles_ReturnsFalse()
        {
            // Arrange
            var result = new SplitResult();

            // Act & Assert
            Assert.False(result.HasWarnings);
        }

        [Fact]
        public void SplitResult_SkippedFiles_ReturnsCorrectFiles()
        {
            // Arrange
            var result = new SplitResult();
            var skippedFile = new FileHandlingInfo("skipped.txt", 1024, LargeFileHandling.SkipFile, null, "Too large");
            var copiedFile = new FileHandlingInfo("copied.txt", 1024, LargeFileHandling.CopyUncompressed, "output.txt", "Copied");

            result.SpeciallyHandledFiles.Add(skippedFile);
            result.SpeciallyHandledFiles.Add(copiedFile);

            // Act
            var skippedFiles = result.SkippedFiles.ToList();

            // Assert
            Assert.Single(skippedFiles);
            Assert.Same(skippedFile, skippedFiles[0]);
        }

        [Fact]
        public void SplitResult_UncompressedFiles_ReturnsCorrectFiles()
        {
            // Arrange
            var result = new SplitResult();
            var skippedFile = new FileHandlingInfo("skipped.txt", 1024, LargeFileHandling.SkipFile, null, "Too large");
            var copiedFile = new FileHandlingInfo("copied.txt", 1024, LargeFileHandling.CopyUncompressed, "output.txt", "Copied");

            result.SpeciallyHandledFiles.Add(skippedFile);
            result.SpeciallyHandledFiles.Add(copiedFile);

            // Act
            var uncompressedFiles = result.UncompressedFiles.ToList();

            // Assert
            Assert.Single(uncompressedFiles);
            Assert.Same(copiedFile, uncompressedFiles[0]);
        }

        [Fact]
        public void SplitResult_SeparateArchiveFiles_ReturnsCorrectFiles()
        {
            // Arrange
            var result = new SplitResult();
            var skippedFile = new FileHandlingInfo("skipped.txt", 1024, LargeFileHandling.SkipFile, null, "Too large");
            var separateFile = new FileHandlingInfo("separate.txt", 1024, LargeFileHandling.CreateSeparateArchive, "archive.zip", "Separate");

            result.SpeciallyHandledFiles.Add(skippedFile);
            result.SpeciallyHandledFiles.Add(separateFile);

            // Act
            var separateArchiveFiles = result.SeparateArchiveFiles.ToList();

            // Assert
            Assert.Single(separateArchiveFiles);
            Assert.Same(separateFile, separateArchiveFiles[0]);
        }

        [Fact]
        public void SplitResult_ToString_WithoutWarnings_FormatsCorrectly()
        {
            // Arrange
            var result = new SplitResult
            {
                StrategyUsed = ArchiveStrategy.SingleArchive,
                TotalBytesProcessed = 1024,
                Duration = TimeSpan.FromSeconds(5)
            };
            result.CreatedArchives.Add("archive.zip");

            // Act
            var resultString = result.ToString();

            // Assert
            Assert.Contains("Strategy: SingleArchive", resultString);
            Assert.Contains("Archives: 1", resultString);
            Assert.Contains("Total Size: 1,024 bytes", resultString);
            Assert.Contains("Duration: 00:00:05", resultString);
            Assert.DoesNotContain("Warnings", resultString);
        }

        [Fact]
        public void SplitResult_ToString_WithWarnings_FormatsCorrectly()
        {
            // Arrange
            var result = new SplitResult
            {
                StrategyUsed = ArchiveStrategy.SplitBySize,
                TotalBytesProcessed = 2048,
                Duration = TimeSpan.FromSeconds(10)
            };
            result.CreatedArchives.Add("archive001.zip");
            result.CreatedArchives.Add("archive002.zip");
            result.SpeciallyHandledFiles.Add(new FileHandlingInfo("large.txt", 1024, LargeFileHandling.SkipFile, null, "Too large"));

            // Act
            var resultString = result.ToString();

            // Assert
            Assert.Contains("Strategy: SplitBySize", resultString);
            Assert.Contains("Archives: 2", resultString);
            Assert.Contains("Total Size: 2,048 bytes", resultString);
            Assert.Contains("Duration: 00:00:10", resultString);
            Assert.Contains("Warnings: 1", resultString);
        }

        #endregion

        #region SplitOptions Validation Tests

        [Fact]
        public void SplitOptions_Validate_SplitBySizeWithValidMaxSize_DoesNotThrow()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 1024 * 1024 // 1MB
            };

            // Act & Assert
            var exception = Record.Exception(() => options.Validate());
            Assert.Null(exception);
        }

        [Fact]
        public void SplitOptions_Validate_SplitBySizeWithInvalidMaxSize_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 512 * 1024 // 512KB - below minimum
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.Equal("MaxSizeBytes", exception.ParamName);
            Assert.Contains("Maximum size must be at least 1MB", exception.Message);
        }

        [Fact]
        public void SplitOptions_Validate_InvalidCompressionRatio_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 1024 * 1024,
                EstimatedCompressionRatio = 1.5 // Invalid - greater than 1.0
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.Equal("EstimatedCompressionRatio", exception.ParamName);
            Assert.Contains("Compression ratio must be between 0 and 1.0", exception.Message);
        }

        [Fact]
        public void SplitOptions_Validate_ZeroCompressionRatio_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 1024 * 1024,
                EstimatedCompressionRatio = 0.0 // Invalid - zero
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.Equal("EstimatedCompressionRatio", exception.ParamName);
        }

        [Fact]
        public void SplitOptions_Validate_NullSingleArchiveName_ThrowsArgumentException()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                SingleArchiveName = null!
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Equal("SingleArchiveName", exception.ParamName);
            Assert.Contains("Single archive name cannot be null or empty", exception.Message);
        }

        [Fact]
        public void SplitOptions_Validate_EmptySingleArchiveName_ThrowsArgumentException()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                SingleArchiveName = ""
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Equal("SingleArchiveName", exception.ParamName);
        }

        [Fact]
        public void SplitOptions_Validate_WhitespaceSingleArchiveName_ThrowsArgumentException()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                SingleArchiveName = "   "
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Equal("SingleArchiveName", exception.ParamName);
        }

        [Fact]
        public void SplitOptions_Validate_SingleArchiveNameWithoutZipExtension_ThrowsArgumentException()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                SingleArchiveName = "archive.tar"
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Equal("SingleArchiveName", exception.ParamName);
            Assert.Contains("Single archive name must end with .zip extension", exception.Message);
        }

        [Fact]
        public void SplitOptions_Validate_SingleArchiveNameWithZipExtension_DoesNotThrow()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                SingleArchiveName = "archive.zip"
            };

            // Act & Assert
            var exception = Record.Exception(() => options.Validate());
            Assert.Null(exception);
        }

        [Fact]
        public void SplitOptions_Validate_SingleArchiveNameWithZipExtensionCaseInsensitive_DoesNotThrow()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                SingleArchiveName = "archive.ZIP"
            };

            // Act & Assert
            var exception = Record.Exception(() => options.Validate());
            Assert.Null(exception);
        }

        [Fact]
        public void SplitOptions_Validate_SingleArchiveStrategy_IgnoresSizeValidation()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                MaxSizeBytes = 512 * 1024, // Below minimum, but should be ignored
                SingleArchiveName = "archive.zip"
            };

            // Act & Assert
            var exception = Record.Exception(() => options.Validate());
            Assert.Null(exception);
        }

        #endregion

        #region ZipSplitterWithProgress Edge Case Tests

        [Fact]
        public async Task CreateArchivesAsync_EmptyDirectory_ReturnsEmptyResult()
        {
            // Arrange
            var options = new SplitOptions { ArchiveStrategy = ArchiveStrategy.SingleArchive };

            // Act
            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                _sourceDirectory, _destinationDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.CreatedArchives);
            Assert.Equal(0, result.TotalBytesProcessed);
            Assert.False(result.HasWarnings);
            Assert.Equal(ArchiveStrategy.SingleArchive, result.StrategyUsed);
        }

        [Fact]
        public async Task CreateArchivesAsync_SingleArchiveWithLargeFile_HandlesCorrectly()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "large.txt"), 2 * 1024 * 1024); // 2MB
            var options = new SplitOptions { ArchiveStrategy = ArchiveStrategy.SingleArchive };

            // Act
            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                _sourceDirectory, _destinationDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.CreatedArchives);
            Assert.True(result.TotalBytesProcessed > 0);
            Assert.False(result.HasWarnings);
        }

        [Fact]
        public async Task CreateArchivesAsync_SplitBySizeWithAllLargeFiles_HandlesCorrectly()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "large1.txt"), 2 * 1024 * 1024); // 2MB
            CreateTestFile(Path.Combine(_sourceDirectory, "large2.txt"), 2 * 1024 * 1024); // 2MB
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 1024 * 1024, // 1MB
                LargeFileHandling = LargeFileHandling.CreateSeparateArchive
            };

            // Act
            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                _sourceDirectory, _destinationDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreatedArchives.Count >= 2); // Should create separate archives for large files
            Assert.True(result.HasWarnings);
            Assert.Equal(2, result.SpeciallyHandledFiles.Count);
            Assert.All(result.SpeciallyHandledFiles, f =>
                Assert.Equal(LargeFileHandling.CreateSeparateArchive, f.HandlingMethod));
        }

        [Fact]
        public async Task CreateArchivesAsync_CompressedSizeLimit_CalculatesCorrectly()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "file1.txt"), 800 * 1024); // 800KB
            CreateTestFile(Path.Combine(_sourceDirectory, "file2.txt"), 800 * 1024); // 800KB
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 1024 * 1024, // 1MB compressed limit
                SizeLimitType = SizeLimitType.CompressedArchive,
                EstimatedCompressionRatio = 0.5 // 50% compression
            };

            // Act
            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                _sourceDirectory, _destinationDirectory, options);

            // Assert
            Assert.NotNull(result);
            // With 50% compression ratio, 800KB files should compress to ~400KB each
            // So both files should fit in one archive under 1MB compressed limit
            Assert.Single(result.CreatedArchives);
        }

        [Fact]
        public async Task CreateArchivesAsync_ProgressReporting_ReportsAllStages()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "file1.txt"), 100 * 1024);
            CreateTestFile(Path.Combine(_sourceDirectory, "file2.txt"), 100 * 1024);
            var progressReports = new List<ProgressInfo>();
            var progress = new Progress<ProgressInfo>(report => progressReports.Add(report));
            var options = new SplitOptions { ArchiveStrategy = ArchiveStrategy.SingleArchive };

            // Act
            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                _sourceDirectory, _destinationDirectory, options, progress);

            // Assert
            Assert.True(progressReports.Count > 0);
            Assert.Contains(progressReports, r => r.PercentageComplete == 0);
            Assert.Contains(progressReports, r => r.PercentageComplete == 100);
            Assert.All(progressReports, r =>
            {
                Assert.True(r.PercentageComplete >= 0 && r.PercentageComplete <= 100);
                Assert.NotNull(r.CurrentOperation);
                Assert.True(r.BytesProcessed >= 0);
            });
        }

        [Fact]
        public async Task CreateArchivesAsync_CancellationDuringProcessing_ThrowsOperationCanceledException()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "large.txt"), 10 * 1024 * 1024); // 10MB
            var options = new SplitOptions { ArchiveStrategy = ArchiveStrategy.SingleArchive };
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                ZipSplitterWithProgress.CreateArchivesAsync(
                    _sourceDirectory, _destinationDirectory, options,
                    cancellationToken: cts.Token));
        }

        [Fact]
        public async Task CreateArchivesAsync_NonExistentSource_ThrowsArgumentException()
        {
            // Arrange
            var nonExistentDir = Path.Combine(_testDirectory, "NonExistent");
            var options = new SplitOptions { ArchiveStrategy = ArchiveStrategy.SingleArchive };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                ZipSplitterWithProgress.CreateArchivesAsync(
                    nonExistentDir, _destinationDirectory, options));

            Assert.Equal("sourceDirectory", exception.ParamName);
            Assert.Contains("Source directory does not exist", exception.Message);
        }

        [Fact]
        public async Task CreateArchivesAsync_InvalidOptions_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            CreateTestFile(Path.Combine(_sourceDirectory, "test.txt"), 1024);
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 512 * 1024 // Below minimum
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                ZipSplitterWithProgress.CreateArchivesAsync(
                    _sourceDirectory, _destinationDirectory, options));

            Assert.Equal("MaxSizeBytes", exception.ParamName);
        }

        [Fact]
        public async Task CreateArchivesAsync_DestinationDirectoryCreated_IfNotExists()
        {
            // Arrange
            var newDestination = Path.Combine(_testDirectory, "NewDestination");
            CreateTestFile(Path.Combine(_sourceDirectory, "test.txt"), 1024);
            var options = new SplitOptions { ArchiveStrategy = ArchiveStrategy.SingleArchive };

            // Act
            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                _sourceDirectory, newDestination, options);

            // Assert
            Assert.True(Directory.Exists(newDestination));
            Assert.Single(result.CreatedArchives);
        }

        #endregion

        #region Helper Methods

        private void CreateTestFile(string filePath, int sizeInBytes)
        {
            var random = new Random(42); // Fixed seed for reproducible tests
            var buffer = new byte[Math.Min(sizeInBytes, 8192)];
            random.NextBytes(buffer);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            int bytesWritten = 0;
            while (bytesWritten < sizeInBytes)
            {
                int bytesToWrite = Math.Min(buffer.Length, sizeInBytes - bytesWritten);
                fileStream.Write(buffer, 0, bytesToWrite);
                bytesWritten += bytesToWrite;
            }
        }

        #endregion
    }
}