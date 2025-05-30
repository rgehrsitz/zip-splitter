# ZIP Splitter Enhancements

## Overview

The ZIP Splitter has been enhanced with new configuration options that provide much more flexibility for handling different archiving scenarios, including the ability to create single archives regardless of size.

## New Features

### 1. Archive Strategy Options

- **`ArchiveStrategy.SplitBySize`**: Split files into multiple archives based on size limits. Archives are named sequentially by default (e.g., `archive001.zip`, `archive002.zip`, etc.).
- **`ArchiveStrategy.SingleArchive`**: Create a single archive containing all files regardless of size. The name is specified by the `SingleArchiveName` property in `SplitOptions`.

### 2. Large File Handling Options

When using split archives, files that exceed the size limit can be handled in multiple ways:

- **`LargeFileHandling.ThrowException`**: Throw an exception (original behavior).
- **`LargeFileHandling.CreateSeparateArchive`**: Create a separate archive for the large file, named `large_file_{OriginalFileNameWithoutExtension}.zip`.
- **`LargeFileHandling.SkipFile`**: Skip the file and report it in results.
- **`LargeFileHandling.CopyUncompressed`**: Copy the file uncompressed to destination

### 3. Size Limit Types

The size limit can now represent different things:

- **`SizeLimitType.UncompressedData`**: Limit based on original file sizes (original behavior)
- **`SizeLimitType.CompressedArchive`**: Limit based on estimated compressed archive size

### 4. Enhanced Result Information

The new `SplitResult` class provides:

- List of created archives
- Information about specially handled files
- Total bytes processed
- Operation duration
- Strategy used
- Warning indicators

## Usage Examples

### Single Archive (Regardless of Size)

```csharp
var options = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SingleArchive,
    SingleArchiveName = "complete_backup.zip"
};

var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    sourceDirectory,
    destinationDirectory,
    options,
    progress,
    cancellationToken);
```

### Split Archives with Compressed Size Limit

```csharp
var options = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxSizeBytes = 50 * 1024 * 1024, // 50MB
    SizeLimitType = SizeLimitType.CompressedArchive,
    LargeFileHandling = LargeFileHandling.CreateSeparateArchive,
    EstimatedCompressionRatio = 0.6 // Assume 40% compression
};
```

### Handle Large Files by Copying Uncompressed

```csharp
var options = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxSizeBytes = 100 * 1024 * 1024, // 100MB
    LargeFileHandling = LargeFileHandling.CopyUncompressed
};
```

## Backward Compatibility

All existing method signatures continue to work unchanged. The original `CreateSplitArchivesWithProgressAsync` method now internally uses the new enhanced system with default settings that match the original behavior.

## Key Benefits

1. **Flexibility**: Choose between single archive or splitting strategies
2. **Better Large File Handling**: Multiple options instead of just throwing exceptions
3. **Clearer Size Semantics**: Specify whether limits apply to compressed or uncompressed data
4. **Rich Feedback**: Detailed results about what happened during the operation
5. **Graceful Degradation**: Handle edge cases without failing the entire operation

## Size Limit Clarification

### Before

The `maxArchiveSizeBytes` parameter was misleading - it actually referred to the uncompressed data size, not the final ZIP file size.

### After

Now you can explicitly choose:

- `SizeLimitType.UncompressedData`: Limit based on original file sizes
- `SizeLimitType.CompressedArchive`: Limit based on estimated compressed archive size (uses configurable compression ratio)

This makes the behavior much more predictable and intuitive for users who want to control the actual size of the resulting ZIP files.
