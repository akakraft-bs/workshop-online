namespace AkaKraft.Application.DTOs;

public record SavePushSubscriptionDto(string Endpoint, string P256DH, string Auth);

public record NotificationPreferencesDto(
    bool WerkzeugRueckgabe,
    bool Veranstaltungen,
    bool VerbrauchsmaterialMindestbestand);

public record UpdateNotificationPreferencesDto(
    bool WerkzeugRueckgabe,
    bool Veranstaltungen,
    bool VerbrauchsmaterialMindestbestand);
