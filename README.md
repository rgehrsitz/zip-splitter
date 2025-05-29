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
- ✅ **Async/Await Support**: Efficient handling of I/O operations
- ✅ **Progress Reporting**: Detailed progress information via `IProgress<ProgressInfo>`
- ✅ **Cancellation Support**: Graceful cancellation using `CancellationToken`
- ✅ **Error Handling**: Comprehensive error handling with meaningful exceptions
- ✅ **Directory Structure Preservation**: Maintains relative paths in archives
- ✅ **File Size Validation**: Prevents individual files larger than max archive size

### Improvements Over Original Code
- **Better Architecture**: Separated concerns, interfaces for testability
- **Async Operations**: Non-blocking I/O operations
- **Enhanced Progress Reporting**: More detailed progress information
- **Robust Error Handling**: Proper exception handling and validation
- **Cancellation Support**: Ability to cancel long-running operations
- **Comprehensive Testing**: Unit and integration tests
- **Documentation**: XML documentation and README

## Usage

### Console Application

Run the console application in two ways:

#### Interactive Demo Mode
```bash
dotnet run --project ZipSplitter.Console
```
This creates sample files and demonstrates the splitting functionality.

#### Command Line Mode
```bash
dotnet run --project ZipSplitter.Console -- "C:\Source\Path" "C:\Destination\Path" 100
```
Parameters:
- Source directory path
- Destination directory path  
- Maximum archive size in MB

### Programmatic Usage

#### Async Version (Recommended)
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

#### Synchronous Version (Legacy Compatibility)
```csharp
var progress = new Progress<double>(percentage =>
{
    Console.WriteLine($"Progress: {percentage:F2}%");
});

ZipSplitterWithProgress.CreateSplitArchivesWithProgress(
    sourceDirectory: @"C:\MyFiles",
    destinationDirectory: @"C:\Archives", 
    maxArchiveSizeBytes: 100 * 1024 * 1024, // 100MB
    progress: progress);
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

### Progress Reporting
The `ProgressInfo` class provides detailed information:
- `PercentageComplete`: Overall completion percentage (0-100)
- `CurrentArchiveIndex`: Which archive is currently being created
- `BytesProcessed`: Total bytes processed so far
- `CurrentOperation`: Description of current operation

### Error Handling
- **ArgumentException**: Invalid input parameters
- **ArgumentOutOfRangeException**: Archive size too small
- **InvalidOperationException**: File larger than max archive size or I/O errors
- **OperationCanceledException**: User cancellation
- **UnauthorizedAccessException**: File access permissions

## Requirements

- .NET 9.0 or later
- Windows, macOS, or Linux
- Sufficient disk space for source files + compressed archives

## Limitations

- Individual files cannot exceed the maximum archive size
- Memory usage scales with buffer size (default 80KB per file operation)
- Progress reporting frequency depends on file sizes and I/O speed

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is provided as a demonstration and learning resource.
