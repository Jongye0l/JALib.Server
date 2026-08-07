using Serilog.Core;
using Serilog.Events;

namespace JALib.Server;

public class ThreadDisplayEnricher : ILogEventEnricher {
	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
		Thread currentThread = Thread.CurrentThread;
		string? threadName = currentThread.Name;
		int threadId = currentThread.ManagedThreadId;

		string displayString = string.IsNullOrWhiteSpace(threadName) ? $"Thread {threadId}" : $"{threadName}({threadId})";

		LogEventProperty property = propertyFactory.CreateProperty("Thread", displayString);
		logEvent.AddOrUpdateProperty(property);
	}
}