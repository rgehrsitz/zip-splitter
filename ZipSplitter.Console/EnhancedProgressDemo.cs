using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZipSplitter.Core;

namespace ZipSplitter.Console
{
    class EnhancedProgressDemo
    {
        public static async Task RunDemo()
        {
            System.Console.WriteLine("=== Enhanced ZIP Splitter Progress Demo ===");
            System.Console.WriteLine(
                "This demo showcases the advanced progress reporting capabilities.\n"
            );

            try
            {
                string demoSourceDir = Path.Combine(
                    Environment.CurrentDirectory,
                    "EnhancedDemoSource"
                );
                string demoDestDir = Path.Combine(
                    Environment.CurrentDirectory,
                    "EnhancedDemoOutput"
                );

                // Create larger files for better progress visualization
                System.Console.WriteLine(
                    "Creating enhanced demo files (this may take a moment)..."
                );
                CreateEnhancedDemoFiles(demoSourceDir);
                long totalSize = CalculateTotalSize(demoSourceDir);
                System.Console.WriteLine(
                    $"\nCreated {totalSize / (1024.0 * 1024.0):F1} MB of demo files"
                );
                System.Console.WriteLine($"These will be split into archives of max 4 MB each\n");

                System.Console.WriteLine("Progress will show:");
                System.Console.WriteLine("• Visual progress bar");
                System.Console.WriteLine("• Percentage completion for ENTIRE operation");
                System.Console.WriteLine("• Current archive being created");
                System.Console.WriteLine("• Bytes processed so far");
                System.Console.WriteLine("• Current file being processed");
                System.Console.WriteLine(
                    "\nStarting compression with enhanced progress display...\n"
                );

                await RunWithEnhancedProgress(demoSourceDir, demoDestDir, 4 * 1024 * 1024);

                System.Console.WriteLine("\n=== Demo Complete ===");
                ShowFinalResults(demoDestDir);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Demo failed: {ex.Message}");
            }

            // Only prompt for key press if console input is available
            if (Environment.UserInteractive && !System.Console.IsInputRedirected)
            {
                System.Console.WriteLine("\nPress any key to exit...");
                System.Console.ReadKey();
            }
        }

        private static async Task RunWithEnhancedProgress(
            string sourceDir,
            string destDir,
            long maxSizeBytes
        )
        {
            var progress = new Progress<ProgressInfo>(info => DisplayProgressBar(info));
            var cancellationTokenSource = new CancellationTokenSource();

            // Handle Ctrl+C gracefully
            System.Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                System.Console.WriteLine("\n\nCancellation requested...");
                cancellationTokenSource.Cancel();
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    sourceDir,
                    destDir,
                    maxSizeBytes,
                    progress,
                    cancellationTokenSource.Token
                );

                stopwatch.Stop();
                System.Console.WriteLine(
                    $"\n\nOperation completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds!"
                );
            }
            catch (OperationCanceledException)
            {
                System.Console.WriteLine("\n\nOperation was cancelled by user.");
            }
        }

        private static void DisplayProgressBar(ProgressInfo info)
        {
            // Save cursor position and clear lines
            int currentLine = System.Console.CursorTop;

            // Create visual progress bar
            int barWidth = 50;
            int filledWidth = (int)(info.PercentageComplete / 100.0 * barWidth);
            string bar = "█".PadRight(filledWidth, '█').PadRight(barWidth, '░');

            // Clear and rewrite progress display
            System.Console.SetCursorPosition(0, currentLine);
            System.Console.WriteLine(
                $"[{bar}] {info.PercentageComplete:F1}%".PadRight(System.Console.WindowWidth - 1)
            );
            System.Console.WriteLine(
                $"Archive: {info.CurrentArchiveIndex} | Processed: {FormatBytes(info.BytesProcessed)}".PadRight(
                    System.Console.WindowWidth - 1
                )
            );
            System.Console.WriteLine(
                $"Current: {TruncateString(info.CurrentOperation, System.Console.WindowWidth - 10)}".PadRight(
                    System.Console.WindowWidth - 1
                )
            );
            System.Console.WriteLine("".PadRight(System.Console.WindowWidth - 1)); // Empty line for spacing

            // Move cursor back to beginning of progress display
            if (info.PercentageComplete < 100)
            {
                System.Console.SetCursorPosition(0, currentLine);
            }
        }

        private static void CreateEnhancedDemoFiles(string demoDir)
        {
            if (Directory.Exists(demoDir))
                Directory.Delete(demoDir, true);

            Directory.CreateDirectory(demoDir);

            // Create directory structure
            var dirs = new[]
            {
                "Documents",
                "Images",
                "Projects",
                "Data",
                Path.Combine("Projects", "WebApp"),
                Path.Combine("Projects", "API"),
                Path.Combine("Data", "Exports"),
            };

            foreach (var dir in dirs)
            {
                Directory.CreateDirectory(Path.Combine(demoDir, dir));
            }

            System.Console.Write("Creating files: ");

            // Create various file types with delays to show progress
            CreateProgressiveFile(
                Path.Combine(demoDir, "Documents", "manual.pdf"),
                1.2,
                "PDF manual"
            );
            CreateProgressiveFile(Path.Combine(demoDir, "Documents", "readme.txt"), 0.3, "readme");
            CreateProgressiveFile(Path.Combine(demoDir, "Images", "photo1.jpg"), 2.1, "photo1");
            CreateProgressiveFile(Path.Combine(demoDir, "Images", "photo2.jpg"), 1.8, "photo2");
            CreateProgressiveFile(Path.Combine(demoDir, "Images", "banner.png"), 0.9, "banner");
            CreateProgressiveFile(
                Path.Combine(demoDir, "Projects", "source.zip"),
                2.5,
                "source code"
            );
            CreateProgressiveFile(
                Path.Combine(demoDir, "Projects", "WebApp", "bundle.js"),
                1.1,
                "web bundle"
            );
            CreateProgressiveFile(
                Path.Combine(demoDir, "Projects", "API", "docs.html"),
                0.7,
                "API docs"
            );
            CreateProgressiveFile(Path.Combine(demoDir, "Data", "database.db"), 2.8, "database");
            CreateProgressiveFile(
                Path.Combine(demoDir, "Data", "Exports", "report.csv"),
                1.6,
                "export data"
            );

            System.Console.WriteLine(" ✓");
        }

        private static void CreateProgressiveFile(
            string filePath,
            double sizeMB,
            string description
        )
        {
            System.Console.Write($"{description}... ");

            long sizeBytes = (long)(sizeMB * 1024 * 1024);
            var buffer = new byte[64 * 1024]; // 64KB chunks
            var random = new Random();

            using (var file = File.Create(filePath))
            {
                long written = 0;
                while (written < sizeBytes)
                {
                    random.NextBytes(buffer);
                    int toWrite = (int)Math.Min(buffer.Length, sizeBytes - written);
                    file.Write(buffer, 0, toWrite);
                    written += toWrite;
                    // Small delay to make file creation visible
                    Thread.Sleep(50);
                }
            }
        }

        private static long CalculateTotalSize(string directory)
        {
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            long total = 0;
            foreach (var file in files)
            {
                total += new FileInfo(file).Length;
            }
            return total;
        }

        private static void ShowFinalResults(string destDir)
        {
            if (!Directory.Exists(destDir))
                return;

            var archives = Directory.GetFiles(destDir, "*.zip");
            System.Console.WriteLine($"Created {archives.Length} archive(s):");

            foreach (var archive in archives)
            {
                var info = new FileInfo(archive);
                System.Console.WriteLine(
                    $"  {Path.GetFileName(archive)} - {FormatBytes(info.Length)}"
                );
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int suffixIndex = 0;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        private static string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;

            return str.Substring(0, maxLength - 3) + "...";
        }
    }
}
