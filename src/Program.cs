using System.IO;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ServerHttp.Classes;
using ServerHttp.Interfaces;

var build = new ConfigurationBuilder();
build.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);
    

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(build.Build())
    .WriteTo.Console()
    .CreateLogger();


Parser.Default.ParseArguments<CommandLineArgs>(args).WithParsed(opts =>
{
    var app = Host.CreateDefaultBuilder()
        .ConfigureServices((context, service) =>
        {
            service.AddSingleton(new CommandLineArgs {port = opts.port});
            service.AddSingleton<IServerHttp, ServerHttpHandler>();
            service.AddHostedService<Worker>();
        })
        .UseSerilog()
        .Build();
    
        app.Run();
})
.WithNotParsed(erros => 
{
    Log.Logger.Error("Failed to convert port argument");
});

