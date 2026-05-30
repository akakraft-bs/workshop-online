using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

internal static class AdminEndpoints
{
    internal static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        app.MapPost("/admin/backfill-thumbnails", async (
            ApplicationDbContext db,
            IUploadService uploadService) =>
        {
            var werkzeuge = await db.Werkzeuge
                .Where(w => w.ImageUrl != null && w.ThumbnailUrl == null)
                .ToListAsync();

            var verbrauchsmaterialien = await db.Verbrauchsmaterialien
                .Where(v => v.ImageUrl != null && v.ThumbnailUrl == null)
                .ToListAsync();

            int processedWerkzeug = 0;
            int processedVerbrauchsmaterial = 0;
            int errors = 0;

            foreach (var w in werkzeuge)
            {
                var thumbUrl = await uploadService.GenerateThumbnailForExistingAsync(w.ImageUrl!);
                if (thumbUrl is not null)
                {
                    w.ThumbnailUrl = thumbUrl;
                    processedWerkzeug++;
                }
                else
                {
                    errors++;
                }
            }

            foreach (var v in verbrauchsmaterialien)
            {
                var thumbUrl = await uploadService.GenerateThumbnailForExistingAsync(v.ImageUrl!);
                if (thumbUrl is not null)
                {
                    v.ThumbnailUrl = thumbUrl;
                    processedVerbrauchsmaterial++;
                }
                else
                {
                    errors++;
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                processedWerkzeug,
                processedVerbrauchsmaterial,
                errors,
                total = processedWerkzeug + processedVerbrauchsmaterial,
            });
        }).RequireAuthorization("AdminOnly");

        // -------------------------------------------------------------------------
        // Ablageort (Storage Location) Management
        // -------------------------------------------------------------------------

        app.MapGet("/admin/ablageorte", async (ApplicationDbContext db) =>
        {
            var werkzeugCounts = await db.Werkzeuge
                .Where(w => w.StorageLocation != null && w.StorageLocation != "")
                .GroupBy(w => w.StorageLocation!)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            var verbrauchsCounts = await db.Verbrauchsmaterialien
                .Where(v => v.StorageLocation != null && v.StorageLocation != "")
                .GroupBy(v => v.StorageLocation!)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            var countMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var x in werkzeugCounts)
                countMap[x.Name] = x.Count;
            foreach (var x in verbrauchsCounts)
                countMap[x.Name] = countMap.GetValueOrDefault(x.Name) + x.Count;

            var ablageorte = await db.Ablageorte.OrderBy(a => a.Name).ToListAsync();
            var ablageortMap = ablageorte.ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);

            var allNames = countMap.Keys
                .Union(ablageorte.Select(a => a.Name), StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();

            var result = allNames.Select(name =>
            {
                ablageortMap.TryGetValue(name, out var a);
                return new AblageortOverviewDto(a?.Id, name, a?.Color, countMap.GetValueOrDefault(name));
            });

            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/ablageorte", async (CreateAblageortDto dto, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Name darf nicht leer sein.");

            var newName = dto.Name.Trim();
            var oldName = dto.OldName?.Trim();
            var isRename = oldName is not null
                && !string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase);

            var conflict = await db.Ablageorte
                .Where(a => a.Name.ToLower() == newName.ToLower())
                .Select(a => new { a.Id, a.Name, a.Color })
                .FirstOrDefaultAsync();

            if (conflict is not null)
            {
                if (isRename)
                    return Results.Conflict(new
                    {
                        conflictsWithId    = conflict.Id,
                        conflictsWithName  = conflict.Name,
                        conflictsWithColor = conflict.Color,
                    });
                return Results.Conflict("Ein Ablageort mit diesem Namen existiert bereits.");
            }

            if (isRename)
            {
                await db.Werkzeuge
                    .Where(w => w.StorageLocation == oldName)
                    .ExecuteUpdateAsync(s => s.SetProperty(w => w.StorageLocation, newName));
                await db.Verbrauchsmaterialien
                    .Where(v => v.StorageLocation == oldName)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.StorageLocation, newName));
            }

            var ablageort = new Ablageort
            {
                Id = Guid.NewGuid(),
                Name = newName,
                Color = dto.Color,
            };
            db.Ablageorte.Add(ablageort);
            await db.SaveChangesAsync();

            return Results.Created($"/admin/ablageorte/{ablageort.Id}",
                new AblageortDto(ablageort.Id, ablageort.Name, ablageort.Color));
        }).RequireAuthorization("AdminOnly");

        app.MapPut("/admin/ablageorte/{id:guid}", async (Guid id, UpdateAblageortDto dto, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Name darf nicht leer sein.");

            var ablageort = await db.Ablageorte.FindAsync(id);
            if (ablageort is null) return Results.NotFound();

            var oldName = ablageort.Name;
            var newName = dto.Name.Trim();

            if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                var conflict = await db.Ablageorte
                    .Where(a => a.Name.ToLower() == newName.ToLower() && a.Id != id)
                    .Select(a => new { a.Id, a.Name, a.Color })
                    .FirstOrDefaultAsync();

                if (conflict is not null)
                    return Results.Conflict(new
                    {
                        conflictsWithId    = conflict.Id,
                        conflictsWithName  = conflict.Name,
                        conflictsWithColor = conflict.Color,
                    });

                await db.Werkzeuge
                    .Where(w => w.StorageLocation == oldName)
                    .ExecuteUpdateAsync(s => s.SetProperty(w => w.StorageLocation, newName));
                await db.Verbrauchsmaterialien
                    .Where(v => v.StorageLocation == oldName)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.StorageLocation, newName));
            }

            ablageort.Name = newName;
            ablageort.Color = dto.Color;
            await db.SaveChangesAsync();

            return Results.Ok(new AblageortDto(ablageort.Id, ablageort.Name, ablageort.Color));
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/ablageorte/{id:guid}/merge-into/{targetId:guid}", async (
            Guid id, Guid targetId, ApplicationDbContext db) =>
        {
            var source = await db.Ablageorte.FindAsync(id);
            var target = await db.Ablageorte.FindAsync(targetId);

            if (source is null || target is null) return Results.NotFound();

            await db.Werkzeuge
                .Where(w => w.StorageLocation == source.Name)
                .ExecuteUpdateAsync(s => s.SetProperty(w => w.StorageLocation, target.Name));
            await db.Verbrauchsmaterialien
                .Where(v => v.StorageLocation == source.Name)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StorageLocation, target.Name));

            db.Ablageorte.Remove(source);
            await db.SaveChangesAsync();

            return Results.Ok(new AblageortDto(target.Id, target.Name, target.Color));
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/ablageorte/{targetId:guid}/merge-from-name", async (
            Guid targetId, MergeFromNameDto dto, ApplicationDbContext db) =>
        {
            var target = await db.Ablageorte.FindAsync(targetId);
            if (target is null) return Results.NotFound();

            await db.Werkzeuge
                .Where(w => w.StorageLocation == dto.SourceName)
                .ExecuteUpdateAsync(s => s.SetProperty(w => w.StorageLocation, target.Name));
            await db.Verbrauchsmaterialien
                .Where(v => v.StorageLocation == dto.SourceName)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StorageLocation, target.Name));

            return Results.Ok(new AblageortDto(target.Id, target.Name, target.Color));
        }).RequireAuthorization("AdminOnly");

        app.MapDelete("/admin/ablageorte/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var ablageort = await db.Ablageorte.FindAsync(id);
            if (ablageort is null) return Results.NotFound();

            db.Ablageorte.Remove(ablageort);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}
