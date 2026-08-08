using System.IO.Compression;
using JALib.Server.Models;
using Serilog;
using Serilog.Sinks.File.GzArchive;
using ILogger = Serilog.ILogger;
using RollingInterval = Serilog.Sinks.FileEx.RollingInterval;

namespace JALib.Server;

public static class Program {
	public static Action<WebApplicationBuilder>? ConfigureBuilder;
	public static Action<WebApplication>? OnReady = _ => ConnectOtherLib.SetupModData();

	public static void Main(string[] args) {
		Thread.CurrentThread.Name ??= "Main";
		Settings.SetupInstance();

		Log.Logger = CreateLogger(null);
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		builder.WebHost.UseUrls("http://*:43910");
		builder.Host.UseSerilog();

		builder.Services.AddSingleton(Settings.Instance);
		builder.Services.AddControllersWithViews()
			.AddApplicationPart(typeof(Program).Assembly);
		ConfigureBuilder?.Invoke(builder);

		WebApplication app = builder.Build();

		app.UseStaticFiles();
		app.MapControllers();

		TokenData.LoadToken(Settings.Instance.TokenPath);
		ModData.LoadModData();

		OnReady?.Invoke(app);

		app.Run();
	}

	public static ILogger CreateLogger(string? name) {
		return new LoggerConfiguration()
			.Enrich.With<ThreadDisplayEnricher>()
#if DEBUG
			.MinimumLevel.Debug()
#else
			.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
#endif
			.WriteTo.Console(
				outputTemplate: "[{Level:u4} {Timestamp:HH:mm:ss.fff} {Thread}]" + (name == null ? "" : $" [{name}]") + " {Message:lj}{NewLine}{Exception}"
			)
			.WriteTo.FileEx(
				path: Path.Combine(Settings.Instance.LogPath, (name ?? "server") + ".log"),
				rollingInterval: RollingInterval.Day,
				hooks: new FileArchiveRollingHooks(CompressionLevel.SmallestSize, Path.Combine(Settings.Instance.LogPath, "archive")),
				outputTemplate: "[{Level:u4} {Timestamp:HH:mm:ss.fff} {Thread}] {Message:lj}{NewLine}{Exception}"
			).CreateLogger();
	}
}