# Copilot Instructions

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a C# solution for demonstrating, improving, and testing a ZIP file splitter utility. The solution contains:

- **ZipSplitter.Core**: Core library with the ZIP splitting functionality
- **ZipSplitter.Console**: Console application for demonstrations
- **ZipSplitter.Tests**: Unit and integration tests

## Code Style and Conventions
- Follow C# naming conventions (PascalCase for public members, camelCase for private fields)
- Use async/await patterns where appropriate for I/O operations
- Implement proper error handling and logging
- Write comprehensive unit tests for all public methods
- Use dependency injection where applicable
- Include XML documentation comments for public APIs

## Architecture Principles
- Follow SOLID principles
- Separate concerns between business logic and presentation
- Use interfaces for testability
- Implement progress reporting using IProgress<T>
- Handle large files efficiently with streaming
- Provide cancellation support using CancellationToken

## Testing Guidelines
- Write tests that cover both happy path and error scenarios
- Mock external dependencies
- Test progress reporting functionality
- Include integration tests with real file operations
- Test edge cases like empty directories, large files, and permission issues
