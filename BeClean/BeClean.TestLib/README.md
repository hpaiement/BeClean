# BeClean.TestLib

Test infrastructure utilities for BeClean-based projects.

## Features

- **`TestDirectories.GetProjectDirectory(anchorType)`** — resolves the on-disk project directory for a test assembly at runtime. Works with both standard output layouts and the .NET centralized `artifacts/` build output, by walking up from the assembly location to find the `.sln` file and then locating the project folder by assembly name.
- **`IDataFixture<TContext>`** — contract for a reusable set of test data seeded into a database context (`Seed(TContext dbContext)`). Database fixtures can seed them on demand so each test only gets the data it needs.

## Requirements

- .NET 10+
