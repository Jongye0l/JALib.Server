using System.IO.Compression;
using JALib.Server.Helpers;
using JALib.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace JALib.Server.Controllers;

[ApiController]
[Route("admin")]
public class AdminController(ILogger<AdminController> logger) : ControllerBase {
	private bool CheckPermission() {
		return Request.Headers.TryGetValue("Authorization", out StringValues token) && TokenData.CheckToken(token.ToString().Replace("Bearer ", "").Trim());
	}

	[HttpPost("modData")]
	public async Task<IActionResult> AddModData() {
		if(!CheckPermission()) return Unauthorized();

		using MemoryStream ms = new();
		await Request.Body.CopyToAsync(ms);
		ms.Position = 0;
		await using GZipStream gZipStream = new(ms, CompressionMode.Decompress);

		using JavaByteArrayDataInput input = new(gZipStream);
		string name = input.ReadUTF()!;
		logger.LogInformation("[{Ip}] Add ModData: {Name}", HttpContext.GetClientIp(), name);

		ModData.CreateMod(name, input);
		logger.LogInformation("[{Ip}] Complete Add ModData: {Name}", HttpContext.GetClientIp(), name);

		return Ok("Complete Add ModData");
	}

	[HttpPut("modData")]
	public async Task<IActionResult> ChangeModData() {
		if(!CheckPermission()) return Unauthorized();

		using MemoryStream ms = new();
		await Request.Body.CopyToAsync(ms);
		ms.Position = 0;
		await using GZipStream gZipStream = new(ms, CompressionMode.Decompress);

		using JavaByteArrayDataInput input = new(gZipStream);
		ModData? modData = ModData.GetModData(input.ReadUTF()!);
		if(modData == null) return NotFound();

		byte action = input.ReadByte();
		switch(action) {
			case 0:
				string? vStr = input.ReadUTF();
				modData.Version = string.IsNullOrEmpty(vStr) ? VersionStruct.Null : new VersionStruct(vStr);
				break;
			case 1:
				string? bStr = input.ReadUTF();
				modData.BetaVersion = string.IsNullOrEmpty(bStr) ? VersionStruct.Null : new VersionStruct(bStr);
				break;
			case 2:
				modData.IsForceUpdate = input.ReadBoolean();
				break;
			case 7:
				modData.Homepage = input.ReadUTF();
				break;
			case 9:
				modData.Gid = input.ReadInt();
				break;
			case 11:
				modData.Discord = input.ReadUTF();
				break;
			case 12:
				modData.IsForceUpdateBeta = input.ReadBoolean();
				break;
			case 13:
				VersionStruct verKey = new(input.ReadUTF()!);
				sbyte isBeta = (sbyte) input.ReadByte();
				if(isBeta == -1) modData.BetaMap.Remove(verKey);
				else modData.BetaMap[verKey] = isBeta == 1;
				break;
		}
		modData.Save();

		return Ok("Complete Change ModData");
	}

	[HttpGet("modData")]
	public IActionResult GetModData() {
		if(!CheckPermission()) return Unauthorized();

		logger.LogInformation("[{Ip}] Get ModData", HttpContext.GetClientIp());
		List<ModData> list = ModData.GetModDataList();
		JavaByteArrayDataOutput output = new(true);

		output.WriteInt(list.Count);
		foreach(ModData mod in list) ConnectOtherLib.ModToBytes(output, mod);

		return File(output.ToArray(), "application/octet-stream");
	}
}