using AkaKraft.Application.Interfaces;
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

        return app;
    }
}
