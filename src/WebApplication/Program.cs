using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace WebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostContext, loggingBuilder) =>
                {
                    Console.WriteLine("Setting up logging...");

                    var seqServerUrl = hostContext.Configuration["Serilog:SeqServerUrl"];

                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .WriteTo.File(
                            $@"D:\home\LogFiles\WebApplication-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 15,
                            shared: true,
                            flushToDiskInterval: TimeSpan.FromSeconds(1))
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .CreateLogger();

                    loggingBuilder.Services
                        .AddSingleton(new LoggerFactory().AddSerilog())
                        .AddLogging();
                })
                .UseStartup<Startup>();
    }
}
