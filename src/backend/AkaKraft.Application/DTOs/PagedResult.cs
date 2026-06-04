namespace AkaKraft.Application.DTOs;

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
