namespace JALib.Server.Models;

public static class TokenData {
	private static readonly HashSet<string> Tokens = [];

	public static string? Token => Tokens.FirstOrDefault();

	public static void LoadToken(string path) {
		if(!File.Exists(path)) return;
		Tokens.Clear();
		foreach(string line in File.ReadAllLines(path)) {
			if(!string.IsNullOrWhiteSpace(line)) Tokens.Add(line.Trim());
		}
		ConnectOtherLib.RefreshToken();
	}

	public static bool CheckToken(string? token) => token != null && Tokens.Contains(token);
}