namespace AkaKraft.Application.DTOs;

public record UpdateAmtsTraegerKontaktDto(string? Phone, string? Address);

public record CreateSchluesselhinterlegungDto(string Name, string Address, string? Phone);

public record UpdateSchluesselhinterlegungDto(string Name, string Address, string? Phone);

public record CreateVereinZugangDto(string Anbieter, string Zugangsdaten);
public record UpdateVereinZugangDto(string Anbieter, string Zugangsdaten);
