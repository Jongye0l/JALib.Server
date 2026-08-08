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
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		Instance ??= new Settings();
		Instance.Load(ref reader);
	}


	public void Load(ref Utf8JsonReader reader) {
		while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
			if(reader.TokenType != JsonTokenType.PropertyName) continue;
			string propertyName = reader.GetString()!;
			reader.Read();
			Load(propertyName, ref reader);
		}
	}

	public virtual void Load(string key, ref Utf8JsonReader reader) {
		switch(key) {
			case "logPath":
			case nameof(LogPath):
				LogPath = reader.GetString()!;
				break;
			case "modDataPath":
			case nameof(ModDataPath):
				ModDataPath = reader.GetString()!;
				break;
			case "adminManagerPath":
			case nameof(AdminManagerPath):
				AdminManagerPath = reader.GetString()!;
				break;
			case "tokenPath":
			case nameof(TokenPath):
				TokenPath = reader.GetString()!;
				break;
			case "otherLibURL":
			case nameof(OtherLibUrl):
				OtherLibUrl = reader.GetString()!;
				if(!OtherLibUrl.EndsWith('/')) OtherLibUrl += '/';
				break;
			case "modFilePath":
			case nameof(ModFilePath):
				ModFilePath = reader.GetString()!;
				break;
			default:
				Log.Logger.Warning("Unknown property in Settings: {PropertyName}({TokenType})", key, reader.TokenType);
				reader.Skip();
				break;
		}
	}

	public virtual void Save(Utf8JsonWriter writer) {
		writer.WriteStartObject();
		writer.WriteString(nameof(LogPath), LogPath);
		writer.WriteString(nameof(ModDataPath), ModDataPath);
		writer.WriteString(nameof(AdminManagerPath), AdminManagerPath);
		writer.WriteString(nameof(TokenPath), TokenPath);
		writer.WriteString(nameof(OtherLibUrl), OtherLibUrl);
		writer.WriteString(nameof(ModFilePath), ModFilePath);
		writer.WriteEndObject();
	}
}