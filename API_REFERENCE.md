# ZIP Splitter API Reference

## Overview

The ZIP Splitter library provides flexible functionality to create ZIP archives from directory structures with configurable options for size limits, large file handling, and archive strategies.

## Main API

### ZipSplitterWithProgress.CreateArchivesAsync

The primary method for all archive creation operations.

```csharp
public static async Task<SplitResult> CreateArchivesAsync(
    string sourceDirectory,
    string destinationDirectory,
    SplitOptions options,
    IProgress<ProgressInfo>? progress = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**

- `sourceDirectory`: Path to the source directory to compress
- `destinationDirectory`: Path where ZIP files will be created
- `options`: Configuration options for the operation (see SplitOptions below)
- `progress`: Optional progress reporter for real-time updates
- `cancellationToken`: Optional cancellation token for operation cancellation

**Returns:** `SplitResult` containing detailed operation information

## Configuration Classes

### SplitOptions

Primary configuration class for archive operations.

**Properties:**

```csharp
public class SplitOptions
{
    public ArchiveStrategy ArchiveStrategy { get; set; } = ArchiveStrategy.SplitBySize;
    public long MaxArchiveSize { get; set; } = 50 * 1024 * 1024; // 50MB
    public LargeFileHandling LargeFileHandling { get; set; } = LargeFileHandling.ThrowException;
    public SizeLimitType SizeLimitType { get; set; } = SizeLimitType.UncompressedData;
    public double EstimatedCompressionRatio { get; set; } = 0.7;
    public string SingleArchiveName { get; set; } = "archive.zip";
}
```

**Property Details:**

- `ArchiveStrategy`: Determines whether to split by size or create a single archive
- `MaxArchiveSize`: Maximum size limit (interpretation depends on SizeLimitType)
- `LargeFileHandling`: How to handle files that exceed the size limit
- `SizeLimitType`: Whether size limit applies to uncompressed data or final ZIP size
- `EstimatedCompressionRatio`: Used for compressed size calculations (0.1-1.0)
- `SingleArchiveName`: Name for single archive mode

### SplitResult

Contains comprehensive information about the archive operation.

**Properties:**

```csharp
public class SplitResult
{
    public List<string> CreatedArchives { get; set; } = new();
    public List<string> SpeciallyHandledFiles { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public long TotalBytesProcessed { get; set; }
    public bool HasSpeciallyHandledFiles => SpeciallyHandledFiles.Any();
}
```

## Enumerations

### ArchiveStrategy

```csharp
public enum ArchiveStrategy
{
    SplitBySize,    // Create multiple archives based on size limits
    SingleArchive   // Create one archive regardless of size
}
```

### LargeFileHandling

```csharp
public enum LargeFileHandling
{
    ThrowException,        // Throw exception for oversized files
    CreateSeparateArchive, // Create individual archives for large files
    SkipFile,             // Skip oversized files entirely
    CopyUncompressed      // Copy large files without compression
}
```

### SizeLimitType

```csharp
public enum SizeLimitType
{
    UncompressedData,  // Limit applies to source file sizes
    CompressedArchive  // Limit applies to resulting ZIP file size
}
```

## Usage Examples

### Basic Usage

#### Creating a Single Archive

```csharp
var options = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SingleArchive,
    SingleArchiveName = "backup.zip"
};

var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    @"C:\MyFiles",
    @"C:\Archives",
    options);

Console.WriteLine($"Created archive: {result.CreatedArchives[0]}");
```

#### Creating Split Archives

```csharp
var options = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxArchiveSize = 50 * 1024 * 1024, // 50MB
    LargeFileHandling = LargeFileHandling.CreateSeparateArchive
};

var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    @"C:\MyFiles",
    @"C:\Archives",
    options);

Console.WriteLine($"Created {result.CreatedArchives.Count} archives");
```

### Advanced Usage

#### With Progress Reporting

```csharp
var progress = new Progress<ProgressInfo>(info =>
{
    Console.WriteLine($"Progress: {info.PercentageComplete:F1}%");
    Console.WriteLine($"Current: {info.CurrentOperation}");
});

var options = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxArchiveSize = 100 * 1024 * 1024
};

var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    sourceDir, destDir, options, progress);
```

#### With Cancellation Support

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromMinutes(5)); // Cancel after 5 minutes

try
{
    var result = await ZipSplitterWithProgress.CreateArchivesAsync(
        sourceDir, destDir, options, progress, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

#### Compressed Size Limits

```csharp
var options = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxArchiveSize = 25 * 1024 * 1024, // 25MB final ZIP size
    SizeLimitType = SizeLimitType.CompressedArchive,
    EstimatedCompressionRatio = 0.7, // Expect 30% compression
    LargeFileHandling = LargeFileHandling.SkipFile
};

var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    sourceDir, destDir, options);

// Check for skipped files
if (result.HasSpeciallyHandledFiles)
{
    Console.WriteLine("Skipped files due to size:");
    foreach (var file in result.SpeciallyHandledFiles)
    {
        Console.WriteLine($"  {Path.GetFileName(file)}");
    }
}
```

### Result Analysis

#### Working with SplitResult

```csharp
var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    sourceDir, destDir, options);

// Basic information
Console.WriteLine($"Operation completed in {result.Duration}");
Console.WriteLine($"Total bytes processed: {result.TotalBytesProcessed:N0}");
Console.WriteLine($"Created archives: {result.CreatedArchives.Count}");

// List created archives
foreach (var archive in result.CreatedArchives)
{
    var info = new FileInfo(archive);
    Console.WriteLine($"  {Path.GetFileName(archive)} ({info.Length:N0} bytes)");
}

// Check for specially handled files
if (result.HasSpeciallyHandledFiles)
{
    Console.WriteLine($"\nSpecial handling required for {result.SpeciallyHandledFiles.Count} files:");
    foreach (var file in result.SpeciallyHandledFiles)
    {
        Console.WriteLine($"  {Path.GetFileName(file)} - handled specially");
    }
}

```

## Supporting Classes

### ProgressInfo

Information provided during progress reporting.

```csharp
public class ProgressInfo
{
    public double PercentageComplete { get; set; }     // 0-100
    public string CurrentOperation { get; set; } = "";
    public long BytesProcessed { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
}
```

**Example Progress Handler:**

```csharp
var progress = new Progress<ProgressInfo>(info =>
{
    Console.WriteLine($"Progress: {info.PercentageComplete:F1}%");
    Console.WriteLine($"Files: {info.ProcessedFiles}/{info.TotalFiles}");
    Console.WriteLine($"Bytes: {info.BytesProcessed:N0}");
    Console.WriteLine($"Current: {info.CurrentOperation}");
    Console.WriteLine();
});
```

## Error Handling

### Common Exceptions

**ArgumentException**

- Invalid source or destination directory paths
- Invalid SplitOptions configuration

**ArgumentOutOfRangeException**

- MaxArchiveSize too small (< 1MB for SplitBySize)
- EstimatedCompressionRatio not between 0.1 and 1.0

**InvalidOperationException**

- File exceeds maximum size limit (when using ThrowException handling)
- I/O errors during operation

**OperationCanceledException**

- Operation cancelled via CancellationToken

### Error Handling Example

```csharp
try
{
    var options = new SplitOptions
    {
        ArchiveStrategy = ArchiveStrategy.SplitBySize,
        MaxArchiveSize = 50 * 1024 * 1024,
        LargeFileHandling = LargeFileHandling.CreateSeparateArchive
    };

    var result = await ZipSplitterWithProgress.CreateArchivesAsync(
        sourceDir, destDir, options, progress, cancellationToken);

    // Process successful result
    Console.WriteLine($"Success! Created {result.CreatedArchives.Count} archives");

    if (result.HasSpeciallyHandledFiles)
    {
        Console.WriteLine($"Note: {result.SpeciallyHandledFiles.Count} files required special handling");
    }
}
catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "MaxArchiveSize")
{
    Console.WriteLine("Error: Archive size limit is too small (minimum 1MB)");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Operation error: {ex.Message}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled by user");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"Access denied: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Best Practices

### Performance Considerations

1. **Use appropriate archive sizes**: Very small archives create overhead, very large archives may be unwieldy
2. **Monitor progress**: Use progress reporting for long operations
3. **Handle cancellation**: Always provide cancellation tokens for long operations
4. **Configure compression ratio carefully**: Test with sample data to determine optimal ratio

### Large File Strategies

1. **CreateSeparateArchive**: Best for mixed content with occasional large files
2. **SkipFile**: Best when large files can be handled separately
3. **CopyUncompressed**: Best when you need all files but can accept uncompressed large files
4. **ThrowException**: Best for strict size requirements

### Size Limit Types

1. **UncompressedData**: Easier to predict, works with known file sizes
2. **CompressedArchive**: More precise for storage requirements, requires compression ratio estimation

### Example Configuration Patterns

```csharp
// Conservative backup with strict size limits
var backupOptions = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxArchiveSize = 25 * 1024 * 1024, // 25MB
    SizeLimitType = SizeLimitType.CompressedArchive,
    EstimatedCompressionRatio = 0.8, // Conservative estimate
    LargeFileHandling = LargeFileHandling.CreateSeparateArchive
};

// Flexible archive creation
var flexibleOptions = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxArchiveSize = 100 * 1024 * 1024, // 100MB uncompressed
    SizeLimitType = SizeLimitType.UncompressedData,
    LargeFileHandling = LargeFileHandling.SkipFile
};

// Single large archive
var singleOptions = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SingleArchive,
    SingleArchiveName = "complete_backup.zip"
};
```
