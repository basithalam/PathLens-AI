using PathLens.Application.Evidence;

namespace PathLens.Api.Endpoints;

public static class EvidenceEndpoints
{
    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports/{reportId:guid}/evidence", async (
            Guid reportId,
            HttpRequest httpRequest,
            EvidenceService service,
            CancellationToken cancellationToken) =>
        {
            if (!httpRequest.HasFormContentType)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["file"] = ["Send the photo as multipart form data."] },
                    title: "Validation failed", statusCode: 400);
            }

            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["file"] = ["A photo file is required."] },
                    title: "Validation failed", statusCode: 400);
            }

            byte[] content;
            await using (var uploadStream = file.OpenReadStream())
            using (var memory = new MemoryStream())
            {
                // Read at most one byte past the limit so oversized files are rejected safely.
                var buffer = new byte[81920];
                int read;
                while ((read = await uploadStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    if (memory.Length + read > ImageFileValidator.MaxFileSizeBytes + 1)
                    {
                        return Results.ValidationProblem(
                            new Dictionary<string, string[]> { ["file"] = ["File is too large."] },
                            title: "Validation failed", statusCode: 400);
                    }

                    memory.Write(buffer, 0, read);
                }

                content = memory.ToArray();
            }

            var errors = ImageFileValidator.Validate(content, file.ContentType, file.Length);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Validation failed", statusCode: 400);
            }

            // Optional fields that are present but unreadable are a client
            // error, never silently ignored.
            var fieldErrors = new Dictionary<string, string[]>();

            DateTimeOffset? captureTime = null;
            if (!string.IsNullOrWhiteSpace(form["captureTimeUtc"]))
            {
                if (DateTimeOffset.TryParse(form["captureTimeUtc"], out var parsedCapture))
                {
                    captureTime = parsedCapture;
                }
                else
                {
                    fieldErrors["captureTimeUtc"] = ["Capture time must be a valid date and time."];
                }
            }

            double? latitude = ParseOptionalDouble(form, "latitude", fieldErrors);
            double? longitude = ParseOptionalDouble(form, "longitude", fieldErrors);
            double? accuracy = ParseOptionalDouble(form, "locationAccuracyMeters", fieldErrors);

            if (latitude is not null && (latitude < -90 || latitude > 90))
            {
                fieldErrors["latitude"] = ["Latitude must be between -90 and 90."];
            }

            if (longitude is not null && (longitude < -180 || longitude > 180))
            {
                fieldErrors["longitude"] = ["Longitude must be between -180 and 180."];
            }

            if (fieldErrors.Count > 0)
            {
                return Results.ValidationProblem(fieldErrors, title: "Validation failed", statusCode: 400);
            }

            var created = await service.AddEvidenceAsync(
                reportId, content, file.FileName, file.ContentType,
                captureTime, latitude, longitude, accuracy, cancellationToken);

            return created is null
                ? Results.Problem(title: "Report not found", statusCode: 404)
                : Results.Created($"/api/evidence/{created.Id}", created);
        }).WithTags("Evidence");

        app.MapGet("/api/reports/{reportId:guid}/evidence", async (
            Guid reportId,
            EvidenceService service,
            CancellationToken cancellationToken) =>
        {
            var items = await service.ListForReportAsync(reportId, cancellationToken);
            return items is null
                ? Results.Problem(title: "Report not found", statusCode: 404)
                : Results.Ok(items);
        }).WithTags("Evidence");

        app.MapGet("/api/evidence/{id:guid}", async (
            Guid id,
            EvidenceService service,
            CancellationToken cancellationToken) =>
        {
            var item = await service.GetAsync(id, cancellationToken);
            return item is null
                ? Results.Problem(title: "Evidence not found", statusCode: 404)
                : Results.Ok(item);
        }).WithTags("Evidence");

        app.MapGet("/api/evidence/{id:guid}/file", async (
            Guid id,
            EvidenceService service,
            CancellationToken cancellationToken) =>
        {
            var file = await service.OpenFileAsync(id, cancellationToken);
            return file is null
                ? Results.Problem(title: "Evidence file not found", statusCode: 404)
                : Results.File(file.Value.Stream, file.Value.Item.ContentType, enableRangeProcessing: false);
        }).WithTags("Evidence");

        app.MapPost("/api/evidence/{id:guid}/analyze", async (
            Guid id,
            EvidenceService service,
            CancellationToken cancellationToken) =>
        {
            var job = await service.StartAnalysisAsync(id, cancellationToken);
            return job is null
                ? Results.Problem(title: "Evidence not found", statusCode: 404)
                : Results.Accepted($"/api/analysis-jobs/{job.Id}", job);
        }).WithTags("Analysis");

        app.MapGet("/api/analysis-jobs/{id:guid}", async (
            Guid id,
            EvidenceService service,
            CancellationToken cancellationToken) =>
        {
            var job = await service.GetJobAsync(id, cancellationToken);
            return job is null
                ? Results.Problem(title: "Analysis job not found", statusCode: 404)
                : Results.Ok(job);
        }).WithTags("Analysis");

        return app;
    }

    private static double? ParseOptionalDouble(
        IFormCollection form, string field, Dictionary<string, string[]> errors)
    {
        var raw = form[field].ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (double.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        errors[field] = [$"{field} must be a number."];
        return null;
    }
}
