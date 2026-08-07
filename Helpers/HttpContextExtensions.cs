namespace JALib.Server.Helpers;

public static class HttpContextExtensions {
	public static string GetClientIp(this HttpContext context) {
		string? ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
		if(string.IsNullOrEmpty(ipAddress) || string.Equals(ipAddress, "unknown", StringComparison.OrdinalIgnoreCase))
			return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		int comma = ipAddress.IndexOf(',');
		return comma == -1 ? ipAddress : ipAddress[..comma];
	}
}
