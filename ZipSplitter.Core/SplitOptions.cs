using System;

namespace ZipSplitter.Core
{
    /// <summary>
    /// Defines how to handle files that exceed the maximum archive size.
    /// </summary>
    public enum LargeFileHandling
    {
        /// <summary>
        /// Throw an exception when a file exceeds the maximum size (current behavior).
        /// </summary>
        ThrowException,

        /// <summary>
        /// Create a separate archive for the large file alone.
        /// </summary>
        CreateSeparateArchive,

        /// <summary>
        /// Skip the large file and report it in the results.
        /// </summary>
        SkipFile,

        /// <summary>
        /// Copy the large file uncompressed to the destination directory.
        /// </summary>
        CopyUncompressed,
    }

    /// <summary>
    /// Defines what the size limit represents.
    /// </summary>
    public enum SizeLimitType
    {
        /// <summary>
        /// Limit based on uncompressed data size (current behavior).
        /// </summary>
        UncompressedData,

        /// <summary>
        /// Limit based on the resulting ZIP file size (approximate).
        /// </summary>
        CompressedArchive,
    }

    /// <summary>
    /// Defines the archive creation strategy.
    /// </summary>
    public enum ArchiveStrategy
    {
        /// <summary>
        /// Split files into multiple archives based on size limits.
        /// </summary>
        SplitBySize,

        /// <summary>
        /// Create a single archive containing all files regardless of size.
        /// </summary>
        SingleArchive,
    }

    /// <summary>
    /// Configuration options for ZIP splitting operations.
    /// </summary>
    public class SplitOptions
    {
        /// <summary>
        /// The archive creation strategy. Default is SplitBySize.
        /// </summary>
        public ArchiveStrategy ArchiveStrategy { get; set; } = ArchiveStrategy.SplitBySize;

        /// <summary>
        /// Maximum size limit in bytes. Default is 100MB.
        /// Only applies when ArchiveStrategy is SplitBySize.
        /// </summary>
        public long MaxSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB default

        /// <summary>
        /// How to handle files that exceed the maximum size. Default is CreateSeparateArchive.
        /// Only applies when ArchiveStrategy is SplitBySize.
        /// </summary>
        public LargeFileHandling LargeFileHandling { get; set; } =
            LargeFileHandling.CreateSeparateArchive;

        /// <summary>
        /// What the size limit represents. Default is UncompressedData for backward compatibility.
        /// Only applies when ArchiveStrategy is SplitBySize.
        /// </summary>
        public SizeLimitType SizeLimitType { get; set; } = SizeLimitType.UncompressedData;

        /// <summary>
        /// Compression ratio estimate for compressed archive size calculation (0.1 to 1.0).
        /// Default is 0.7 (assumes 30% compression). Only used when SizeLimitType is CompressedArchive.
        /// </summary>
        public double EstimatedCompressionRatio { get; set; } = 0.7;

        /// <summary>
        /// The name for the single archive when using SingleArchive strategy.
        /// Default is "archive.zip". This name is used directly for the output file.
        /// It must end with ".zip".
        /// </summary>
        public string SingleArchiveName { get; set; } = "archive.zip";

        /// <summary>
        /// Base name for sequentially numbered archives when using SplitBySize strategy.
        /// Default is "archive". Resulting files will be "archive001.zip", "archive002.zip", etc.
        /// This is not currently customizable through SplitOptions but reflects the default behavior.
        /// Large files handled with CreateSeparateArchive will be named "large_file_{OriginalFileNameWithoutExtension}.zip".
        /// </summary>
        // Note: This is a conceptual property to aid documentation of default behavior.
        // The actual naming logic is in ZipSplitterWithProgress.GetArchivePath and HandleLargeFileAsync.
        public string SplitArchiveBaseName { get; } = "archive"; // Readonly, reflects current default

        /// <summary>
        /// Validates the options and throws ArgumentException if invalid.
        /// </summary>
        public void Validate()
        {
            if (ArchiveStrategy == ArchiveStrategy.SplitBySize)
            {
                if (MaxSizeBytes < 1024 * 1024) // Minimum 1MB
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(MaxSizeBytes),
                        "Maximum size must be at least 1MB when using SplitBySize strategy"
                    );
                }

                if (EstimatedCompressionRatio <= 0 || EstimatedCompressionRatio > 1.0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(EstimatedCompressionRatio),
                        "Compression ratio must be between 0 and 1.0"
                    );
                }
            }

            if (string.IsNullOrWhiteSpace(SingleArchiveName))
            {
                throw new ArgumentException(
                    "Single archive name cannot be null or empty",
                    nameof(SingleArchiveName)
                );
            }

            if (!SingleArchiveName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Single archive name must end with .zip extension",
                    nameof(SingleArchiveName)
                );
            }
        }
    }
}
