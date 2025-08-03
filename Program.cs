using TestProject.Helpers;

namespace TestProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /// Initialize web app builder
            var builder = WebApplication.CreateBuilder(args);

            /// Add controlller services 
            builder.Services.AddControllers();

            var app = builder.Build();

            /// Scope item: Allow configuration of a server-side home/root directory via a variable
            /// Read from environment variable or CLI arg
            /// Falls back to '/' (directory from dotnet run)
            if (args.Length > 0)
            {
                Config.RootDirectory = args[0];
                Console.WriteLine($"Root directory set to: {Config.RootDirectory}");
            }
            else
            {
                Console.WriteLine("⚠️No root directory provided. Using '/' as fallback.");
            }

            /// Configure the HTTP request pipeline. Enforce HTTPS, serve default files (index.html) and js, css files, 
            /// map api controllers, start web server 

            app.UseHttpsRedirection(); 

            app.UseDefaultFiles(); 

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}