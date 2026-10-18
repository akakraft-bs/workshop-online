namespace AkaKraft.Application.DTOs;

public record HallenCheckDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? PictureUrl,
    string? Message,
    DateTime CheckedInAt,
    DateTime ExpiresAt
);

public record HallenCheckInRequest(string? Message);

/// <summary>Hallen-Position für den Standort-Check-in; <see cref="RadiusMeter"/> ist der Geofence-Radius.</summary>
public record HallenStandortDto(double Latitude, double Longitude, int RadiusMeter, DateTime UpdatedAt);

public record SetHallenStandortRequest(double Latitude, double Longitude);
