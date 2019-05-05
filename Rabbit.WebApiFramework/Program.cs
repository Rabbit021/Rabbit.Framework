using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Rabbit.WebApiFramework
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseSetting(WebHostDefaults.ApplicationKey, "Rabbit.WebApiFramework")
                .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "HostAssembly")
                .UseStartup<Startup>();
        }
    }
}