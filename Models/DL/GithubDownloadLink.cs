using System.Buffers;
using System.Text;
using System.Text.Json;
using JALib.Server.Helpers;
using Serilog;

namespace JALib.Server.Models.DL;

public class GithubDownloadLink(string modName) : DownloadLink {
	public string ModName = modName;

	public GithubDownloadLink() : this(null!) {
	}

	public override string GetLink(VersionStruct version) => "https://github.com/Jongye0l/" + ModName + "/releases/download/v" + version + "/" + ModName + ".zip";
	public string GetSourceLink(VersionStruct version) => "https://github.com/Jongye0l/" + ModName + "/tree/v" + version;

	public override void WriteTo(JavaByteArrayDataOutput output) {
		output.WriteByte(0);
	}

	public override void Load(ref Utf8JsonReader reader) {
		while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
			if(reader.TokenType != JsonTokenType.PropertyName) continue;
			ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
			reader.Read();
			if(propertyName.SequenceEqual("modName"u8)) ModName = reader.GetString()!;
			else if(!propertyName.SequenceEqual("type"u8)) {
				Log.Logger.Warning("Unknown property in GithubDownloadLink: {PropertyName}({TokenType})", Encoding.UTF8.GetString(propertyName), reader.TokenType);
				reader.Skip();
			}
		}
	}

	public override void Save(Utf8JsonWriter writer) {
		writer.WriteStartObject();
		{
			writer.WriteString("type"u8, "github");
			writer.WriteString("modName"u8, ModName);
		}
		writer.WriteEndObject();
	}
}