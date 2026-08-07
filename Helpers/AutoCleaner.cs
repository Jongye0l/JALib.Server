using System.Collections.Concurrent;
using Serilog;

namespace JALib.Server.Helpers;

public class AutoCleaner {
	private static ConcurrentBag<AutoCleaner> _cleaners = [];
	protected object Obj;
	protected Action OnDispose;
	protected bool Used = true;

	static AutoCleaner() {
		Task.Run(CleanUp);
	}

	private static void CleanUp() {
		try {
			AutoCleaner[] cleaners = _cleaners.ToArray();
			foreach(AutoCleaner autoCleaner in cleaners) {
				if(!autoCleaner.Used) {
					autoCleaner.Dispose();
					continue;
				}
				autoCleaner.Used = false;
			}
			Task.Delay(300000).GetAwaiter().UnsafeOnCompleted(CleanUp); // 5 minute
		} catch (Exception e) {
			Log.Logger.Error(e, "Failed to clean up AutoCleaners");
		}
	}

	public AutoCleaner(object obj, Action onDispose) {
		Obj = obj;
		OnDispose = onDispose;
		_cleaners.Add(this);
	}

	public void Dispose() {
		OnDispose.Invoke();
		Log.Logger.Debug("AutoCleaner Disposing {Obj}", Obj);
		_cleaners.TryTake(out _);
	}
}

public class AutoCleaner<T> : AutoCleaner where T : class {
	public T Value {
		get {
			Used = true;
			return Obj.As<T>();
		}
		set {
			Used = true;
			Obj = value;
		}
	}

	public AutoCleaner(T value, Action onDispose) : base(value, onDispose) {
		Used = true;
	}
}