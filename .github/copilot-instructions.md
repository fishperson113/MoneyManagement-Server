Use the latest version of C#, currently C# 13.
Apply formatting based on the .editorconfig file.
Use file-scoped namespace declarations.
Use single-line using directives.
Insert a newline before opening curly braces of control blocks.
Place the final return statement on its own line.
Use pattern matching and switch expressions when appropriate.
Use nameof instead of string literals for member references.
Add XML documentation to all public APIs, including <example> and <code> tags where applicable.
Declare variables as non-nullable and check for null only at entry points.
Use 'is null' or 'is not null' instead of '== null' or '!= null'.
Use xUnit SDK v3 for all unit tests.
Do not use Arrange, Act, or Assert comments in tests.
Use Moq for mocking in tests.
Follow naming conventions from nearby test files.
Do not modify global.json or package.json files unless explicitly requested.
Run tests using the build.sh script in each src subdirectory.
