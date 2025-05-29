# Changelog

All notable changes and improvements to the ZIP Splitter project are documented in this file.

## [2.1.0] - 2025-05-29

### Added
- ✅ **Enhanced Progress Demo**: New visual progress demonstration with 15MB of realistic files
- ✅ **Visual Progress Bar**: 50-character progress bar display (`█░░░░░░░░░░░░░░░░░░░`)
- ✅ **Demo Mode Selection**: Console app now offers Quick Demo vs Enhanced Progress Demo
- ✅ **Input Redirection Support**: Fixed Console.ReadKey() issues for automated runs
- ✅ **Realistic Demo Files**: Database, photos, PDFs, source code, and documentation files

### Improved
- **Progress Visualization**: Enhanced demo showcases progress reporting capabilities
- **Demo Experience**: Two distinct demo modes for different use cases
- **Cross-Platform Compatibility**: Better handling of console input in various environments
- **Documentation**: Updated to reflect enhanced demo features and progress reporting scope

## [2.1.0] - 2025-05-29

### Added
- ✅ **Enhanced Progress Demo**: New comprehensive demo mode with visual progress bar
- ✅ **Visual Progress Bar**: 50-character progress display (`█░░░░░░░░░░░░░░░░░░░`)
- ✅ **Realistic Demo Files**: 15MB of diverse file types (database, photos, documents, source code)
- ✅ **Dual Demo Modes**: Quick demo (2.9MB, ~0.04s) and Enhanced demo (15MB, ~0.68s)
- ✅ **Input Redirection Support**: Console apps work with piped input (e.g., `echo "2" | dotnet run`)

### Improved
- **Progress Reporting Scope**: Clarified that progress represents entire operation (0-100% across all archives)
- **Console Experience**: Enhanced visual feedback with real-time archive index and current file display
- **Demo Variety**: Realistic file types including database.db, photo1.jpg, manual.pdf, source.zip, etc.
- **Cross-Platform Input**: Proper handling of input redirection for automated scenarios

### Fixed
- **Console.ReadKey() Exception**: Fixed crashes when input is redirected by checking `Environment.UserInteractive` and `Console.IsInputRedirected`
- **Demo Reliability**: Both demo modes now work flawlessly in various execution environments

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
