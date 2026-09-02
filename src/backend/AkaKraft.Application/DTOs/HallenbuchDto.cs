using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record HallenbuchEintragDto(
    Guid Id,
    Guid UserId,
    string UserName,
    DateTime Start,
    DateTime End,
    string Description,
    bool HatGastgeschraubt,
    GastschraubenArt? GastschraubenArt,
    bool? GastschraubenBezahlt,
    bool HatFamiliegeschraubt,
    DateTime CreatedAt,
    Guid? FahrzeugId = null,
    string? FahrzeugLabel = null);

public record CreateHallenbuchEintragDto(
    DateTime Start,
    DateTime End,
    string Description,
    bool HatGastgeschraubt,
    GastschraubenArt? GastschraubenArt,
    bool? GastschraubenBezahlt,
    bool HatFamiliegeschraubt,
    Guid? FahrzeugId = null);

public record UpdateHallenbuchEintragDto(
    DateTime Start,
    DateTime End,
    string Description,
    bool HatGastgeschraubt,
    GastschraubenArt? GastschraubenArt,
    bool? GastschraubenBezahlt,
    bool HatFamiliegeschraubt,
    Guid? FahrzeugId = null);

public record HallenbuchStatistikEintragDto(
    Guid UserId,
    string UserName,
    double EigeneStunden,
    double GastStunden,
    double FamilieStunden);
