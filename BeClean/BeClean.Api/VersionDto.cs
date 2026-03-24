namespace BeClean.Api
{
    public class VersionDto
    {
        public DateTime BuildDate { get; set; }
        public string Version { get; set; } = null!;
        public string GitCommit { get; set; } = null!;
        public string GitBranch { get; set; } = null!;
        public string DeployedBy { get; set; } = null!;
        public string BuildMachine { get; set; } = null!;
    }
}
