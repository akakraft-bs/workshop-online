namespace AkaKraft.Application.DTOs;

public record RegisterFcmTokenDto(string Token);

/// <param name="UserId">Null = an alle Nutzer mit Token senden.</param>
public record SendTestPushDto(Guid? UserId, string Title, string Body);
