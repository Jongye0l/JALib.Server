using System.Runtime.CompilerServices;
using JALib.Server.Helpers;
using JALib.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace JALib.Server.Controllers;

[ApiController]
[Route("")]
public class JALibController : ControllerBase {
	private readonly ILogger<JALibController> _logger;

	public JALibController(ILogger<JALibController> logger) {
		_logger = logger;
	}

	[HttpPost("exceptionInfo")]
	public IActionResult ExceptionInfo() {
		// TODO
		return Ok();
	}

	[HttpGet("modInfo/{name}/{version}/{beta:int}")]
	public IActionResult GetModInfo(string name, string version, int beta) {
		_logger.LogInformation("[{Ip}] GetModInfo: {Name} {Version}, beta: {Beta}", HttpContext.GetClientIp(), name, version, beta == 1);
		ModData? modData = ModData.GetModData(name);
		JavaByteArrayDataOutput output = new();

		VersionStruct ver = modData == null ? VersionStruct.Null : beta == 1 ? modData.BetaVersion : modData.Version;
		output.WriteBoolean(!ver.IsNull());

		if(!ver.IsNull()) {
			output.WriteUTF(ver.ToString());
			output.WriteBoolean((beta != 0 ? modData!.IsForceUpdateBeta : modData!.IsForceUpdate) || modData.CheckForceUpdate(new VersionStruct(version)));

			Language[] languages = modData.AvailableLanguages;
			output.WriteByte((byte) languages.Length);
			foreach(Language language in languages) output.WriteByte((byte) language);

			output.WriteUTF(modData.Homepage);
			output.WriteUTF(modData.Discord);
			output.WriteInt(modData.Gid);
		}

		return File(output.ToArray(), "application/octet-stream");
	}

	[HttpGet("modInfoV2/{name}/{version}")]
	public IActionResult GetModInfoV2(string name, string version) {
		_logger.LogInformation("[{Ip}] GetModInfo(V2): {Name} {Version}, beta: null", HttpContext.GetClientIp(), name, version);
		ModData? modData = ModData.GetModData(name);
		if(modData == null) return NotFound();

		Unsafe.SkipInit(out VersionStruct curVer);
		if(version.Equals("latest", StringComparison.CurrentCultureIgnoreCase)) {
			curVer = modData.Version;
			if(curVer.IsNull()) curVer = modData.BetaVersion;
		} else curVer.Setup(version);

		bool beta = !curVer.IsNull() && modData.BetaMap.ContainsKey(curVer);
		return ProcessModInfoV2(modData, curVer, beta);
	}

	[HttpGet("modInfoV2/{name}/{version}/{beta}")]
	public IActionResult GetModInfoV2(string name, string version, int beta) {
		_logger.LogInformation("[{Ip}] GetModInfo(V2): {Name} {Version}, beta: {Beta}", HttpContext.GetClientIp(), name, version, beta == 1);
		ModData? modData = ModData.GetModData(name);
		if(modData == null) return NotFound();

		VersionStruct targetVer = version.Equals("latest", StringComparison.OrdinalIgnoreCase)
			                          ? beta == 0 ? modData.Version : modData.BetaVersion
			                          : new VersionStruct(version);

		return ProcessModInfoV2(modData, targetVer, beta != 0);
	}

	private FileContentResult ProcessModInfoV2(ModData modData, VersionStruct version, bool beta) {
		JavaByteArrayDataOutput output = new();
		bool success = !modData.BetaVersion.IsNull();
		output.WriteBoolean(success);

		if(success && !version.IsNull()) {
			output.WriteUTF(modData.Version.ToNullableString());
			output.WriteUTF(modData.BetaVersion.ToNullableString());
			output.WriteBoolean(modData.IsForceUpdate);
			output.WriteBoolean(modData.IsForceUpdateBeta);

			bool needUpdate = version.IsUpper(beta ? modData.BetaVersion : modData.Version)
			                  && ((beta ? modData.IsForceUpdateBeta : modData.IsForceUpdate) || modData.CheckForceUpdate(version)); // Fixed pseudo logic mapping to match java source structure logic

			output.WriteUTF(needUpdate ? (beta ? modData.BetaVersion : modData.Version).ToNullableString() : null);

			Language[] languages = modData.AvailableLanguages;
			output.WriteByte((byte) languages.Length);
			foreach(Language language in languages) output.WriteByte((byte) language);

			output.WriteUTF(modData.Homepage);
			output.WriteUTF(modData.Discord);
			output.WriteInt(modData.Gid);
		}

		return File(output.ToArray(), "application/octet-stream");
	}

	[HttpGet("downloadMod/{name}/{version}")]
	public IActionResult DownloadMod(string name, string version) {
		try {
			_logger.LogInformation("[{Ip}] DownloadMod: {Name} {Version}", HttpContext.GetClientIp(), name, version);

			ModData? modData = ModData.GetModData(name);
			if(modData == null) return NotFound();


			Unsafe.SkipInit(out VersionStruct ver);
			if(version.Equals("latest", StringComparison.OrdinalIgnoreCase)) {
				ver = modData.Version;
				if(ver.IsNull()) ver = modData.BetaVersion;
			} else ver = new VersionStruct(version);

			string filePath = Path.Combine(Settings.Instance.ModFilePath, name, version);
			if(System.IO.File.Exists(filePath)) {
				byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
				return File(fileBytes, "application/zip", $"{name}.zip");
			}

			if(ver.IsNull()) return NotFound();

			string? dl = modData.DownloadLink.GetLink(ver);
			if(dl == null) return NotFound();

			return Redirect(dl);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error processing downloadMod");
			return StatusCode(500, "Internal Server Error");
		}
	}

	[HttpGet("ping")]
	public string Ping() {
		_logger.LogInformation("[{Ip}] ping!", HttpContext.GetClientIp());
		return "pong!";
	}

	[HttpGet("reloadToken")]
	public string ReloadToken() {
		_logger.LogInformation("[{Ip}] reloading Token..", HttpContext.GetClientIp());
		TokenData.LoadToken(Settings.Instance.TokenPath);
		return "Token reloaded!";
	}

	[HttpGet("autoInstaller/{version}")]
	public IActionResult AutoInstaller(string version) {
		_logger.LogInformation("[{Ip}] GetAutoInstaller: {Version}", HttpContext.GetClientIp(), version);
		VersionStruct ver = new(version);
		ModData? modData = ModData.GetModData("JALib");
		if(modData == null || modData.BetaVersion.IsNull()) return NotFound();

		if(ver.IsUpper(modData.BetaVersion)) {
			return DownloadMod("JALib", modData.BetaVersion.ToString());
		}
		return StatusCode(304);
	}
}