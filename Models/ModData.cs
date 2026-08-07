using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JALib.Server.Helpers;
using JALib.Server.Models.DL;
using Serilog;

namespace JALib.Server.Models;

public class ModData : IJsonData {
	private static readonly Dictionary<string, ModData> Registry = new(StringComparer.OrdinalIgnoreCase); // TODO: Add AutoCleaner

	public string Name = null!;
	public VersionStruct Version = VersionStruct.Null;
	public VersionStruct BetaVersion = VersionStruct.Null;
	public bool IsForceUpdate;
	public bool IsForceUpdateBeta;
	public readonly Dictionary<VersionStruct, bool> BetaMap = new();
	public ForceUpdateHandle[] ForceUpdateHandles = [];
	public Language[] AvailableLanguages = [];
	public string? Homepage;
	public string? Discord;
	public DownloadLink DownloadLink = null!;
	public int Gid;
	public Dictionary<string, Dictionary<Language, string?>> Localizations = new();

	public static void LoadModData() {
		string dir = Settings.Instance.ModDataPath;
		if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
		foreach(string file in Directory.GetFiles(dir)) {
			if(file.EndsWith(".old") && File.Exists(file[..^4])) continue;
			ModData modData = new();
			try {
				byte[] buffer = File.ReadAllBytes(file);
				Utf8JsonReader reader = new(buffer);
				modData.Load(ref reader);
			} catch (Exception e) {
				Log.Logger.Error(e, "Failed to load ModData from file: {File}", file);
				if(File.Exists(file + ".old")) {
					try {
						byte[] buffer = File.ReadAllBytes(file + ".old");
						Utf8JsonReader reader = new(buffer);
						modData.Load(ref reader);
						Log.Logger.Information("Loaded ModData from backup file: {File}", file + ".old");
					} catch (Exception ex) {
						Log.Logger.Error(ex, "Failed to load ModData from backup file: {File}", file + ".old");
						continue;
					}
				}
			}
			Registry[modData.Name] = modData;
		}
	}

	public static ModData CreateMod(string name, JavaByteArrayDataInput input) {
		ModData modData = GetModData(name)!;
		if(modData == null!) {
			modData = new ModData();
			Registry[name] = modData;
		}
		modData.Name = name;
		byte[]? versionString = input.ReadBytes();
		if(versionString == null) modData.Version.SetNull();
		else modData.Version.Setup(versionString);
		versionString = input.ReadBytes();
		if(versionString == null) modData.BetaVersion.SetNull();
		else modData.BetaVersion.Setup(versionString);
		modData.IsForceUpdate = input.ReadBoolean();
		modData.IsForceUpdateBeta = input.ReadBoolean();
		int size = input.ReadInt();
		Unsafe.SkipInit(out VersionStruct newVersion);
		modData.BetaMap.Clear();
		for(int i = 0; i < size; i++) {
			versionString = input.ReadBytes()!;
			newVersion.Setup(versionString);
			bool isBeta = input.ReadBoolean();
			modData.BetaMap[newVersion] = isBeta;
		}
		ForceUpdateHandle[] handles = new ForceUpdateHandle[input.ReadInt()];
		for(int i = 0; i < handles.Length; i++) {
			versionString = input.ReadBytes();
			if(versionString == null) handles[i].LowVersion.SetNull();
			else handles[i].LowVersion.Setup(versionString);
			versionString = input.ReadBytes();
			if(versionString == null) handles[i].HighVersion.SetNull();
			else handles[i].HighVersion.Setup(versionString);
			handles[i].ForceUpdate = input.ReadBoolean();
		}
		modData.ForceUpdateHandles = handles;
		modData.Homepage = input.ReadUTF();
		modData.Discord = input.ReadUTF();
		modData.DownloadLink = DownloadLink.CreateDownloadLink(modData, input);
		modData.Gid = input.ReadInt();
		modData.Save();
		return modData;
	}

	public static ModData? GetModData(string name) => Registry.GetValueOrDefault(name);
	public static void RegisterMod(ModData modData) => Registry[modData.Name] = modData;
	public static List<ModData> GetModDataList() => Registry.Values.ToList();

	public bool CheckForceUpdate(VersionStruct currentVersion) {
		foreach(ForceUpdateHandle handle in ForceUpdateHandles) {
			if(handle.CheckVersion(currentVersion)) return handle.ForceUpdate;
		}
		return false;
	}

	public async void Save() {
		try {
			string modDataPath = Settings.Instance.ModDataPath;
			string path = Path.Combine(modDataPath, Name);

			bool exists = File.Exists(path);
			if(exists) File.Move(path, path + ".old", true);

			await using(FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
				await using Utf8JsonWriter writer = new(fs);
				Save(writer);
			}

			if(exists) File.Delete(path + ".old");
		} catch (Exception e) {
			Log.Logger.Error(e, "Failed to save ModData for {Name}", Name);
		}
	}

	public override string ToString() => Name + " ModData";

	public void Load(ref Utf8JsonReader reader) {
		while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
			if(reader.TokenType != JsonTokenType.PropertyName) continue;
			string propertyName = reader.GetString()!;
			reader.Read();
			switch(propertyName) {
				case "name":
					Name = reader.GetString()!;
					break;
				case "version":
					Version.Setup(ref reader);
					break;
				case "betaVersion":
					BetaVersion.Setup(ref reader);
					break;
				case "forceUpdate":
					IsForceUpdate = reader.GetBoolean();
					break;
				case "forceUpdateBeta":
					IsForceUpdateBeta = reader.GetBoolean();
					break;
				case "betaMap":
					Unsafe.SkipInit(out VersionStruct version);
					while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
						if(reader.TokenType != JsonTokenType.PropertyName) continue;
						if(reader.HasValueSequence) version.Setup(reader.ValueSequence);
						else version.Setup(reader.ValueSpan);
						reader.Read();
						BetaMap[version] = reader.GetBoolean();
					}
					break;
				case "forceUpdateHandles":
					List<ForceUpdateHandle>? handles = null;
					Unsafe.SkipInit(out ForceUpdateHandle handle);
					while(reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
						if(reader.TokenType != JsonTokenType.StartObject) continue;
						handles ??= [];
						handle.Load(ref reader);
						handles.Add(handle);
					}
					ForceUpdateHandles = handles == null ? [] : handles.ToArray();
					break;
				case "availableLanguages":
					List<Language>? languages = null;
					while(reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
						if(reader.TokenType != JsonTokenType.String) {
							Log.Logger.Warning("Unknown token type in ModData.AvailableLanguages: {TokenType}", reader.TokenType);
							continue;
						}
						if(!Enum.TryParse(reader.GetString(), out Language language)) {
							Log.Logger.Warning("Unknown language in ModData.AvailableLanguages: {Language}", reader.GetString());
							continue;
						}
						languages ??= [];
						languages.Add(language);
					}
					AvailableLanguages = languages == null ? [] : languages.ToArray();
					break;
				case "homepage":
					Homepage = reader.GetString();
					break;
				case "discord":
					Discord = reader.GetString();
					break;
				case "downloadLink":
					Utf8JsonReader readerCopy = reader;
					string? type = null;
					while(readerCopy.Read() && readerCopy.TokenType != JsonTokenType.EndObject) {
						if(readerCopy.TokenType != JsonTokenType.PropertyName) continue;
						ReadOnlySpan<byte> value = readerCopy.HasValueSequence ? readerCopy.ValueSequence.ToArray() : readerCopy.ValueSpan;
						readerCopy.Read();
						if(value.SequenceEqual("type"u8)) {
							type = readerCopy.GetString();
							break;
						}
						readerCopy.Skip();
					}
					DownloadLink = type switch {
						null => throw new ArgumentException("DownloadLink is need Type."),
						"github" => new GithubDownloadLink(),
						"custom" => new CustomDownloadLink(),
						_ => throw new ArgumentException("Unknown DownloadLink Type: " + type)
					};
					DownloadLink.Load(ref reader);
					break;
				case "gid":
					Gid = reader.GetInt32();
					break;
				case "localizations":
					while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
						if(reader.TokenType != JsonTokenType.PropertyName) continue;
						string value = reader.GetString()!;
						reader.Read();
						Dictionary<Language, string?> dict = new();
						while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
							if(reader.TokenType != JsonTokenType.PropertyName) continue;
							if(!Enum.TryParse(reader.GetString(), out Language language)) throw new ArgumentException("Unknown Language: " + reader.GetString());
							reader.Read();
							dict[language] = reader.GetString();
						}
						Localizations[value] = dict;
					}
					break;
				default:
					Log.Logger.Warning("Unknown property in ModData: {PropertyName}({TokenType})", propertyName, reader.TokenType);
					reader.Skip();
					break;
			}
		}
	}

	public void Save(Utf8JsonWriter writer) {
		writer.WriteStartObject();
		{
			writer.WriteString("name"u8, Name);
			Span<byte> versionStr = stackalloc byte[43];
			if(Version.Major == -1) writer.WriteNull("version"u8);
			else {
				Version.WriteToString(versionStr, out int written);
				writer.WriteString("version"u8, versionStr[..written]);
			}
			if(BetaVersion.Major == -1) writer.WriteNull("betaVersion"u8);
			else {
				BetaVersion.WriteToString(versionStr, out int written);
				writer.WriteString("betaVersion"u8, versionStr[..written]);
			}
			writer.WriteBoolean("forceUpdate"u8, IsForceUpdate);
			writer.WriteBoolean("forceUpdateBeta"u8, IsForceUpdateBeta);

			writer.WriteStartObject("betaMap"u8);
			{
				foreach((VersionStruct ver, bool isBeta) in BetaMap) {
					ver.WriteToString(versionStr, out int written);
					writer.WriteBoolean(versionStr[..written], isBeta);
				}
			}
			writer.WriteEndObject();

			writer.WriteStartArray("forceUpdateHandles"u8);
			{
				foreach(ForceUpdateHandle handle in ForceUpdateHandles) {
					handle.Save(writer);
				}
			}
			writer.WriteEndArray();

			writer.WriteStartArray("availableLanguages"u8);
			foreach(Language language in AvailableLanguages) {
				writer.WriteStringValue(language.ToString());
			}
			writer.WriteEndArray();

			writer.WriteString("homepage"u8, Homepage);
			writer.WriteString("discord"u8, Discord);

			writer.WritePropertyName("downloadLink"u8);
			DownloadLink.Save(writer);

			writer.WriteNumber("gid"u8, Gid);

			writer.WriteStartObject("localizations"u8);
			foreach((string key, Dictionary<Language, string?> dict) in Localizations) {
				writer.WriteStartObject(key);
				{
					foreach((Language language, string? value) in dict) {
						writer.WriteString(language.ToString(), value);
					}
				}
				writer.WriteEndObject();
			}
			writer.WriteEndObject();
		}
	}
}