using System.Text.Json.Serialization;

namespace Parlamento.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VotingOrientation
{
    InFavor,
    Against,
    Abstaining,
    NotInterested,
    Absent,
}
