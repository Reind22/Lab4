using System.Buffers.Binary;
using System.Text.Json;

namespace TicTacToe.Shared.Protocol;

/// <summary>
/// Длинно-префиксированный фрейминг сообщений: [4 байта BE длина][JSON-полезка].
/// </summary>
public static class MessageFramer
{
    private const int MaxFrameSize = 1 * 1024 * 1024;

    public static async Task WriteAsync(Stream stream, NetMessage message, CancellationToken ct = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message, ProtocolJson.Options);
        var lengthHeader = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthHeader, json.Length);
        await stream.WriteAsync(lengthHeader, ct).ConfigureAwait(false);
        await stream.WriteAsync(json, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Читает следующее сообщение из потока. Возвращает <c>null</c>, если поток корректно закрыт.
    /// </summary>
    public static async Task<NetMessage?> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        var header = new byte[4];
        if (!await ReadExactAsync(stream, header, ct).ConfigureAwait(false))
            return null;

        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length <= 0 || length > MaxFrameSize)
            throw new InvalidDataException($"Некорректный размер кадра: {length}");

        var payload = new byte[length];
        if (!await ReadExactAsync(stream, payload, ct).ConfigureAwait(false))
            throw new EndOfStreamException("Поток закрыт посреди кадра.");

        var msg = JsonSerializer.Deserialize<NetMessage>(payload, ProtocolJson.Options)
            ?? throw new InvalidDataException("Не удалось десериализовать NetMessage.");
        return msg;
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read), ct).ConfigureAwait(false);
            if (n == 0)
            {
                if (read == 0) return false;
                throw new EndOfStreamException("Поток закрыт до окончания кадра.");
            }
            read += n;
        }
        return true;
    }
}
