using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record MotdDto(Guid Id, string Message, MotdSeverity Severity, DateTime UpdatedAt);

public record SetMotdDto(string Message, MotdSeverity Severity);
