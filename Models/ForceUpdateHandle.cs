using System.Text.Json;
using JALib.Server.Helpers;
using Serilog;

namespace JALib.Server.Models;

public struct ForceUpdateHandle : IJsonData {
	public VersionStruct LowVersion = VersionStruct.Null;
	public VersionStruct HighVersion = VersionStruct.Null;
	public bool ForceUpdate;

	public ForceUpdateHandle() {
	}

	public bool CheckVersion(VersionStruct version) {
		return version >= LowVersion && version <= HighVersion;
	}

	public void Load(ref Utf8JsonReader reader) {
		while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
			if(reader.TokenType != JsonTokenType.PropertyName) continue;
			string propertyName = reader.GetString()!;
			reader.Read();
			switch(propertyName) {
				case "lowVersion":
				case "version1":
					LowVersion.Setup(ref reader);
					break;
				case "highVersion":
				case "version2":
					HighVersion.Setup(ref reader);
					break;
				case "forceUpdate":
					ForceUpdate = reader.GetBoolean();
					break;
				default:
					Log.Logger.Warning("Unknown property in ForceUpdateHandle: {PropertyName}({TokenType})", propertyName, reader.TokenType);
					reader.Skip();
					break;
			}
		}
	}

	public void Save(Utf8JsonWriter writer) {
		writer.WriteStartObject();
		int maxField = Math.Max(LowVersion.DefaultFormatFieldCount, HighVersion.DefaultFormatFieldCount);
		if(maxField == 0) {
			writer.WriteNull("lowVersion"u8);
			writer.WriteNull("highVersion"u8);
		} else {
			Span<byte> versionStr = stackalloc byte[-1 + maxField * 11];
			if(LowVersion.Major == -1) writer.WriteNull("lowVersion"u8);
			else {
				LowVersion.WriteToString(versionStr, out int written);
				writer.WriteString("lowVersion"u8, versionStr[..written]);
			}
			if(HighVersion.Major == -1) writer.WriteNull("highVersion"u8);
			else {
				HighVersion.WriteToString(versionStr, out int written);
				writer.WriteString("highVersion"u8, versionStr[..written]);
			}
		}
		writer.WriteBoolean("forceUpdate"u8, ForceUpdate);
		writer.WriteEndObject();
	}

	public void WriteTo(JavaByteArrayDataOutput output) {
		output.WriteUTF(LowVersion.ToString());
		output.WriteUTF(HighVersion.ToString());
		output.WriteBoolean(ForceUpdate);
	}
}