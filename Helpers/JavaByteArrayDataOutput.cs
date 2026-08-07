using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace JALib.Server.Helpers;

public class JavaByteArrayDataOutput {
	private readonly MemoryStream _resultMemory = new();
	private readonly Stream _stream;

	public JavaByteArrayDataOutput(bool useCompress = false) {
		_stream = useCompress ? new GZipStream(_resultMemory, CompressionLevel.SmallestSize, true) : _resultMemory;
	}

	// ReSharper disable once InconsistentNaming
	public void WriteUTF(string? str) {
		WriteBytes(str == null ? null : Encoding.UTF8.GetBytes(str));
	}

	public void WriteInt(int value) {
		Span<byte> buffer = stackalloc byte[4];
		buffer[0] = (byte) (value >> 24);
		buffer[1] = (byte) (value >> 16);
		buffer[2] = (byte) (value >> 8);
		buffer[3] = (byte) value;
		_stream.Write(buffer);
	}

	public void WriteLong(long value) {
		Span<byte> buffer = stackalloc byte[8];
		buffer[0] = (byte) (value >> 56);
		buffer[1] = (byte) (value >> 48);
		buffer[2] = (byte) (value >> 40);
		buffer[3] = (byte) (value >> 32);
		buffer[4] = (byte) (value >> 24);
		buffer[5] = (byte) (value >> 16);
		buffer[6] = (byte) (value >> 8);
		buffer[7] = (byte) value;
		_stream.Write(buffer);
	}

	public void WriteBoolean(bool value) {
		_stream.WriteByte((byte) (value ? 1 : 0));
	}

	public void WriteFloat(float value) {
		Span<byte> buffer = stackalloc byte[4];
		Unsafe.As<byte, float>(ref buffer[0]) = value;
		buffer.Reverse();
		_stream.Write(buffer);
	}

	public void WriteDouble(double value) {
		Span<byte> buffer = stackalloc byte[8];
		Unsafe.As<byte, double>(ref buffer[0]) = value;
		buffer.Reverse();
		_stream.Write(buffer);
	}

	public void WriteByte(byte value) {
		_stream.WriteByte(value);
	}

	public void WriteShort(short value) {
		Span<byte> buffer = stackalloc byte[2];
		buffer[0] = (byte) (value >> 8);
		buffer[1] = (byte) value;
		_stream.Write(buffer);
	}

	public void WriteBytes(byte[]? buffer) {
		if(buffer is null) {
			WriteInt(-1);
			return;
		}
		WriteInt(buffer.Length);
		_stream.Write(buffer);
	}

	public void WriteBytesOnly(byte[] buffer) {
		_stream.Write(buffer);
	}

	public void WriteBytes(Span<byte> buffer) {
		WriteInt(buffer.Length);
		_stream.Write(buffer);
	}

	public byte[] ToArray() {
		if(_stream != _resultMemory) _stream.Dispose();
		return _resultMemory.ToArray();
	}
}