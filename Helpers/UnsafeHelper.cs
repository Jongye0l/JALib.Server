using System.Runtime.CompilerServices;

namespace JALib.Server.Helpers;

public static class UnsafeHelper {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T As<T>(this object obj) where T : class => Unsafe.As<T>(obj);
}