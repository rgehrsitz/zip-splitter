using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZipSplitter.Core;

namespace ZipSplitter.Examples
{
    /// <summary>
    /// Examples demonstrating various ways to use the ZipSplitter library.
    /// For a live demonstration, run the console application which offers:
    /// - Quick Demo: Fast demonstration with 2.9MB sample files
    /// - Enhanced Progress Demo: Visual progress bar with 15MB realistic files
    /// </summary>
    public class UsageExamples
    {
        /// <summary>
        /// Basic usage with progress reporting.
        /// Progress updates occur every 80KB chunk during file compression,
        /// providing smooth progress for large files.
        /// </summary>
        public static async Task BasicUsageExample()
        {
            Console.WriteLine("=== Basic Usage Example ===");

            var progress = new Progress<ProgressInfo>(info =>
            {
                Console.Write($"\r{info.PercentageComplete:F1}% - {info.CurrentOperation}");
            });

            try
            {
                await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    sourceDirectory: @"C:\MyDocuments",
                    destinationDirectory: @"C:\Backups",
                    maxArchiveSizeBytes: 50 * 1024 * 1024, // 50MB
                    progress: progress
                );

                Console.WriteLine("\nBackup completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }

        /// <summary>
        /// Usage with cancellation support
        /// </summary>
        public static async Task CancellationExample()
        {
            Console.WriteLine("=== Cancellation Example ===");

            var cancellationTokenSource = new CancellationTokenSource();

            // Cancel after 10 seconds
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
            var progress = new Progress<ProgressInfo>(info =>
            {
                Console.WriteLine(
                    $"Archive {info.CurrentArchiveIndex}: {info.PercentageComplete:F1}% - {info.CurrentOperation}"
                );
                // Note: Progress updates occur every 80KB chunk during compression
                // For a 15MB file, you'll see ~192 progress updates (15MB ÷ 80KB)
                // This provides smooth progress feedback for large files
            });

            try
            {
                await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    sourceDirectory: @"C:\LargeProject",
                    destinationDirectory: @"C:\Archives",
                    maxArchiveSizeBytes: 100 * 1024 * 1024, // 100MB
                    progress: progress,
                    cancellationToken: cancellationTokenSource.Token
                );

                Console.WriteLine("Archive creation completed!");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronous usage for simple scenarios.
        /// Progress updates occur every 80KB chunk, providing smooth feedback.
        /// </summary>
        public static void SynchronousExample()
        {
            Console.WriteLine("=== Synchronous Example ===");

            var progress = new Progress<double>(percentage =>
            {
                Console.Write($"\rProgress: {percentage:F1}%");
            });

            try
            {
                ZipSplitterWithProgress.CreateSplitArchivesWithProgress(
                    sourceDirectory: @"C:\SmallProject",
                    destinationDirectory: @"C:\QuickBackup",
                    maxArchiveSizeBytes: 25 * 1024 * 1024, // 25MB
                    progress: progress
                );

                Console.WriteLine("\nQuick backup completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced visual progress example (similar to the Enhanced Demo)
        /// </summary>
        public static async Task VisualProgressExample()
        {
            Console.WriteLine("=== Visual Progress Example ===");

            var progress = new Progress<ProgressInfo>(info =>
            {
                // Create a visual progress bar (50 characters wide)
                int barLength = 50;
                int filledLength = (int)(info.PercentageComplete / 100.0 * barLength);
                string progressBar =
                    new string('█', filledLength) + new string('░', barLength - filledLength);

                // Clear the line and write the progress
                Console.Write($"\r[{progressBar}] {info.PercentageComplete:F1}%");
                Console.Write(
                    $"\nArchive: {info.CurrentArchiveIndex} | Processed: {FormatBytes(info.BytesProcessed)}"
                );
                if (!string.IsNullOrEmpty(info.CurrentOperation))
                {
                    Console.Write($"\nCurrent: {info.CurrentOperation}");
                }

                // Move cursor back up to overwrite on next update
                Console.SetCursorPosition(0, Console.CursorTop - 2);
            });

            try
            {
                await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    sourceDirectory: @"C:\LargeProject",
                    destinationDirectory: @"C:\VisualBackup",
                    maxArchiveSizeBytes: 100 * 1024 * 1024, // 100MB
                    progress: progress
                );

                Console.WriteLine("\n\n=== Visual Progress Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\nError: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to format bytes in a human-readable way
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public static async Task AdvancedProgressExample()
        {
            Console.WriteLine("=== Advanced Progress Example ===");

            var startTime = DateTime.Now;
            long totalBytesAtStart = 0;

            var progress = new Progress<ProgressInfo>(info =>
            {
                var elapsed = DateTime.Now - startTime;
                var speed = info.BytesProcessed / elapsed.TotalSeconds;
                var speedMB = speed / (1024 * 1024);

                Console.WriteLine(
                    $"Archive {info.CurrentArchiveIndex:D3} | "
                        + $"{info.PercentageComplete:F1}% | "
                        + $"{info.BytesProcessed / (1024 * 1024):F1} MB | "
                        + $"{speedMB:F1} MB/s | "
                        + $"{info.CurrentOperation}"
                );
            });

            try
            {
                await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    sourceDirectory: @"C:\PhotoLibrary",
                    destinationDirectory: @"C:\PhotoBackups",
                    maxArchiveSizeBytes: 200 * 1024 * 1024, // 200MB
                    progress: progress
                );

                var totalTime = DateTime.Now - startTime;
                Console.WriteLine(
                    $"\nPhoto backup completed in {totalTime.TotalMinutes:F1} minutes!"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }

        /// <summary>
        /// Error handling example
        /// </summary>
        public static async Task ErrorHandlingExample()
        {
            Console.WriteLine("=== Error Handling Example ===");

            try
            {
                await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    sourceDirectory: @"C:\NonExistentFolder",
                    destinationDirectory: @"C:\Backups",
                    maxArchiveSizeBytes: 1024
                ); // Too small
            }
            catch (ArgumentException ex) when (ex.ParamName == "sourceDirectory")
            {
                Console.WriteLine($"Source directory error: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "maxArchiveSizeBytes")
            {
                Console.WriteLine($"Archive size error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Operation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Utility method to get directory size
        /// </summary>
        public static long GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return 0;

            long size = 0;
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch (Exception)
                {
                    // Skip files that can't be accessed
                }
            }

            return size;
        }

        /// <summary>
        /// Example showing how to calculate optimal archive size
        /// </summary>
        public static void OptimalArchiveSizeExample()
        {
            Console.WriteLine("=== Optimal Archive Size Example ===");

            string sourceDir = @"C:\MyProject";
            long totalSize = GetDirectorySize(sourceDir);

            if (totalSize == 0)
            {
                Console.WriteLine("Directory is empty or doesn't exist.");
                return;
            }

            // Aim for 10-20 archives
            long optimalSize = totalSize / 15; // Target 15 archives

            // Round to nearest 10MB
            optimalSize = ((optimalSize / (10 * 1024 * 1024)) + 1) * (10 * 1024 * 1024);

            // Ensure minimum size
            optimalSize = Math.Max(optimalSize, 10 * 1024 * 1024); // At least 10MB

            Console.WriteLine($"Total directory size: {totalSize / (1024.0 * 1024):F1} MB");
            Console.WriteLine($"Recommended archive size: {optimalSize / (1024.0 * 1024):F0} MB");
            Console.WriteLine(
                $"Estimated number of archives: {Math.Ceiling((double)totalSize / optimalSize):F0}"
            );
        }
    }
}
