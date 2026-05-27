namespace AkaKraft.Application.DTOs;

public record BadgesDto(
    int PendingUmfragen,
    int OpenMaengel,
    int LowStock,
    int UnseenFeedback,
    int OpenAufgaben);
