# Changelog

All notable changes and improvements to the ZIP Splitter project are documented in this file.

## [2.0.0] - 2025-05-29

### Added
- ✅ **Async/Await Support**: Complete rewrite with async/await for non-blocking I/O operations
- ✅ **Enhanced Progress Reporting**: New `ProgressInfo` class with detailed progress information
- ✅ **Cancellation Support**: Graceful cancellation using `CancellationToken`
- ✅ **Comprehensive Error Handling**: Proper exception types with meaningful messages
- ✅ **XML Documentation**: Complete API documentation with examples
- ✅ **Unit Testing**: Comprehensive test suite with 15+ test cases
- ✅ **Integration Testing**: Real-world scenario testing with various file types
- ✅ **Console Demo Application**: Interactive demo with progress visualization
- ✅ **VS Code Integration**: Tasks, launch configurations, and workspace setup
- ✅ **Project Structure**: Multi-project solution with proper separation of concerns

### Improved
- **Architecture**: Better separation of concerns with testable interfaces
- **Performance**: Streaming operations for efficient memory usage
- **Reliability**: Robust error handling and input validation
- **User Experience**: Real-time progress reporting with detailed status
- **File Naming**: Sequential archive naming with zero-padding (archive001.zip, etc.)
- **Path Handling**: Cross-platform path handling for directory structures

### Fixed
- **File Size Validation**: Prevents individual files larger than archive size limit
- **Directory Structure**: Preserves relative paths in ZIP archives
- **Memory Usage**: Efficient streaming instead of loading entire files
- **Progress Accuracy**: Precise byte-level progress tracking
- **Cross-Platform**: Works on Windows, macOS, and Linux

### Technical Details
- **Target Framework**: .NET 9.0
- **Dependencies**: System.IO.Compression (built-in)
- **Test Framework**: xUnit with Moq for mocking
- **Buffer Size**: 80KB for optimal I/O performance
- **Archive Size**: Minimum 1MB with configurable maximum

## [1.0.0] - Original Implementation

### Features
- Basic ZIP splitting functionality
- Simple progress reporting via `IProgress<double>`
- Console output for file operations
- Fixed 80KB buffer size
- Directory traversal with `SearchOption.AllDirectories`

### Limitations
- Synchronous I/O operations
- Basic error handling
- No cancellation support
- Limited progress information
- No unit tests
- Hardcoded archive naming
