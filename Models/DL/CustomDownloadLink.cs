using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JALib.Server.Helpers;

namespace JALib.Server.Models.DL;

public class CustomDownloadLink : DownloadLink {
	public Dictionary<VersionStruct, string> Links = new();

	public override string GetLink(VersionStruct version) => Links[version];

	public override void WriteTo(JavaByteArrayDataOutput output) {
		output.WriteByte(1);
		output.WriteInt(Links.Count);
		foreach((VersionStruct ver, string link) in Links) {
			output.WriteUTF(ver.ToString());
			output.WriteUTF(link);
		}
	}

	public override void Load(ref Utf8JsonReader reader) {
		Unsafe.SkipInit(out VersionStruct ver);
		while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
			if(reader.TokenType != JsonTokenType.PropertyName) continue;
			ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
			reader.Read();
			if(propertyName.SequenceEqual("type"u8)) continue;
			ver.Setup(propertyName);
			Links[ver] = reader.GetString()!;
		}
	}

	public override void Save(Utf8JsonWriter writer) {
		writer.WriteStartObject();
		{
			writer.WriteString("type"u8, "custom");
			Span<byte> versionStr = stackalloc byte[43];
			foreach((VersionStruct ver, string link) in Links) {
				ver.WriteToString(versionStr, out int written);
				writer.WriteString(versionStr[..written], link);
			}
		}
		writer.WriteEndObject();
	}
}