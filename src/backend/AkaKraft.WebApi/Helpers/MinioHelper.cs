namespace AkaKraft.WebApi.Helpers;

public static class MinioHelper
{
    internal static async Task EnsureMinioReadyAsync(WebApplication app)
    {
        var logger = app.Logger;

        using var scope = app.Services.CreateScope();
        var minio = scope.ServiceProvider.GetRequiredService<Minio.IMinioClient>();
        var opts  = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<AkaKraft.Infrastructure.Options.MinioOptions>>()
            .Value;

        var bucket = opts.BucketName;

        try
        {
            var exists = await minio.BucketExistsAsync(
                new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucket));

            if (!exists)
            {
                await minio.MakeBucketAsync(
                    new Minio.DataModel.Args.MakeBucketArgs().WithBucket(bucket));
                logger.LogInformation("MinIO-Bucket '{Bucket}' wurde erstellt.", bucket);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "MinIO-Bucket '{Bucket}' konnte nicht geprüft/erstellt werden. " +
                "Ist der MinIO-Server erreichbar und sind die Zugangsdaten korrekt?", bucket);
            return;
        }

        // Öffentliche Leseberechtigung setzen – schlägt auf manchen MinIO-Versionen
        // fehl, wenn SetBucketPolicy durch Server-Policy gesperrt ist.
        // Fallback: Bucket manuell in der MinIO-Console (http://localhost:9001) auf
        // "Anonymous access: readonly" setzen.
        try
        {
            var policy = $$"""
                {
                  "Version": "2012-10-17",
                  "Statement": [{
                    "Effect": "Allow",
                    "Principal": {"AWS": ["*"]},
                    "Action": ["s3:GetObject"],
                    "Resource": ["arn:aws:s3:::{{bucket}}/*"]
                  }]
                }
                """;

            await minio.SetPolicyAsync(
                new Minio.DataModel.Args.SetPolicyArgs()
                    .WithBucket(bucket)
                    .WithPolicy(policy));

            logger.LogInformation("MinIO-Bucket '{Bucket}': öffentlicher Lesezugriff gesetzt.", bucket);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "MinIO-Bucket '{Bucket}': Bucket-Policy konnte nicht gesetzt werden " +
                "(häufig bei neueren MinIO-Versionen). " +
                "Bitte Bucket manuell über die MinIO-Console (http://localhost:9001) " +
                "auf 'Anonymous access: readonly' stellen.", bucket);
        }
    }

}