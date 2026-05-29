using System.Text.Json;

namespace TicTacToe.Shared.Protocol;

/// <summary>Конверт сообщения сетевого протокола.</summary>
public sealed record NetMessage(string Type, JsonElement Payload);
