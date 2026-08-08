using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace JALib.Server.Models;

public struct VersionStruct : IEquatable<VersionStruct> {
	public static readonly VersionStruct Null = new(-1, -1, -1, -1);
	public int Major;
	public int Minor;
	public int Build;
	public int Revision;

	public int DefaultFormatFieldCount {
		get {
			if(Major == -1) return 0;
			if(Build == -1) return 2;
			return Revision != -1 ? 4 : 3;
		}
	}

	public VersionStruct(int major, int minor, int build, int revision) {
		Major = major;
		Minor = minor;
		Build = build;
		Revision = revision;
	}

	public VersionStruct(string value) {
		Setup(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Setup(string value) => Setup(value.AsSpan());

	public void Setup(ReadOnlySpan<char> value) {
		int currentValue = -1;
		int changeIndex = 0;
		for(int i = 0; i < value.Length; i++) {
			char c = value[i];
			if(c is >= '0' and <= '9') {
				currentValue = currentValue == -1 ? c - '0' : currentValue * 10 + (c - '0');
			} else if(c == '.') {
				if(currentValue == -1) throw new ArgumentException("Version int value is not set.", nameof(value));
				Set(changeIndex++, currentValue);
				currentValue = -1;
			}
		}
		Set(changeIndex, currentValue);
		while(changeIndex < 4) Set(changeIndex++, -1);
	}

	public void Setup(ReadOnlySpan<byte> value) {
		int currentValue = -1;
		int changeIndex = 0;
		for(int i = 0; i < value.Length; i++) {
			byte c = value[i];
			if(c is >= (byte) '0' and <= (byte) '9') {
				currentValue = currentValue == -1 ? c - '0' : currentValue * 10 + (c - '0');
			} else if(c == '.') {
				if(currentValue == -1) throw new ArgumentException("Version int value is not set.", nameof(value));
				Set(changeIndex++, currentValue);
				currentValue = -1;
			}
		}
		Set(changeIndex, currentValue);
		while(changeIndex < 4) Set(changeIndex++, -1);
	}

	public void Setup(ReadOnlySequence<byte> value) {
		int currentValue = -1;
		int changeIndex = 0;
		foreach(ReadOnlyMemory<byte> readOnlyMemory in value) {
			ReadOnlySpan<byte> span = readOnlyMemory.Span;
			for(int i = 0; i < span.Length; i++) {
				byte c = span[i];
				if(c is >= (byte) '0' and <= (byte) '9') {
					currentValue = currentValue == -1 ? c - '0' : currentValue * 10 + (c - '0');
				} else if(c == '.') {
					if(currentValue == -1) throw new ArgumentException("Version int value is not set.", nameof(value));
					Set(changeIndex++, currentValue);
					currentValue = -1;
				}
			}
		}
		Set(changeIndex, currentValue);
		while(changeIndex < 4) Set(changeIndex++, -1);
	}

	public void Setup(ref Utf8JsonReader reader) {
		if(reader.TokenType == JsonTokenType.Null) SetNull();
		else if(reader.HasValueSequence) Setup(reader.ValueSequence);
		else Setup(reader.ValueSpan);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetNull() {
		Major = Minor = Build = Revision = -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsNull() => Major == -1;

	private void Set(int index, int value) {
		if(index > 3) throw new ArgumentException("Version index is out of range", nameof(value));
		Unsafe.Add(ref Major, index) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(VersionStruct a, VersionStruct b) => a.Equals(b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(VersionStruct a, VersionStruct b) => !a.Equals(b);

	public static bool operator <(VersionStruct a, VersionStruct b) {
		for(int i = 0; i < 4; i++) {
			int aValue = Unsafe.Add(ref a.Major, i);
			int bValue = Unsafe.Add(ref b.Major, i);
			if(aValue != bValue) return aValue < bValue;
		}
		return false;
	}

	public static bool operator >(VersionStruct a, VersionStruct b) {
		for(int i = 0; i < 4; i++) {
			int aValue = Unsafe.Add(ref a.Major, i);
			int bValue = Unsafe.Add(ref b.Major, i);
			if(aValue != bValue) return aValue > bValue;
		}
		return false;
	}

	public static bool operator <=(VersionStruct a, VersionStruct b) {
		for(int i = 0; i < 4; i++) {
			int aValue = Unsafe.Add(ref a.Major, i);
			int bValue = Unsafe.Add(ref b.Major, i);
			if(aValue != bValue) return aValue < bValue;
		}
		return true;
	}

	public static bool operator >=(VersionStruct a, VersionStruct b) {
		for(int i = 0; i < 4; i++) {
			int aValue = Unsafe.Add(ref a.Major, i);
			int bValue = Unsafe.Add(ref b.Major, i);
			if(aValue != bValue) return aValue > bValue;
		}
		return true;
	}

	public string? ToNullableString() {
		return IsNull() ? null : ToString();
	}

	public override string ToString() {
		int fieldCount = DefaultFormatFieldCount;
		if(fieldCount == 0) return string.Empty;
		Span<char> v = stackalloc char[-1 + fieldCount * 11];
		int totalWritten = 0;
		for(int i = 0; i < fieldCount; i++) {
			if(i != 0) v[totalWritten++] = '.';
			if(Unsafe.Add(ref Major, i).TryFormat(v[totalWritten..], out int written)) totalWritten += written;
			else throw new InvalidOperationException("Version int value is too large.");
		}
		return new string(v[..totalWritten]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteToString(Span<byte> data, out int written) => WriteToString(data, DefaultFormatFieldCount, out written);

	public void WriteToString(Span<byte> data, int fieldCount, out int written) {
		written = 0;
		for(int i = 0; i < fieldCount; i++) {
			if(i != 0) data[written++] = (byte) '.';
			if(Unsafe.Add(ref Major, i).TryFormat(data[written..], out int bytesWritten)) written += bytesWritten;
			else throw new InvalidOperationException("Version int value is too large.");
		}
	}

	public bool IsUpper(VersionStruct version) {
		if(Major < version.Major) return true;
		if(Major > version.Major) return false;
		if(Minor < version.Minor) return true;
		if(Minor > version.Minor) return false;
		if(Build < version.Build) return true;
		if(Build > version.Build) return false;
		return Revision < version.Revision;
	}

	public bool Equals(VersionStruct other) => Major == other.Major && Major == -1 || Minor == other.Minor && Build == other.Build && Revision == other.Revision;

	public override bool Equals(object? obj) => obj == null && Major == -1 || obj is VersionStruct other && Equals(other);

	public override int GetHashCode() => Major == -1 ? 0 : HashCode.Combine(Major, Minor, Build, Revision);
}