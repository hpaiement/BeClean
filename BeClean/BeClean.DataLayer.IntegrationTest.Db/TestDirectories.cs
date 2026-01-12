namespace BeClean.DataLayer.IntegrationTest.Db
{
    public static class TestDirectories
    {
        public static string ProjectDirectory
        {
            get
            {
                string? currentDirectory = Environment.CurrentDirectory;
                while (!Directory.GetFiles(currentDirectory, "*.csproj").Any())
                {
                    currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
                    if (currentDirectory == null)
                    {
                        throw new Exception("Could not find the project directory.");
                    }
                }

                return currentDirectory;
            }
        }
    }
}
