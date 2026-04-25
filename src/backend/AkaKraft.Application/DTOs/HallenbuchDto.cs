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
    DateTime CreatedAt);

public record CreateHallenbuchEintragDto(
    DateTime Start,
    DateTime End,
    string Description,
    bool HatGastgeschraubt,
    GastschraubenArt? GastschraubenArt,
    bool? GastschraubenBezahlt,
    bool HatFamiliegeschraubt);

public record UpdateHallenbuchEintragDto(
    DateTime Start,
    DateTime End,
    string Description,
    bool HatGastgeschraubt,
    GastschraubenArt? GastschraubenArt,
    bool? GastschraubenBezahlt,
    bool HatFamiliegeschraubt);

public record HallenbuchStatistikEintragDto(
    Guid UserId,
    string UserName,
    double EigeneStunden,
    double GastStunden,
    double FamilieStunden);
