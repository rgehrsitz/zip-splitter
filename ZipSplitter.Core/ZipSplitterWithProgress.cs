using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        /// Creates archives from a source directory with configurable options and progress reporting.
        /// </summary>
        /// <param name="sourceDirectory">The source directory to compress</param>
        /// <param name="destinationDirectory">The destination directory for ZIP files</param>
        /// <param name="options">Configuration options for the archiving operation</param>
        /// <param name="progress">Progress reporter for completion updates</param>
        /// <param name="cancellationToken">Cancellation token to stop the operation</param>
        /// <returns>A SplitResult containing information about the operation</returns>
        /// <exception cref="ArgumentException">Thrown when source directory doesn't exist</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when options are invalid</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
        public static async Task<SplitResult> CreateArchivesAsync(
            string sourceDirectory,
            string destinationDirectory,
            SplitOptions options,
            IProgress<ProgressInfo>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var startTime = DateTime.UtcNow;
            var result = new SplitResult { StrategyUsed = options.ArchiveStrategy };

            // Validate inputs
            if (!Directory.Exists(sourceDirectory))
            {
                throw new ArgumentException(
                    $"Source directory does not exist: {sourceDirectory}",
                    nameof(sourceDirectory)
                );
            }

            options.Validate();

            // Create destination directory if it doesn't exist
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            var allFiles = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

            if (allFiles.Length == 0)
            {
                progress?.Report(new ProgressInfo(100, 0, 0, "No files to compress"));
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            if (options.ArchiveStrategy == ArchiveStrategy.SingleArchive)
            {
                await CreateSingleArchiveAsync(
                    allFiles,
                    sourceDirectory,
                    destinationDirectory,
                    options,
                    result,
                    progress,
                    cancellationToken
                );
            }
            else
            {
                await CreateSplitArchivesAsync(
                    allFiles,
                    sourceDirectory,
                    destinationDirectory,
                    options,
                    result,
                    progress,
                    cancellationToken
                );
            }

            result.Duration = DateTime.UtcNow - startTime;
            return result;
        }

        private static async Task CreateSingleArchiveAsync(
            string[] allFiles,
            string sourceDirectory,
            string destinationDirectory,
            SplitOptions options,
            SplitResult result,
            IProgress<ProgressInfo>? progress,
            CancellationToken cancellationToken
        )
        {
            var fileInfos = allFiles.Select(f => new FileInfo(f)).ToList();
            long totalBytes = fileInfos.Sum(f => f.Length);
            long bytesProcessed = 0;

            string archivePath = Path.Combine(destinationDirectory, options.SingleArchiveName);

            using var archive = CreateNewArchive(archivePath);
            result.CreatedArchives.Add(archivePath);

            progress?.Report(new ProgressInfo(0, 1, 0, "Starting single archive creation"));

            foreach (var fileInfo in fileInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string entryName = Path.GetRelativePath(sourceDirectory, fileInfo.FullName);

                bytesProcessed = await AddFileToArchiveAsync(
                    fileInfo,
                    entryName,
                    archive,
                    bytesProcessed,
                    totalBytes,
                    1,
                    progress,
                    cancellationToken
                );
                result.TotalBytesProcessed = bytesProcessed;
            }

            progress?.Report(
                new ProgressInfo(100, 1, bytesProcessed, "Single archive creation completed")
            );
        }

        private static async Task CreateSplitArchivesAsync(
            string[] allFiles,
            string sourceDirectory,
            string destinationDirectory,
            SplitOptions options,
            SplitResult result,
            IProgress<ProgressInfo>? progress,
            CancellationToken cancellationToken
        )
        {
            var processableFiles = new List<FileInfo>();
            long totalBytes = 0;

            // Calculate effective size limit based on options
            long effectiveSizeLimit =
                options.SizeLimitType == SizeLimitType.CompressedArchive
                    ? (long)(options.MaxSizeBytes / options.EstimatedCompressionRatio)
                    : options.MaxSizeBytes;

            // Categorize files and handle large files
            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(file);

                if (fileInfo.Length > effectiveSizeLimit)
                {
                    await HandleLargeFileAsync(
                        fileInfo,
                        sourceDirectory,
                        destinationDirectory,
                        options,
                        result,
                        cancellationToken
                    );
                }
                else
                {
                    processableFiles.Add(fileInfo);
                    totalBytes += fileInfo.Length;
                }
            }

            if (processableFiles.Count == 0)
            {
                progress?.Report(
                    new ProgressInfo(
                        100,
                        0,
                        result.TotalBytesProcessed,
                        "No files to compress in split archives"
                    )
                );
                return;
            }

            // Process normal files into split archives
            await ProcessNormalFilesAsync(
                processableFiles,
                sourceDirectory,
                destinationDirectory,
                effectiveSizeLimit,
                totalBytes,
                result,
                progress,
                cancellationToken
            );
        }

        private static async Task HandleLargeFileAsync(
            FileInfo fileInfo,
            string sourceDirectory,
            string destinationDirectory,
            SplitOptions options,
            SplitResult result,
            CancellationToken cancellationToken
        )
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, fileInfo.FullName);

            switch (options.LargeFileHandling)
            {
                case LargeFileHandling.ThrowException:
                    throw new InvalidOperationException(
                        $"File {fileInfo.FullName} ({fileInfo.Length:N0} bytes) exceeds maximum archive size ({options.MaxSizeBytes:N0} bytes)"
                    );

                case LargeFileHandling.CreateSeparateArchive:
                    string archiveName =
                        $"large_file_{Path.GetFileNameWithoutExtension(fileInfo.Name)}.zip";
                    string archivePath = Path.Combine(destinationDirectory, archiveName);

                    using (var archive = CreateNewArchive(archivePath))
                    {
                        await AddFileToArchiveAsync(
                            fileInfo,
                            relativePath,
                            archive,
                            cancellationToken
                        );
                    }

                    result.CreatedArchives.Add(archivePath);
                    result.SpeciallyHandledFiles.Add(
                        new FileHandlingInfo(
                            fileInfo.FullName,
                            fileInfo.Length,
                            LargeFileHandling.CreateSeparateArchive,
                            archivePath,
                            "File too large for regular archives"
                        )
                    );
                    break;

                case LargeFileHandling.SkipFile:
                    result.SpeciallyHandledFiles.Add(
                        new FileHandlingInfo(
                            fileInfo.FullName,
                            fileInfo.Length,
                            LargeFileHandling.SkipFile,
                            null,
                            "File skipped due to size constraints"
                        )
                    );
                    break;

                case LargeFileHandling.CopyUncompressed:
                    string destPath = Path.Combine(destinationDirectory, relativePath);
                    string? destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    File.Copy(fileInfo.FullName, destPath, true);

                    result.SpeciallyHandledFiles.Add(
                        new FileHandlingInfo(
                            fileInfo.FullName,
                            fileInfo.Length,
                            LargeFileHandling.CopyUncompressed,
                            destPath,
                            "File copied uncompressed due to size"
                        )
                    );
                    break;
            }

            result.TotalBytesProcessed += fileInfo.Length;
        }

        private static async Task ProcessNormalFilesAsync(
            List<FileInfo> fileInfos,
            string sourceDirectory,
            string destinationDirectory,
            long maxArchiveSizeBytes,
            long totalBytes,
            SplitResult result,
            IProgress<ProgressInfo>? progress,
            CancellationToken cancellationToken
        )
        {
            long bytesProcessed = result.TotalBytesProcessed;
            int archiveIndex = result.CreatedArchives.Count + 1;
            long currentArchiveSize = 0;
            string currentArchivePath = GetArchivePath(destinationDirectory, archiveIndex);
            ZipArchive? currentArchive = null;

            try
            {
                currentArchive = CreateNewArchive(currentArchivePath);
                result.CreatedArchives.Add(currentArchivePath);

                foreach (var fileInfo in fileInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    long fileSize = fileInfo.Length;

                    // Check if we need to create a new archive
                    if (
                        currentArchiveSize + fileSize > maxArchiveSizeBytes
                        && currentArchiveSize > 0
                    )
                    {
                        currentArchive?.Dispose();
                        archiveIndex++;
                        currentArchiveSize = 0;
                        currentArchivePath = GetArchivePath(destinationDirectory, archiveIndex);
                        currentArchive = CreateNewArchive(currentArchivePath);
                        result.CreatedArchives.Add(currentArchivePath);
                    }

                    string entryName = Path.GetRelativePath(sourceDirectory, fileInfo.FullName);

                    bytesProcessed = await AddFileToArchiveAsync(
                        fileInfo,
                        entryName,
                        currentArchive,
                        bytesProcessed,
                        totalBytes + result.TotalBytesProcessed,
                        archiveIndex,
                        progress,
                        cancellationToken
                    );

                    currentArchiveSize += fileSize;
                }
                result.TotalBytesProcessed = bytesProcessed;
                progress?.Report(
                    new ProgressInfo(100, archiveIndex, bytesProcessed, "Compression completed")
                );
            }
            finally
            {
                currentArchive?.Dispose();
            }
        }

        private static async Task AddFileToArchiveAsync(
            FileInfo fileInfo,
            string entryName,
            ZipArchive archive,
            CancellationToken cancellationToken
        )
        {
            try
            {
                using var sourceStream = new FileStream(
                    fileInfo.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                await sourceStream.CopyToAsync(entryStream, cancellationToken);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    $"Access denied to file: {fileInfo.FullName}",
                    ex
                );
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    $"I/O error processing file: {fileInfo.FullName}",
                    ex
                );
            }
        }

        private static async Task<long> AddFileToArchiveAsync(
            FileInfo fileInfo,
            string entryName,
            ZipArchive archive,
            long bytesProcessed,
            long totalBytes,
            int archiveIndex,
            IProgress<ProgressInfo>? progress,
            CancellationToken cancellationToken
        )
        {
            try
            {
                using var sourceStream = new FileStream(
                    fileInfo.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();

                // 80KB buffer for streaming file data with progress reporting
                var buffer = new byte[81920]; // 80 KB buffer
                int bytesRead;
                while (
                    (
                        bytesRead = await sourceStream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length,
                            cancellationToken
                        )
                    ) > 0
                )
                {
                    await entryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    bytesProcessed += bytesRead;

                    var progressPercentage = (double)bytesProcessed / totalBytes * 100;
                    var progressInfo = new ProgressInfo(
                        progressPercentage,
                        archiveIndex,
                        bytesProcessed,
                        $"Processing: {entryName}"
                    );

                    progress?.Report(progressInfo);
                }

                return bytesProcessed;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    $"Access denied to file: {fileInfo.FullName}",
                    ex
                );
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    $"I/O error processing file: {fileInfo.FullName}",
                    ex
                );
            }
        }

        // Backward compatibility methods
        /// <summary>
        /// Creates split archives from a source directory with progress reporting and cancellation support.
        /// </summary>
        /// <param name="sourceDirectory">The source directory to compress</param>
        /// <param name="destinationDirectory">The destination directory for ZIP files</param>
        /// <param name="maxArchiveSizeBytes">Maximum size per archive in bytes</param>
        /// <param name="progress">Progress reporter for completion percentage. Reports progress every 80KB chunk during file compression for smooth progress updates.</param>
        /// <param name="cancellationToken">Cancellation token to stop the operation</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <exception cref="ArgumentException">Thrown when source directory doesn't exist</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxArchiveSizeBytes is too small</exception>
        /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
        /// <remarks>
        /// Progress reporting occurs every 80KB chunk during file compression, providing smooth progress updates
        /// even for large files. For example, a 15MB file will generate approximately 192 progress updates.
        /// This ensures responsive user feedback during long-running compression operations.
        /// </remarks>
        public static async Task CreateSplitArchivesWithProgressAsync(
            string sourceDirectory,
            string destinationDirectory,
            long maxArchiveSizeBytes,
            IProgress<ProgressInfo>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var options = new SplitOptions
            {
                ArchiveStrategy = ArchiveStrategy.SplitBySize,
                MaxSizeBytes = maxArchiveSizeBytes,
                LargeFileHandling = LargeFileHandling.ThrowException, // Default strict behavior
            };

            await CreateArchivesAsync(
                sourceDirectory,
                destinationDirectory,
                options,
                progress,
                cancellationToken
            );
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

        public ProgressInfo(
            double percentageComplete,
            int currentArchiveIndex,
            long bytesProcessed,
            string currentOperation
        )
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
