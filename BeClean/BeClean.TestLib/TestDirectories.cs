namespace BeClean.TestLib
{
    public static class TestDirectories
    {
        /// <summary>
        /// Finds the project directory for the assembly that contains <paramref name="anchorType"/>.
        /// Works whether the build output is next to the project (classic) or centralised
        /// under .artifacts/ (ArtifactsPath layout).
        /// </summary>
        public static string GetProjectDirectory(Type anchorType)
        {
            string? dir = AppContext.BaseDirectory;
            while (dir != null && !Directory.GetFiles(dir, "*.sln").Any())
                dir = Directory.GetParent(dir)?.FullName;

            if (dir == null)
                throw new Exception("Could not find the solution directory.");

            var assemblyName = anchorType.Assembly.GetName().Name!;
            var projectDir = Path.Combine(dir, assemblyName);

            if (Directory.Exists(projectDir) && Directory.GetFiles(projectDir, "*.csproj").Any())
                return projectDir;

            throw new Exception($"Could not find the project directory '{assemblyName}' under '{dir}'.");
        }
    }
}
