using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

public static class UploadApi
{
    public static WebApplication AddUploadApi(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Upload Endpoints
        // -------------------------------------------------------------------------

        app.MapPost("/uploads/werkzeug", async (IFormFile file, IUploadService uploadService) =>
        {
            try
            {
                var model = new FileUploadModel(file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
                var url = await uploadService.SaveAsync(model);
                return Results.Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("VorstandOrAdmin")
          .DisableAntiforgery();
          
        app.MapPost("/uploads/verbrauchsmaterial", async (IFormFile file, IUploadService uploadService) =>
        {
            try
            {
                var model = new FileUploadModel(file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
                var url = await uploadService.SaveAsync(model);
                return Results.Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("VorstandOrAdmin")
          .DisableAntiforgery();

        app.MapPost("/uploads/mangel", async (IFormFile file, IUploadService uploadService) =>
        {
            try
            {
                var model = new FileUploadModel(file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
                var url = await uploadService.SaveAsync(model);
                return Results.Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("AnyRole")
          .DisableAntiforgery();

        return app;
    }
}