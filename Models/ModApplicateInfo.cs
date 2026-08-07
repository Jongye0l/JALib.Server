namespace JALib.Server.Models;

public class ModApplicateInfo {
	public string Name = null!;
	public string Version = null!;
	public bool IsWindows;
	public bool Redirected;
	public string? OldVersion;
	public string? UpdateType;
}