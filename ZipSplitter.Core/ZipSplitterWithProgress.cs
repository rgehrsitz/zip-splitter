using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace ZipSplitter.Core
{
    /// <summary>
    /// Provides functionality to split large directories into multiple ZIP archives with progress reporting.
    /// </summary>
    public class ZipSplitterWithProgress
    {
        /// <summary>
        /// Creates split archives from a source directory with progress reporting and cancellation support.
        /// </summary>
        /// <param name="sourceDirectory">The source directory to compress</param>
        /// <param name="destinationDirectory">The destination directory for ZIP files</param>
        /// <param name="maxArchiveSizeBytes">Maximum size per archive in bytes</param>
        /// <param name="progress">Progress reporter for completion percentage</param>
        /// <param name="cancellationToken">Cancellation token to stop the operation</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <exception cref="ArgumentException">Thrown when source directory doesn't exist</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxArchiveSizeBytes is too small</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
        public static async Task CreateSplitArchivesWithProgressAsync(
            string sourceDirectory,
            string destinationDirectory,
            long maxArchiveSizeBytes,
            IProgress<ProgressInfo> progress = null,
            CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (!Directory.Exists(sourceDirectory))
            {
                throw new ArgumentException($"Source directory does not exist: {sourceDirectory}", nameof(sourceDirectory));
            }

            if (maxArchiveSizeBytes < 1024 * 1024) // Minimum 1MB
            {
                throw new ArgumentOutOfRangeException(nameof(maxArchiveSizeBytes), "Maximum archive size must be at least 1MB");
            }

            // Create destination directory if it doesn't exist
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            var allFiles = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            long totalBytes = 0;
            var fileInfos = new List<FileInfo>();

            // Calculate total size and validate files
            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var fileInfo = new FileInfo(file);
                if (fileInfo.Length > maxArchiveSizeBytes)
                {
                    throw new InvalidOperationException($"File {file} ({fileInfo.Length} bytes) exceeds maximum archive size ({maxArchiveSizeBytes} bytes)");
                }
                
                fileInfos.Add(fileInfo);
                totalBytes += fileInfo.Length;
            }

            if (totalBytes == 0)
            {
                progress?.Report(new ProgressInfo(100, 0, 0, "No files to compress"));
                return;
            }

            long bytesProcessed = 0;
            int archiveIndex = 1;
            long currentArchiveSize = 0;
            string currentArchivePath = GetArchivePath(destinationDirectory, archiveIndex);
            ZipArchive currentArchive = null;

            try
            {
                currentArchive = CreateNewArchive(currentArchivePath);

                foreach (var fileInfo in fileInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    long fileSize = fileInfo.Length;

                    // Check if we need to create a new archive
                    if (currentArchiveSize + fileSize > maxArchiveSizeBytes && currentArchiveSize > 0)
                    {
                        currentArchive?.Dispose();
                        archiveIndex++;
                        currentArchiveSize = 0;
                        currentArchivePath = GetArchivePath(destinationDirectory, archiveIndex);
                        currentArchive = CreateNewArchive(currentArchivePath);
                    }

                    string entryName = Path.GetRelativePath(sourceDirectory, fileInfo.FullName);
                    
                    try
                    {
                        using (var sourceStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var entry = currentArchive.CreateEntry(entryName, CompressionLevel.Optimal);
                            using (var entryStream = entry.Open())
                            {
                                var buffer = new byte[81920]; // 80 KB buffer
                                int bytesRead;
                                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                                {
                                    await entryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                    bytesProcessed += bytesRead;
                                    currentArchiveSize += bytesRead;
                                    
                                    var progressPercentage = (double)bytesProcessed / totalBytes * 100;
                                    var progressInfo = new ProgressInfo(
                                        progressPercentage, 
                                        archiveIndex, 
                                        bytesProcessed, 
                                        $"Processing: {entryName}");
                                    
                                    progress?.Report(progressInfo);
                                }
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw new InvalidOperationException($"Access denied to file: {fileInfo.FullName}", ex);
                    }
                    catch (IOException ex)
                    {
                        throw new InvalidOperationException($"I/O error processing file: {fileInfo.FullName}", ex);
                    }
                }

                progress?.Report(new ProgressInfo(100, archiveIndex, bytesProcessed, "Compression completed"));
            }
            finally
            {
                currentArchive?.Dispose();
            }
        }

        /// <summary>
        /// Synchronous version of CreateSplitArchivesWithProgressAsync for compatibility.
        /// </summary>
        public static void CreateSplitArchivesWithProgress(
            string sourceDirectory,
            string destinationDirectory,
            long maxArchiveSizeBytes,
            IProgress<double> progress = null)
        {
            var wrappedProgress = progress != null 
                ? new Progress<ProgressInfo>(info => progress.Report(info.PercentageComplete))
                : null;

            CreateSplitArchivesWithProgressAsync(
                sourceDirectory, 
                destinationDirectory, 
                maxArchiveSizeBytes, 
                wrappedProgress, 
                CancellationToken.None).GetAwaiter().GetResult();
        }

        private static string GetArchivePath(string destinationDirectory, int index)
        {
            return Path.Combine(destinationDirectory, $"archive{index:D3}.zip");
        }

        private static ZipArchive CreateNewArchive(string archivePath)
        {
            var zipToOpen = new FileStream(archivePath, FileMode.Create, FileAccess.Write);
            return new ZipArchive(zipToOpen, ZipArchiveMode.Create);
        }
    }

    /// <summary>
    /// Represents progress information for the ZIP splitting operation.
    /// </summary>
    public class ProgressInfo
    {
        public double PercentageComplete { get; }
        public int CurrentArchiveIndex { get; }
        public long BytesProcessed { get; }
        public string CurrentOperation { get; }

        public ProgressInfo(double percentageComplete, int currentArchiveIndex, long bytesProcessed, string currentOperation)
        {
            PercentageComplete = percentageComplete;
            CurrentArchiveIndex = currentArchiveIndex;
            BytesProcessed = bytesProcessed;
            CurrentOperation = currentOperation;
        }

        public override string ToString()
        {
            return $"{PercentageComplete:F1}% - Archive {CurrentArchiveIndex} - {BytesProcessed:N0} bytes - {CurrentOperation}";
        }
    }
}
