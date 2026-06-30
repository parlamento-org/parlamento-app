using System.Text.Json.Serialization;

namespace Parlamento.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposalResult
{
    RejectedInGenerality,
    RejectedInSpeciality,
    ApprovedInGenerality,
    ApprovedInSpeciality,
}
