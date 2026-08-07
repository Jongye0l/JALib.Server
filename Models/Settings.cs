using System.Text.Json;
using Serilog;

namespace JALib.Server.Models;

public class Settings : IJsonData {
	public static Settings Instance = null!;
	public string LogPath = "logs";
	public string ModDataPath = "moddata";
	public string AdminManagerPath = "admin";
	public string TokenPath = "token";
	public string OtherLibUrl = string.Empty;
	public string ModFilePath = "mods";

	public static void SetupInstance() {
		byte[] data = File.ReadAllBytes("settings.json");
		Utf8JsonReader reader = new(data);
		Settings settings = new();
		settings.Load(ref reader);
		Instance = settings;
	}


	public void Load(ref Utf8JsonReader reader) {
		while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
			if(reader.TokenType != JsonTokenType.PropertyName) continue;
			string propertyName = reader.GetString()!;
			reader.Read();
			switch(propertyName) {
				case "logPath":
					LogPath = reader.GetString()!;
					break;
				case "modDataPath":
					ModDataPath = reader.GetString()!;
					break;
				case "adminManagerPath":
					AdminManagerPath = reader.GetString()!;
					break;
				case "tokenPath":
					TokenPath = reader.GetString()!;
					break;
				case "otherLibURL":
					OtherLibUrl = reader.GetString()!;
					break;
				case "modFilePath":
					ModFilePath = reader.GetString()!;
					break;
				default:
					Log.Logger.Warning("Unknown property in Settings: {PropertyName}({TokenType})", propertyName, reader.TokenType);
					reader.Skip();
					break;
			}
		}
	}

	public void Save(Utf8JsonWriter writer) {
		writer.WriteStartObject();
		writer.WriteString(nameof(LogPath), LogPath);
		writer.WriteString(nameof(ModDataPath), ModDataPath);
		writer.WriteString(nameof(AdminManagerPath), AdminManagerPath);
		writer.WriteString(nameof(TokenPath), TokenPath);
		writer.WriteString("otherLibURL", OtherLibUrl);
		writer.WriteString(nameof(ModFilePath), ModFilePath);
		writer.WriteEndObject();
	}
}