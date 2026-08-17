using System.IO.Compression;
using System.Net.Http.Headers;
using JALib.Server.Helpers;
using JALib.Server.Models;
using JALib.Server.Models.DL;
using ILogger = Serilog.ILogger;

namespace JALib.Server;

public class ConnectOtherLib {
	private static readonly ILogger Logger = Program.CreateLogger("ConnectOtherLib");
	private static readonly HttpClient Client = new() {
		DefaultRequestHeaders = {
			{ "User-Agent", "JALib-Server/" + typeof(ConnectOtherLib).Assembly.GetName().Version + " (X11; Linux x86_64)" },
			{ "Authorization", "Bearer " + TokenData.Token }
		}
	};

	public static void RefreshToken() {
		Client.DefaultRequestHeaders.Remove("Authorization");
		Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + TokenData.Token);
	}

	public static async void AddModData(ModData modData) {
		try {
			JavaByteArrayDataOutput output = new(true);
			ModToBytes(output, modData);
			await SendModData(HttpMethod.Post, output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to add ModData: {ModName}", modData.Name);
		}
	}

	public static void ModToBytes(JavaByteArrayDataOutput output, ModData modData) {
		output.WriteUTF(modData.Name);
		output.WriteUTF(modData.Version.ToNullableString());
		output.WriteUTF(modData.BetaVersion.ToNullableString());
		output.WriteBoolean(modData.IsForceUpdate);
		output.WriteBoolean(modData.IsForceUpdateBeta);

		Dictionary<VersionStruct, bool> betaMap = modData.BetaMap;
		output.WriteInt(betaMap.Count);
		foreach(KeyValuePair<VersionStruct, bool> kvp in betaMap) {
			output.WriteUTF(kvp.Key.ToNullableString());
			output.WriteBoolean(kvp.Value);
		}

		ForceUpdateHandle[] forceUpdateHandles = modData.ForceUpdateHandles;
		output.WriteInt(forceUpdateHandles.Length);
		foreach(ForceUpdateHandle handle in forceUpdateHandles) handle.WriteTo(output);

		output.WriteUTF(modData.Homepage);
		output.WriteUTF(modData.Discord);
		modData.DownloadLink.WriteTo(output);
		output.WriteInt(modData.Gid);
	}

	public static async void SetVersion(ModData modData, VersionStruct version) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(0);
			output.WriteUTF(version.ToNullableString());
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set version for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetBetaVersion(ModData modData, VersionStruct version) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(1);
			output.WriteUTF(version.ToNullableString());
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set beta version for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetForceUpdate(ModData modData, bool forceUpdate) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(2);
			output.WriteBoolean(forceUpdate);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set forceUpdate for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetForceUpdateHandles(ModData modData, ForceUpdateHandle[] handles) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(3);
			output.WriteInt(handles.Length);
			foreach(ForceUpdateHandle handle in handles) handle.WriteTo(output);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set forceUpdateHandles for ModData: {ModName}", modData.Name);
		}
	}

	public static async void AddForceUpdateHandle(ModData modData, ForceUpdateHandle handle) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(4);
			handle.WriteTo(output);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to add forceUpdateHandle for ModData: {ModName}", modData.Name);
		}
	}

	public static async void RemoveForceUpdateHandle(ModData modData, int i) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(5);
			output.WriteInt(i);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to remove forceUpdateHandle for ModData: {ModName}", modData.Name);
		}
	}

	public static async void ChangeForceUpdateHandle(ModData modData, int i, ForceUpdateHandle handle) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(6);
			output.WriteInt(i);
			handle.WriteTo(output);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to change forceUpdateHandle for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetHomepage(ModData modData, string? homepage) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(7);
			output.WriteUTF(homepage);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set homepage for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetDownloadLink(ModData modData, DownloadLink downloadLink) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(8);
			downloadLink.WriteTo(output);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set downloadLink for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetGid(ModData modData, int gid) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(9);
			output.WriteInt(gid);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set gid for ModData: {ModName}", modData.Name);
		}
	}

	public static async void LoadLocalizations(ModData modData) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(10);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to load localizations for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetDiscord(ModData modData, string? discord) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(11);
			output.WriteUTF(discord);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set discord for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetForceUpdateBeta(ModData modData, bool forceUpdateBeta) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(12);
			output.WriteBoolean(forceUpdateBeta);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set forceUpdateBeta for ModData: {ModName}", modData.Name);
		}
	}

	public static async void SetBetaMap(ModData modData, VersionStruct version, bool? isBeta) {
		try {
			JavaByteArrayDataOutput output = new(true);
			output.WriteUTF(modData.Name);
			output.WriteByte(13);
			output.WriteUTF(version.ToString());
			output.WriteByte(isBeta == null ? unchecked((byte) -1) : isBeta.Value ? (byte) 1 : (byte) 0);
			await ChangeModData(output.ToArray());
		} catch (Exception e) {
			Logger.Error(e, "Failed to set betaMap for ModData: {ModName}", modData.Name);
		}
	}

	private static Task ChangeModData(byte[] data) => SendModData(HttpMethod.Put, data);

	private static async Task SendModData(HttpMethod method, byte[] data) {
		using HttpRequestMessage request = new(method, Settings.Instance.OtherLibUrl + "admin/modData");
		request.Content = new ByteArrayContent(data);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
		using HttpResponseMessage response = await Client.SendAsync(request);
		response.EnsureSuccessStatusCode();
	}

	public static async void SetupModData() {
		try {
			using HttpRequestMessage request = new(HttpMethod.Get, Settings.Instance.OtherLibUrl + "admin/modData");
			using HttpResponseMessage response = await Client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			Stream stream = await response.Content.ReadAsStreamAsync();
			GZipStream gzipStream = new(stream, CompressionMode.Decompress);
			using JavaByteArrayDataInput input = new(gzipStream);
			int length = input.ReadInt();
			for(int i = 0; i < length; i++) {
				string name = input.ReadUTF()!;
				ModData.CreateMod(name, input);
			}
		} catch (Exception e) {
			Logger.Error(e, "Failed to setup ModData");
		}
	}
}