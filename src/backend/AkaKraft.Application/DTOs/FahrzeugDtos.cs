namespace AkaKraft.Application.DTOs;

public record FahrzeugDto(
    Guid Id,
    string Marke,
    string? Modell,
    string Kennzeichen,
    bool IstStandard
);

public record SaveFahrzeugRequest(
    string Marke,
    string? Modell,
    string Kennzeichen,
    bool IstStandard
);
