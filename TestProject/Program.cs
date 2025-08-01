using TestProject.Helpers;

namespace TestProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            var app = builder.Build();

            // Read from environment variable or command line arg
            if (args.Length > 0)
            {
                Config.RootDirectory = args[0];
                Console.WriteLine($"Root directory set to: {Config.RootDirectory}");
            }
            else
            {
                Console.WriteLine("⚠️No root directory provided. Using '/' as fallback.");
            }

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}