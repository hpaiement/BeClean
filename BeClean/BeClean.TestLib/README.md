# BeClean.TestLib

Test infrastructure utilities for BeClean-based projects.

## Features

- **`TestDirectories.GetProjectDirectory(anchorType)`** — resolves the on-disk project directory for a test assembly at runtime. Works with both standard output layouts and the .NET centralized `artifacts/` build output, by walking up from the assembly location to find the `.sln` file and then locating the project folder by assembly name.

## Requirements

- .NET 10+
