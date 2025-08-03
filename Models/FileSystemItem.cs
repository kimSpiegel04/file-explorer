namespace TestProject.Models {
    public class FileSystemItem
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public long? Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}