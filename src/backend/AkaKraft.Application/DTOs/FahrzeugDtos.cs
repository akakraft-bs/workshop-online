using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record FahrzeugDto(
    Guid Id,
    string Marke,
    string? Modell,
    string Kennzeichen,
    bool IstStandard,
    FahrzeugArt Art
);

public record SaveFahrzeugRequest(
    string Marke,
    string? Modell,
    // Bei Fahrrad leer/null erlaubt.
    string? Kennzeichen,
    bool IstStandard,
    // Optional, damit ältere App-Versionen ohne das Feld weiter speichern können.
    FahrzeugArt? Art = null
);
