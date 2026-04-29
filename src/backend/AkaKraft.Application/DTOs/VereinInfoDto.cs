namespace AkaKraft.Application.DTOs;

public record AmtsTraegerDto(
    string Role,
    string RoleLabel,
    string UserId,
    string UserName,
    string? Phone,
    string? Address
);

public record SchluesselhinterlegungDto(
    Guid Id,
    string Name,
    string Address,
    string? Phone,
    int SortOrder
);

public record VereinInfoDto(
    IEnumerable<AmtsTraegerDto> Amtstraeger,
    IEnumerable<SchluesselhinterlegungDto> Schluessel
);

public record VereinZugangDto(Guid Id, string Anbieter, string Zugangsdaten);
