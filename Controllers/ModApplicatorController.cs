using JALib.Server.Helpers;
using JALib.Server.Models;
using JALib.Server.Views.ModApplicator;
using Microsoft.AspNetCore.Mvc;

namespace JALib.Server.Controllers;

[ApiController]
[Route("")]
public class ModApplicatorController : Controller {
	private readonly ILogger<ModApplicatorController> _logger;

	public ModApplicatorController(ILogger<ModApplicatorController> logger) {
		_logger = logger;
	}

	[HttpGet("modApplicator/{name}/{version}")]
	public IActionResult ModApplicator(string name, string version, [FromHeader(Name = "Accept-Language")] string? langHeader, [FromHeader(Name = "User-Agent")] string? userAgent) {
		_logger.LogInformation("[{Ip}] ModApplicator: {Name} {Version}", HttpContext.GetClientIp(), name, version);
		bool isKorean = langHeader != null && (langHeader.Contains("ko") || langHeader.Contains("KR"));
		string lang = isKorean ? "ko" : "en";

		ModData? modData = ModData.GetModData(name);
		if(modData == null) return NotFound("Mod not found");

		ModApplicateInfo info = new() {
			Name = name,
			OldVersion = version,
			IsWindows = userAgent != null && userAgent.Contains("windows", StringComparison.CurrentCultureIgnoreCase),
		};

		if(version.Equals("latest", StringComparison.OrdinalIgnoreCase)) {
			info.Version = modData.Version.ToNullableString() ?? "0.0.0";
		} else if(version.Equals("betalatest", StringComparison.OrdinalIgnoreCase)) {
			info.Version = modData.BetaVersion.ToNullableString() ?? "0.0.0";
		} else {
			VersionStruct ver = new(version);
			info.Version = version;

			if(!ver.Equals(modData.Version) && !ver.Equals(modData.BetaVersion)) {
				bool isBeta = modData.BetaMap.ContainsKey(ver);
				bool forceUpdate = true;

				if(modData.IsForceUpdate) info.UpdateType = isKorean ? "모드" : "mod";
				else if(modData.IsForceUpdateBeta && isBeta) info.UpdateType = isKorean ? "베타" : "beta";
				else {
					forceUpdate = false;
					foreach(ForceUpdateHandle handle in modData.ForceUpdateHandles) {
						if(!handle.CheckVersion(ver)) continue;
						forceUpdate = true;
						info.UpdateType = isKorean ? "버전" : "version";
						break;
					}
				}

				if(forceUpdate) {
					info.Version = (isBeta ? modData.BetaVersion : modData.Version).ToNullableString() ?? "0.0.0";
					info.Redirected = true;
				}
			}
		}

		return isKorean ? View("ModApplicator-ko", new ModApplicatorKo(info)) : View("ModApplicator-en", new ModApplicatorEn(info));
	}
}