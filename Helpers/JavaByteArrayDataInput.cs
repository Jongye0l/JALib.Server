using System.Text;

namespace JALib.Server.Helpers;

public class JavaByteArrayDataInput(Stream stream) : IDisposable {
	// ReSharper disable once InconsistentNaming
	public string? ReadUTF() {
		byte[]? buffer = ReadBytes();
		return buffer == null ? null : Encoding.UTF8.GetString(buffer);
	}

	public int ReadInt() {
		Span<byte> buffer = stackalloc byte[4];
		stream.ReadExactly(buffer);
		return (buffer[0] << 24) + (buffer[1] << 16) + (buffer[2] << 8) + buffer[3];
	}

	public long ReadLong() {
		Span<byte> buffer = stackalloc byte[8];
		stream.ReadExactly(buffer);
		return ((long) buffer[0] << 56) + ((long) buffer[1] << 48) + ((long) buffer[2] << 40) + ((long) buffer[3] << 32) +
		       ((long) buffer[4] << 24) + (buffer[5] << 16) + (buffer[6] << 8) + buffer[7];
	}

	public bool ReadBoolean() {
		int value = stream.ReadByte();
		if(value == -1) throw new EndOfStreamException();
		return value != 0;
	}

	public float ReadFloat() {
		Span<byte> buffer = stackalloc byte[4];
		stream.ReadExactly(buffer);
		buffer.Reverse();
		return BitConverter.ToSingle(buffer);
	}

	public double ReadDouble() {
		Span<byte> buffer = stackalloc byte[8];
		stream.ReadExactly(buffer);
		buffer.Reverse();
		return BitConverter.ToDouble(buffer);
	}

	public byte ReadByte() {
		int value = stream.ReadByte();
		if(value == -1) throw new EndOfStreamException();
		return (byte) value;
	}

	public short ReadShort() {
		Span<byte> buffer = stackalloc byte[2];
		stream.ReadExactly(buffer);
		return (short) ((buffer[0] << 8) + buffer[1]);
	}

	public byte[]? ReadBytes() {
		int size = ReadInt();
		if(size == -1) return null;
		byte[] buffer = new byte[size];
		stream.ReadExactly(buffer);
		return buffer;
	}


	public void Dispose() {
		stream.Dispose();
	}
}