using System.Text.Json;

namespace JALib.Server.Models;

public interface IJsonData {
	public void Load(ref Utf8JsonReader reader);
	public void Save(Utf8JsonWriter writer);
}