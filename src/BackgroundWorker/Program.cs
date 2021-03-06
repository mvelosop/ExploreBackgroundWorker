﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BackgroundWorker
{
    public class Program
    {
        public static readonly string ApplicationContext = typeof(Program).Namespace;

        public static async Task<int> Main(string[] args)
        {
            try
            {
                var host = new HostBuilder()
                    .ConfigureHostConfiguration(configHost =>
                    {
                        Console.WriteLine("Setting up host configuration...");

                        configHost.SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("hostsettings.json", optional: true)
                            .AddEnvironmentVariables(prefix: "PREFIX_")
                            .AddCommandLine(args);
                    })
                    .ConfigureAppConfiguration((hostContext, configurationBuilder) =>
                    {
                        Console.WriteLine("Setting up app configuration...");

                        configurationBuilder.AddJsonFile("appsettings.json", optional: true)
                            .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true)
                            .AddEnvironmentVariables(prefix: "PREFIX_")
                            .AddCommandLine(args);
                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        Console.WriteLine("Setting up services configuration...");

                        ConfigureServices(hostContext, services);
                    })
                    .ConfigureLogging((hostContext, loggingBuilder) =>
                    {
                        Console.WriteLine("Setting up logging...");

                        var seqServerUrl = hostContext.Configuration["Serilog:SeqServerUrl"];

                        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .Enrich.WithProperty("ApplicationContext", ApplicationContext)
                            .Enrich.WithThreadId()
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .WriteTo.File(
                                $@"D:\home\LogFiles\{ApplicationContext}-.log",
                                rollingInterval: RollingInterval.Day,
                                retainedFileCountLimit: 15,
                                shared: true,
                                flushToDiskInterval: TimeSpan.FromSeconds(1))
                            .WriteTo.Seq(string.IsNullOrWhiteSpace(seqServerUrl) ? "http://localhost:5341/" : seqServerUrl)
                            .ReadFrom.Configuration(hostContext.Configuration)
                            .CreateLogger();

                        loggingBuilder.Services
                            .AddSingleton(new LoggerFactory().AddSerilog())
                            .AddLogging();
                    })
                    .UseConsoleLifetime()
                    .Build();

                Log.Information("----- STARTING UP APPLICATION ({ApplicationContext}) @{Date}", ApplicationContext, DateTime.Now);
                LogPackageVersions();

                await host.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", ApplicationContext);
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddHostedService<LifetimeEventsHostedService>();
            services.AddHostedService<TimedHostedService>();
        }

        private static string GetVersion(Assembly assembly)
        {
            try
            {
                return $"{assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version} ({assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split()[0]})";
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void LogPackageVersions()
        {
            var assemblies = new List<Assembly>();

            foreach (var assemblyName in typeof(Program).Assembly.GetReferencedAssemblies())
            {
                try
                {
                    // Try to load the referenced assembly...
                    assemblies.Add(Assembly.Load(assemblyName));
                }
                catch
                {
                    // Failed to load assembly. Skip it.
                }
            }

            var versionList = assemblies.Select(a => $"-{a.GetName().Name} - {GetVersion(a)}").OrderBy(value => value);

            Log.Logger.ForContext("PackageVersions", string.Join("\n", versionList)).Information("Package versions ({ApplicationContext})", ApplicationContext);
        }
    }
}
