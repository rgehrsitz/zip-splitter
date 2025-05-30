using System;
using System.Threading;
using System.Threading.Tasks;
using ZipSplitter.Core;

namespace ZipSplitter.Examples
{
    /// <summary>
    /// Examples demonstrating the enhanced ZIP splitter functionality.
    /// </summary>
    public static class EnhancedUsageExamples
    {
        /// <summary>
        /// Example: Create a single archive regardless of size.
        /// </summary>
        public static async Task CreateSingleArchiveExample()
        {
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SingleArchive,
                SingleArchiveName = "complete_backup.zip", // Output file will be "complete_backup.zip"
            };

            var progress = new Progress<ProgressInfo>(info =>
            {
                Console.WriteLine(
                    $"Progress: {info.PercentageComplete:F1}% - {info.CurrentOperation}"
                );
            });

            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                @"C:\MyDataFolder",
                @"C:\Backups",
                options,
                progress,
                CancellationToken.None
            );

            Console.WriteLine($"Created single archive: {result.CreatedArchives[0]}");
            Console.WriteLine($"Total size processed: {result.TotalBytesProcessed:N0} bytes");
            Console.WriteLine($"Duration: {result.Duration}");
        }

        /// <summary>
        /// Example: Split archives with compressed size limit and handle large files gracefully.
        /// </summary>
        public static async Task CreateSplitArchivesWithCompressedSizeLimitExample()
        {
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize, // Archives will be named e.g., archive001.zip, archive002.zip
                MaxSizeBytes = 50 * 1024 * 1024, // 50MB
                SizeLimitType = SizeLimitType.CompressedArchive, // Limit final ZIP size
                LargeFileHandling = LargeFileHandling.CreateSeparateArchive, // Large files: large_file_originalfilename.zip
                EstimatedCompressionRatio = 0.6, // Assume 40% compression
            };

            var progress = new Progress<ProgressInfo>(info =>
            {
                Console.WriteLine(
                    $"Archive {info.CurrentArchiveIndex}: {info.PercentageComplete:F1}% - {info.CurrentOperation}"
                );
            });

            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                @"C:\MyDataFolder",
                @"C:\Backups",
                options,
                progress,
                CancellationToken.None
            );

            Console.WriteLine($"Created {result.CreatedArchives.Count} archives");

            if (result.HasWarnings)
            {
                Console.WriteLine("\nSpecial file handling:");
                foreach (var file in result.SpeciallyHandledFiles)
                {
                    Console.WriteLine(
                        $"  {file.HandlingMethod}: {file.FilePath} -> {file.OutputPath ?? "N/A"}"
                    );
                    Console.WriteLine($"    Reason: {file.Reason}");
                }
            }
        }

        /// <summary>
        /// Example: Handle large files by copying them uncompressed.
        /// </summary>
        public static async Task HandleLargeFilesByCopyingExample()
        {
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize, // Archives will be named e.g., archive001.zip
                MaxSizeBytes = 100 * 1024 * 1024, // 100MB
                LargeFileHandling = LargeFileHandling.CopyUncompressed, // Large files are copied, not zipped.
                SizeLimitType = SizeLimitType.UncompressedData,
            };

            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                @"C:\MyDataFolder",
                @"C:\Backups",
                options
            );

            Console.WriteLine($"Archives created: {result.CreatedArchives.Count}");
            Console.WriteLine($"Files copied uncompressed: {result.UncompressedFiles.Count()}");

            foreach (var file in result.UncompressedFiles)
            {
                Console.WriteLine($"  Copied: {file.FilePath} ({file.FileSizeBytes:N0} bytes)");
            }
        }

        /// <summary>
        /// Example: Skip large files and report them.
        /// </summary>
        public static async Task SkipLargeFilesExample()
        {
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize, // Archives will be named e.g., archive001.zip
                MaxSizeBytes = 10 * 1024 * 1024, // 10MB
                LargeFileHandling = LargeFileHandling.SkipFile, // Large files are skipped.
            };

            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                @"C:\MyDataFolder",
                @"C:\Backups",
                options
            );

            Console.WriteLine($"Archives created: {result.CreatedArchives.Count}");

            if (result.SkippedFiles.Any())
            {
                Console.WriteLine($"\nSkipped {result.SkippedFiles.Count()} large files:");
                foreach (var file in result.SkippedFiles)
                {
                    Console.WriteLine($"  {file.FilePath} ({file.FileSizeBytes:N0} bytes)");
                }
            }
        }

        /// <summary>
        /// Example: Backward compatibility - using the original method signature.
        /// </summary>
        public static async Task BackwardCompatibilityExample()
        {
            var progress = new Progress<ProgressInfo>(info =>
            {
                Console.WriteLine($"Progress: {info.PercentageComplete:F1}%");
            });

            // This still works as before
            await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                @"C:\MyDataFolder",
                @"C:\Backups",
                50 * 1024 * 1024, // 50MB
                progress,
                CancellationToken.None
            );

            Console.WriteLine("Backward compatibility example completed");
        }

        /// <summary>
        /// Example: Comprehensive demonstration with detailed result analysis.
        /// </summary>
        public static async Task ComprehensiveExample()
        {
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = 25 * 1024 * 1024, // 25MB
                SizeLimitType = SizeLimitType.CompressedArchive,
                LargeFileHandling = LargeFileHandling.CreateSeparateArchive,
                EstimatedCompressionRatio = 0.7,
            };

            var progress = new Progress<ProgressInfo>(info =>
            {
                // Update progress every 80KB for smooth UI updates
                if (info.BytesProcessed % (80 * 1024) == 0 || info.PercentageComplete >= 100)
                {
                    Console.Write(
                        $"\rProgress: {info.PercentageComplete:F1}% | Archive {info.CurrentArchiveIndex} | {info.BytesProcessed:N0} bytes"
                    );
                }
            });

            Console.WriteLine("Starting comprehensive ZIP operation...");
            var result = await ZipSplitterWithProgress.CreateArchivesAsync(
                @"C:\MyDataFolder",
                @"C:\Backups",
                options,
                progress,
                CancellationToken.None
            );

            Console.WriteLine($"\n\n=== OPERATION COMPLETE ===");
            Console.WriteLine($"Strategy Used: {result.StrategyUsed}");
            Console.WriteLine($"Total Archives: {result.CreatedArchives.Count}");
            Console.WriteLine($"Total Data Processed: {result.TotalBytesProcessed:N0} bytes");
            Console.WriteLine($"Duration: {result.Duration}");

            if (result.HasWarnings)
            {
                Console.WriteLine(
                    $"\n=== SPECIAL FILE HANDLING ({result.SpeciallyHandledFiles.Count}) ==="
                );
                foreach (var file in result.SpeciallyHandledFiles)
                {
                    Console.WriteLine($"Method: {file.HandlingMethod}");
                    Console.WriteLine($"File: {file.FilePath}");
                    Console.WriteLine($"Size: {file.FileSizeBytes:N0} bytes");
                    Console.WriteLine($"Output: {file.OutputPath ?? "N/A"}");
                    Console.WriteLine($"Reason: {file.Reason}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("\n=== CREATED ARCHIVES ===");
            for (int i = 0; i < result.CreatedArchives.Count; i++)
            {
                var archivePath = result.CreatedArchives[i];
                var fileInfo = new System.IO.FileInfo(archivePath);
                Console.WriteLine($"Archive {i + 1}: {archivePath}");
                Console.WriteLine($"  Size: {fileInfo.Length:N0} bytes");
            }
        }
    }
}
