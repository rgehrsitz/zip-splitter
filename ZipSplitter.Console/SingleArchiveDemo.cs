using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZipSplitter.Core;

namespace ZipSplitter.Console
{
    /// <summary>
    /// Demonstrates the enhanced ZIP splitter with single archive option.
    /// </summary>
    public static class SingleArchiveDemo
    {
        public static async Task RunSingleArchiveDemo()
        {
            System.Console.WriteLine("=== ZIP Splitter - Single Archive Demo ===\n");

            // Create a temporary test directory with some files
            string tempSourceDir = Path.Combine(Path.GetTempPath(), "ZipSplitterTest", "Source");
            string tempDestDir = Path.Combine(Path.GetTempPath(), "ZipSplitterTest", "Output");
            try
            {
                // Setup test data
                SetupTestData(tempSourceDir);

                System.Console.WriteLine($"Source directory: {tempSourceDir}");
                System.Console.WriteLine($"Output directory: {tempDestDir}\n");

                // Demo 1: Single Archive (regardless of size)
                await DemoSingleArchive(tempSourceDir, tempDestDir);

                // Demo 2: Split Archives with flexible large file handling
                await DemoSplitArchivesWithLargeFileHandling(tempSourceDir, tempDestDir);

                // Demo 3: Compressed size limit
                await DemoCompressedSizeLimit(tempSourceDir, tempDestDir);
            }
            finally
            {
                // Cleanup
                try
                {
                    if (Directory.Exists(Path.Combine(Path.GetTempPath(), "ZipSplitterTest")))
                    {
                        Directory.Delete(Path.Combine(Path.GetTempPath(), "ZipSplitterTest"), true);
                    }
                }
                catch
                { /* Ignore cleanup errors */
                }
            }
        }

        private static async Task DemoSingleArchive(string sourceDir, string destDir)
        {
            System.Console.WriteLine("=== Demo 1: Single Archive (All Files) ===");

            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                SingleArchiveName = "complete_backup.zip",
            };
            var progress = new Progress<ProgressInfo>(info =>
            {
                System.Console.Write(
                    $"\rProgress: {info.PercentageComplete:F1}% - {info.CurrentOperation}"
                );
            });

            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                sourceDir,
                Path.Combine(destDir, "SingleArchive"),
                options,
                progress,
                CancellationToken.None
            );
            System.Console.WriteLine($"\n✓ Created: {result.CreatedArchives[0]}");
            System.Console.WriteLine($"  Strategy: {result.StrategyUsed}");
            System.Console.WriteLine($"  Size: {result.TotalBytesProcessed:N0} bytes");
            System.Console.WriteLine($"  Duration: {result.Duration}\n");
        }

        private static async Task DemoSplitArchivesWithLargeFileHandling(
            string sourceDir,
            string destDir
        )
        {
            System.Console.WriteLine("=== Demo 2: Split Archives with Large File Handling ===");
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 1024 * 1024, // 1MB to demonstrate splitting with small files
                LargeFileHandling = LargeFileHandling.CreateSeparateArchive,
                SizeLimitType = SizeLimitType.UncompressedData,
            };
            var progress = new Progress<ProgressInfo>(info =>
            {
                System.Console.Write(
                    $"\rArchive {info.CurrentArchiveIndex}: {info.PercentageComplete:F1}%"
                );
            });

            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                sourceDir,
                Path.Combine(destDir, "SplitWithLargeFileHandling"),
                options,
                progress,
                CancellationToken.None
            );
            System.Console.WriteLine($"\n✓ Created {result.CreatedArchives.Count} archives");
            System.Console.WriteLine($"  Strategy: {result.StrategyUsed}");
            System.Console.WriteLine($"  Total Size: {result.TotalBytesProcessed:N0} bytes");

            if (result.HasWarnings)
            {
                System.Console.WriteLine(
                    $"  Special handling: {result.SpeciallyHandledFiles.Count} files"
                );
                foreach (var file in result.SpeciallyHandledFiles)
                {
                    System.Console.WriteLine(
                        $"    {file.HandlingMethod}: {Path.GetFileName(file.FilePath)}"
                    );
                }
            }
            System.Console.WriteLine();
        }

        private static async Task DemoCompressedSizeLimit(string sourceDir, string destDir)
        {
            System.Console.WriteLine("=== Demo 3: Compressed Size Limit ===");
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 2 * 1024 * 1024, // 2MB compressed size
                SizeLimitType = SizeLimitType.CompressedArchive,
                LargeFileHandling = LargeFileHandling.SkipFile,
                EstimatedCompressionRatio = 0.8, // Assume 20% compression
            };

            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                sourceDir,
                Path.Combine(destDir, "CompressedSizeLimit"),
                options
            );
            System.Console.WriteLine($"✓ Created {result.CreatedArchives.Count} archives");
            System.Console.WriteLine($"  Strategy: {result.StrategyUsed}");
            System.Console.WriteLine($"  Size Limit Type: {options.SizeLimitType}");
            System.Console.WriteLine(
                $"  Estimated Compression: {(1 - options.EstimatedCompressionRatio) * 100:F0}%"
            );

            if (result.SkippedFiles.Any())
            {
                System.Console.WriteLine($"  Skipped files: {result.SkippedFiles.Count()}");
            }
            System.Console.WriteLine();
        }

        private static void SetupTestData(string sourceDir)
        {
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);

            Directory.CreateDirectory(sourceDir);

            // Create some test files with different sizes
            File.WriteAllText(Path.Combine(sourceDir, "small1.txt"), new string('A', 5 * 1024)); // 5KB
            File.WriteAllText(Path.Combine(sourceDir, "small2.txt"), new string('B', 8 * 1024)); // 8KB
            File.WriteAllText(Path.Combine(sourceDir, "medium.txt"), new string('C', 25 * 1024)); // 25KB
            File.WriteAllText(Path.Combine(sourceDir, "large.txt"), new string('D', 100 * 1024)); // 100KB

            // Create subdirectory with files
            string subDir = Path.Combine(sourceDir, "subfolder");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "nested1.txt"), new string('E', 15 * 1024)); // 15KB
            File.WriteAllText(Path.Combine(subDir, "nested2.txt"), new string('F', 12 * 1024)); // 12KB

            System.Console.WriteLine("✓ Test data created");
        }
    }
}
