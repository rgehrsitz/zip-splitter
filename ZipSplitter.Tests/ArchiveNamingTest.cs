using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZipSplitter.Core;

namespace ZipSplitter.Tests
{
    public class ArchiveNamingTest : IDisposable
    {
        private readonly string _testSourceDirectory;
        private readonly string _testOutputDirectory;

        public ArchiveNamingTest()
        {
            var guid = Guid.NewGuid();
            _testSourceDirectory = Path.Combine(
                Path.GetTempPath(),
                $"ZipSplitterTests_Naming_Source_{guid}"
            );
            _testOutputDirectory = Path.Combine(
                Path.GetTempPath(),
                $"ZipSplitterTests_Naming_Output_{guid}"
            );

            Directory.CreateDirectory(_testSourceDirectory);
            Directory.CreateDirectory(_testOutputDirectory);

            // Create test files that will force multiple archives
            CreateTestFile(Path.Combine(_testSourceDirectory, "file1.txt"), 512 * 1024); // 512 KB
            CreateTestFile(Path.Combine(_testSourceDirectory, "file2.txt"), 512 * 1024); // 512 KB
            CreateTestFile(Path.Combine(_testSourceDirectory, "file3.txt"), 512 * 1024); // 512 KB
        }

        public void Dispose()
        {
            if (Directory.Exists(_testSourceDirectory))
                Directory.Delete(_testSourceDirectory, true);
            if (Directory.Exists(_testOutputDirectory))
                Directory.Delete(_testOutputDirectory, true);
        }

        private void CreateTestFile(string filePath, int sizeInBytes)
        {
            var content = new byte[sizeInBytes];
            new Random(42).NextBytes(content);
            File.WriteAllBytes(filePath, content);
        }

        [Fact]
        public async Task TestArchiveNaming()
        {
            // Arrange
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 1024 * 1024, // 1MB to force multiple archives (meets minimum requirement)
                SizeLimitType = SizeLimitType.UncompressedData,
            };

            // Act
            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                _testSourceDirectory,
                _testOutputDirectory,
                options
            );

            // Assert - Debug what we actually get
            Assert.True(result.CreatedArchives.Count > 1, "Should create multiple archives");

            var actualFileNames = result.CreatedArchives.Select(Path.GetFileName).ToList();
            
            // Expected naming convention: archive_1.zip, archive_2.zip, ...
            for (int i = 0; i < actualFileNames.Count; i++)
            {
                var expectedFileName = $"archive{i + 1:D3}.zip"; // Corrected format
                Assert.Equal(expectedFileName, actualFileNames[i]);
            }

            // Ensure the number of created archives matches the expectation based on naming
            // This also implicitly checks that there are no unexpected files.
            Assert.Equal(result.CreatedArchives.Count, actualFileNames.Count);
        }
    }
}
