using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZipSplitter.Core;

namespace ZipSplitter.Console
{
    /// <summary>
    /// Console application demonstrating the ZipSplitter functionality.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("=== ZIP Splitter Demo ===\n");

            if (args.Length >= 3)
            {
                await RunWithArguments(args);
            }
            else
            {
                await RunInteractiveDemo();
            }

            // Only prompt for key press if console input is available
            if (Environment.UserInteractive && !System.Console.IsInputRedirected)
            {
                System.Console.WriteLine("\nPress any key to exit...");
                System.Console.ReadKey();
            }
        }

        private static async Task RunWithArguments(string[] args)
        {
            try
            {
                string sourceDir = args[0];
                string destDir = args[1];

                if (!long.TryParse(args[2], out long maxSizeInMB))
                {
                    System.Console.WriteLine(
                        "Invalid maximum size. Please provide a number in MB."
                    );
                    return;
                }

                long maxSizeInBytes = maxSizeInMB * 1024 * 1024;

                await RunZipSplitter(sourceDir, destDir, maxSizeInBytes);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task RunInteractiveDemo()
        {
            System.Console.WriteLine("=== ZIP Splitter Demo ===");
            System.Console.WriteLine("Choose demo mode:");
            System.Console.WriteLine("1. Quick demo (small files, fast)");
            System.Console.WriteLine(
                "2. Enhanced progress demo (larger files, visual progress bar)"
            );
            System.Console.Write("Enter choice (1 or 2): ");

            var choice = System.Console.ReadLine();

            if (choice == "2")
            {
                await EnhancedProgressDemo.RunDemo();
                return;
            }

            try
            {
                // Create a demo directory structure for testing
                string demoSourceDir = Path.Combine(Environment.CurrentDirectory, "DemoSource");
                string demoDestDir = Path.Combine(Environment.CurrentDirectory, "DemoOutput");

                System.Console.WriteLine("Creating demo files for testing...");
                CreateDemoFiles(demoSourceDir);

                System.Console.WriteLine($"Demo source directory: {demoSourceDir}");
                System.Console.WriteLine($"Demo destination directory: {demoDestDir}");
                System.Console.WriteLine("Maximum archive size: 2MB\n");

                await RunZipSplitter(demoSourceDir, demoDestDir, 2 * 1024 * 1024); // 2MB max

                System.Console.WriteLine($"\nDemo completed! Check the output in: {demoDestDir}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Demo failed: {ex.Message}");
            }
        }

        private static async Task RunZipSplitter(
            string sourceDir,
            string destDir,
            long maxSizeInBytes
        )
        {
            System.Console.WriteLine("Starting ZIP splitting operation...\n");

            var progress = new Progress<ProgressInfo>(info =>
            {
                // Clear the current line and write progress
                System.Console.Write($"\r{info}");
            });

            var cancellationTokenSource = new CancellationTokenSource();

            // Handle Ctrl+C for graceful cancellation
            System.Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                System.Console.WriteLine("\nCancellation requested...");
                cancellationTokenSource.Cancel();
            };

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
                    sourceDir,
                    destDir,
                    maxSizeInBytes,
                    progress,
                    cancellationTokenSource.Token
                );

                stopwatch.Stop();
                System.Console.WriteLine(
                    $"\nOperation completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds."
                );

                // Show created archives
                ShowCreatedArchives(destDir);
            }
            catch (OperationCanceledException)
            {
                System.Console.WriteLine("\nOperation was cancelled by user.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nOperation failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        private static void CreateDemoFiles(string demoDir)
        {
            if (Directory.Exists(demoDir))
            {
                Directory.Delete(demoDir, true);
            }

            Directory.CreateDirectory(demoDir);
            Directory.CreateDirectory(Path.Combine(demoDir, "Subfolder1"));
            Directory.CreateDirectory(Path.Combine(demoDir, "Subfolder2"));

            // Create files of various sizes
            CreateTextFile(Path.Combine(demoDir, "small.txt"), 1024); // 1KB
            CreateTextFile(Path.Combine(demoDir, "medium.txt"), 512 * 1024); // 512KB
            CreateTextFile(Path.Combine(demoDir, "large.txt"), 1024 * 1024); // 1MB
            CreateTextFile(Path.Combine(demoDir, "Subfolder1", "file1.txt"), 256 * 1024); // 256KB
            CreateTextFile(Path.Combine(demoDir, "Subfolder1", "file2.txt"), 128 * 1024); // 128KB
            CreateTextFile(Path.Combine(demoDir, "Subfolder2", "file3.txt"), 750 * 1024); // 750KB
            CreateTextFile(Path.Combine(demoDir, "Subfolder2", "file4.txt"), 300 * 1024); // 300KB

            System.Console.WriteLine(
                $"Created demo files totaling approximately {GetDirectorySize(demoDir) / (1024.0 * 1024):F2} MB"
            );
        }

        private static void CreateTextFile(string filePath, int sizeInBytes)
        {
            using (var writer = new FileStream(filePath, FileMode.Create))
            {
                var buffer = new byte[Math.Min(sizeInBytes, 4096)];
                var random = new Random();

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)('A' + (i % 26)); // Fill with A-Z pattern
                }

                int bytesWritten = 0;
                while (bytesWritten < sizeInBytes)
                {
                    int bytesToWrite = Math.Min(buffer.Length, sizeInBytes - bytesWritten);
                    writer.Write(buffer, 0, bytesToWrite);
                    bytesWritten += bytesToWrite;
                }
            }
        }

        private static long GetDirectorySize(string directoryPath)
        {
            long size = 0;
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                size += new FileInfo(file).Length;
            }
            return size;
        }

        private static void ShowCreatedArchives(string destDir)
        {
            if (!Directory.Exists(destDir))
                return;

            var zipFiles = Directory.GetFiles(destDir, "*.zip");
            if (zipFiles.Length == 0)
            {
                System.Console.WriteLine("No ZIP files were created.");
                return;
            }

            System.Console.WriteLine($"\nCreated {zipFiles.Length} archive(s):");
            foreach (var zipFile in zipFiles)
            {
                var fileInfo = new FileInfo(zipFile);
                System.Console.WriteLine(
                    $"  {Path.GetFileName(zipFile)} - {fileInfo.Length / 1024.0:F1} KB"
                );
            }
        }
    }
}
