using System;
using System.Collections.Generic;
using System.Linq;

namespace ZipSplitter.Core
{
    /// <summary>
    /// Information about a file that was handled specially during splitting.
    /// </summary>
    public class FileHandlingInfo
    {
        public string FilePath { get; }
        public long FileSizeBytes { get; }
        public LargeFileHandling HandlingMethod { get; }
        public string? OutputPath { get; }
        public string Reason { get; }

        public FileHandlingInfo(string filePath, long fileSizeBytes, LargeFileHandling handlingMethod, string? outputPath, string reason)
        {
            FilePath = filePath;
            FileSizeBytes = fileSizeBytes;
            HandlingMethod = handlingMethod;
            OutputPath = outputPath;
            Reason = reason;
        }
    }

    /// <summary>
    /// Results of a ZIP splitting operation.
    /// </summary>
    public class SplitResult
    {
        public List<string> CreatedArchives { get; } = new();
        public List<FileHandlingInfo> SpeciallyHandledFiles { get; } = new();
        public long TotalBytesProcessed { get; set; }
        public TimeSpan Duration { get; set; }
        public ArchiveStrategy StrategyUsed { get; set; }
        public bool HasWarnings => SpeciallyHandledFiles.Count > 0;

        /// <summary>
        /// Gets files that were skipped due to size constraints.
        /// </summary>
        public IEnumerable<FileHandlingInfo> SkippedFiles =>
            SpeciallyHandledFiles.Where(f => f.HandlingMethod == LargeFileHandling.SkipFile);

        /// <summary>
        /// Gets files that were copied uncompressed.
        /// </summary>
        public IEnumerable<FileHandlingInfo> UncompressedFiles =>
            SpeciallyHandledFiles.Where(f => f.HandlingMethod == LargeFileHandling.CopyUncompressed);

        /// <summary>
        /// Gets files that were put in separate archives.
        /// </summary>
        public IEnumerable<FileHandlingInfo> SeparateArchiveFiles =>
            SpeciallyHandledFiles.Where(f => f.HandlingMethod == LargeFileHandling.CreateSeparateArchive);

        public override string ToString()
        {
            var result = $"Strategy: {StrategyUsed}, Archives: {CreatedArchives.Count}, " +
                        $"Total Size: {TotalBytesProcessed:N0} bytes, Duration: {Duration}";

            if (HasWarnings)
            {
                result += $", Warnings: {SpeciallyHandledFiles.Count}";
            }

            return result;
        }
    }
}
