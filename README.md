# ZIP Splitter Demo

A C# solution demonstrating an improved ZIP file splitter utility with progress reporting, error handling, and comprehensive testing.

## Overview

This solution provides functionality to split large directory structures into multiple ZIP archives based on a maximum size limit. It's useful for:

- Backing up large directories while respecting size constraints
- Splitting data for upload to services with file size limits
- Creating manageable archive sizes for storage or transfer

## Projects

### ZipSplitter.Core

The core library containing the main functionality:

- **ZipSplitterWithProgress**: Main class with async and sync methods for splitting directories
- **ProgressInfo**: Class for detailed progress reporting including percentage, archive index, and current operation

### ZipSplitter.Console

Console application demonstrating the functionality:

- Interactive demo mode with sample data
- Command-line argument support
- Real-time progress reporting
- Graceful cancellation support (Ctrl+C)

### ZipSplitter.Tests

Comprehensive test suite including:

- Unit tests for all public methods
- Integration tests with real file operations
- Performance tests with large numbers of files
- Error scenario testing
- Progress reporting validation

## Features

### Core Functionality

- ✅ **Enhanced API**: New `CreateArchivesAsync` method with flexible configuration options
- ✅ **Archive Strategies**:
  - **Split by Size**: Traditional splitting into multiple archives
  - **Single Archive**: Create one archive regardless of size
- ✅ **Large File Handling**: Multiple options for files exceeding size limits - **Throw Exception**: Strict size enforcement (default behavior)
  - **Create Separate Archive**: Put large files in their own archives
  - **Skip File**: Skip large files and report them
  - **Copy Uncompressed**: Copy large files without compression
- ✅ **Size Limit Types**:
  - **Uncompressed Data**: Limit based on original file sizes (default)
  - **Compressed Archive**: Limit based on estimated ZIP file sizes
- ✅ **Async/Await Support**: Efficient handling of I/O operations
- ✅ **Progress Reporting**: Detailed progress information via `IProgress<ProgressInfo>`
  - **Granular Updates**: Progress updates occur every 80KB chunk during file compression
  - **Smooth Progress**: For large files, you'll see continuous progress updates rather than waiting for complete files
  - **Real-time Feedback**: Approximately 192 progress updates for a 15MB file (15MB ÷ 80KB ≈ 192)
- ✅ **Cancellation Support**: Graceful cancellation using `CancellationToken`
- ✅ **Error Handling**: Comprehensive error handling with meaningful exceptions
- ✅ **Directory Structure Preservation**: Maintains relative paths in archives
- ✅ **Detailed Results**: Rich result information including handled files and warnings

### Improvements Over Original Code

- **Enhanced Configuration**: Flexible options via `SplitOptions` class
- **Better Architecture**: Separated concerns, interfaces for testability
- **Async Operations**: Non-blocking I/O operations
- **Enhanced Progress Reporting**: More detailed progress information
- **Robust Error Handling**: Proper exception handling and validation
- **Cancellation Support**: Ability to cancel long-running operations
- **Comprehensive Testing**: Unit and integration tests
- **Documentation**: XML documentation and README
- **Flexible Configuration**: Multiple strategies and options for different use cases

## Usage

### Console Application

Run the console application in two ways:

#### Interactive Demo Mode

```bash
dotnet run --project ZipSplitter.Console
```

This provides two demo options:

- **Quick Demo**: Fast demonstration with 2.9MB of sample files, completes in ~0.04 seconds
- **Enhanced Progress Demo**: Comprehensive showcase with 15MB of realistic files featuring:
  - Visual progress bar with 50-character display (`█░░░░░░░░░░░░░░░░░░░`)
  - Real-time percentage completion for entire operation (0-100%)
  - Current archive index and bytes processed
  - Current file being processed
  - Processes across 5 archives in ~0.68 seconds

#### Command Line Mode

```bash
dotnet run --project ZipSplitter.Console -- "C:\Source\Path" "C:\Destination\Path" 100
```

Parameters:

- Source directory path
- Destination directory path
- Maximum archive size in MB

### Programmatic Usage

#### Enhanced API (Recommended)

```csharp
using ZipSplitter.Core;

// Example 1: Create a single archive regardless of size
var singleArchiveOptions = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SingleArchive,
    SingleArchiveName = "complete_backup.zip"
};

var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    sourceDirectory: @"C:\MyFiles",
    destinationDirectory: @"C:\Archives",
    options: singleArchiveOptions,
    progress: new Progress<ProgressInfo>(info =>
        Console.WriteLine($"{info.PercentageComplete:F1}% - {info.CurrentOperation}")));

Console.WriteLine($"Created: {result.CreatedArchives[0]}");
Console.WriteLine($"Duration: {result.Duration}");
```

```csharp
// Example 2: Split archives with flexible large file handling
var splitOptions = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxSizeBytes = 100 * 1024 * 1024, // 100MB
    LargeFileHandling = LargeFileHandling.CreateSeparateArchive,
    SizeLimitType = SizeLimitType.UncompressedData
};

var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    sourceDirectory: @"C:\MyFiles",
    destinationDirectory: @"C:\Archives",
    options: splitOptions);

Console.WriteLine($"Created {result.CreatedArchives.Count} archives");
if (result.HasWarnings)
{
    Console.WriteLine($"Special handling for {result.SpeciallyHandledFiles.Count} files");
}
```

```csharp
// Example 3: Compressed size limits with skip option
var compressedSizeOptions = new SplitOptions
{
    ArchiveStrategy = ArchiveStrategy.SplitBySize,
    MaxSizeBytes = 50 * 1024 * 1024, // 50MB compressed
    SizeLimitType = SizeLimitType.CompressedArchive,
    LargeFileHandling = LargeFileHandling.SkipFile,
    EstimatedCompressionRatio = 0.8 // 20% compression expected
};

var result = await ZipSplitterWithProgress.CreateArchivesAsync(
    sourceDirectory: @"C:\MyFiles",
    destinationDirectory: @"C:\Archives",
    options: compressedSizeOptions);

if (result.SkippedFiles.Any())
{
    Console.WriteLine($"Skipped {result.SkippedFiles.Count()} large files");
}
```

#### Alternative Method Signatures

For simpler scenarios that don't need the full configuration options:

```csharp
using ZipSplitter.Core;

var progress = new Progress<ProgressInfo>(info =>
{
    Console.WriteLine($"Progress: {info.PercentageComplete:F1}% - {info.CurrentOperation}");
});

var cancellationTokenSource = new CancellationTokenSource();

try
{
    await ZipSplitterWithProgress.CreateSplitArchivesWithProgressAsync(
        sourceDirectory: @"C:\MyFiles",
        destinationDirectory: @"C:\Archives",
        maxArchiveSizeBytes: 100 * 1024 * 1024, // 100MB
        progress: progress,
        cancellationToken: cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Building and Testing

### Build the Solution

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Console Application

```bash
dotnet run --project ZipSplitter.Console
```

## Architecture

### Design Principles

- **Single Responsibility**: Each class has a focused purpose
- **Dependency Inversion**: Uses interfaces for testability
- **Open/Closed**: Extensible without modifying existing code
- **Error Handling**: Fail-fast with meaningful error messages
- **Performance**: Efficient streaming for large files

### Core Classes

#### `ZipSplitterWithProgress`

The main class providing archive creation functionality:

- `CreateArchivesAsync(sourceDirectory, destinationDirectory, options, progress, cancellationToken)`: Primary API with flexible configuration
- `CreateSplitArchivesWithProgressAsync(...)`: Alternative method for split-by-size scenarios

#### `SplitOptions`

Configuration class for archive creation:

- `ArchiveStrategy`: Choose between splitting by size or creating a single archive
- `MaxSizeBytes`: Maximum size limit (applies to split strategy)
- `LargeFileHandling`: How to handle files exceeding size limits
- `SizeLimitType`: Whether limits apply to uncompressed data or compressed archives
- `EstimatedCompressionRatio`: Used for compressed size calculations
- `SingleArchiveName`: Custom name for single archives

#### `SplitResult`

Results class providing detailed operation information:

- `CreatedArchives`: List of created archive file paths
- `SpeciallyHandledFiles`: Files that required special handling
- `TotalBytesProcessed`: Total bytes processed during operation
- `Duration`: Time taken for the operation
- `StrategyUsed`: Which strategy was actually used
- `HasWarnings`: Quick check for files needing special handling

#### `ProgressInfo`

Progress reporting class with detailed status:

- `PercentageComplete`: Overall completion percentage (0-100) for the **entire operation**
- `CurrentArchiveIndex`: Which archive is currently being created
- `BytesProcessed`: Total bytes processed so far across all archives
- `CurrentOperation`: Description of current operation

### Progress Reporting

The `ProgressInfo` class provides detailed information:

- `PercentageComplete`: Overall completion percentage (0-100) for the **entire operation** across all archives
- `CurrentArchiveIndex`: Which archive is currently being created
- `BytesProcessed`: Total bytes processed so far across all archives
- `CurrentOperation`: Description of current operation

**Note**: Progress percentage represents the entire job completion, not per-archive progress. The enhanced demo showcases this with a visual progress bar that fills from 0% to 100% as it processes multiple archives.

### Large File Handling Strategies

The library provides multiple options for handling files that exceed size limits:

1. **ThrowException**: Maintains backward compatibility by throwing an exception
2. **CreateSeparateArchive**: Creates individual archives for large files
3. **SkipFile**: Skips large files and reports them in results
4. **CopyUncompressed**: Copies large files without compression

### Size Limit Types

Choose how size limits are interpreted:

1. **UncompressedData**: Traditional approach based on original file sizes
2. **CompressedArchive**: Estimates based on expected ZIP file sizes using compression ratio

### Error Handling

- **ArgumentException**: Invalid input parameters
- **ArgumentOutOfRangeException**: Archive size too small or invalid options
- **InvalidOperationException**: File larger than max archive size or I/O errors
- **OperationCanceledException**: User cancellation
- **UnauthorizedAccessException**: File access permissions

## Requirements

- .NET 9.0 or later
- Windows, macOS, or Linux
- Sufficient disk space for source files + compressed archives

## Limitations

- When using `LargeFileHandling.ThrowException`, individual files cannot exceed the maximum archive size
- Memory usage scales with buffer size (default 80KB per file operation)
- Progress reporting frequency depends on file sizes and I/O speed
- Compressed size limits are estimates based on `EstimatedCompressionRatio` and may not be exact

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is provided as a demonstration and learning resource.
