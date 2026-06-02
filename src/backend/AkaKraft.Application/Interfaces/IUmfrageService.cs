using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IUmfrageService
{
    Task<IEnumerable<UmfrageDto>> GetAllAsync(Guid currentUserId, bool isPrivileged);

    Task<UmfrageDto> CreateAsync(Guid userId, CreateUmfrageDto dto);

    /// <summary>Returns (updated DTO, forbidden flag). Null DTO = not found.</summary>
    Task<(UmfrageDto? Dto, bool Forbidden)> UpdateAsync(
        Guid umfrageId, Guid requestingUserId, bool isPrivileged, UpdateUmfrageDto dto);

    /// <summary>Returns (success, forbidden flag).</summary>
    Task<(bool Success, bool Forbidden)> DeleteAsync(
        Guid umfrageId, Guid requestingUserId, bool isPrivileged);

    /// <summary>Replaces the user's votes for this poll. Empty list = remove all votes.</summary>
    Task<(UmfrageDto? Dto, string? Error)> VoteAsync(
        Guid umfrageId, Guid userId, VoteUmfrageDto dto, bool isPrivileged);

    /// <summary>Returns (updated DTO, forbidden flag). Null DTO = not found.</summary>
    Task<(UmfrageDto? Dto, bool Forbidden)> CloseAsync(
        Guid umfrageId, Guid requestingUserId, bool isPrivileged);

    /// <summary>Toggles abstain for the current user. Removes any existing votes when abstaining.</summary>
    Task<(UmfrageDto? Dto, string? Error)> AbstainAsync(
        Guid umfrageId, Guid userId, bool isPrivileged);

    /// <summary>Sends a push reminder to non-voters. Max once per 24 hours. Creator or privileged only.</summary>
    Task<(bool Success, string? Error)> RemindAsync(
        Guid umfrageId, Guid requestingUserId, bool isPrivileged);
}
