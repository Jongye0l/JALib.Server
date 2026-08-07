using System.Runtime.CompilerServices;
using System.Text.Json;
using JALib.Server.Helpers;

namespace JALib.Server.Models.DL;

public abstract class DownloadLink : IJsonData {
	public static DownloadLink CreateDownloadLink(ModData modData, JavaByteArrayDataInput input) {
		byte b;
		switch(b = input.ReadByte()) {
			case 0:
				return new GithubDownloadLink(modData.Name);
			case 1:
				CustomDownloadLink link = new();
				int size = input.ReadInt();
				link.Links.EnsureCapacity(size);
				Unsafe.SkipInit(out VersionStruct ver);
				for(int i = 0; i < size; i++) {
					byte[]? data = input.ReadBytes();
					if(data == null) ver.SetNull();
					else ver.Setup(data);
					link.Links[ver] = input.ReadUTF()!;
				}
				return link;
			default:
				throw new ArgumentException("Unknown download link type: " + b);
		}
	}

	public abstract string? GetLink(VersionStruct version);
	public abstract void WriteTo(JavaByteArrayDataOutput output);
	public abstract void Load(ref Utf8JsonReader reader);
	public abstract void Save(Utf8JsonWriter writer);
}