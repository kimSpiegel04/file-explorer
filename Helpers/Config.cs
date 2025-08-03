namespace TestProject.Helpers
{
    // Defines the root directory of the file system accessible to the application
    // This can be set at runtime (e.g. dotnet run /Users/jsmith/Documents/Test/)
    public static class Config
    {
        public static string RootDirectory { get; set; } = "./";
    }
    
}
