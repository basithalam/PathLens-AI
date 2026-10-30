using System.Security.Claims;
using PathLens.Application.Users;

namespace PathLens.Api.Endpoints;

public static class AuthEndpoints
{
    public const string ReviewerPolicy = "ReviewerOnly";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["credentials"] = ["Email and password are required."] },
                    title: "Validation failed", statusCode: 400);
            }

            var login = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
            return login is null
                ? Results.Problem(title: "Invalid email or password", statusCode: 401)
                : Results.Ok(login);
        }).RequireRateLimiting("login").WithTags("Auth");

        app.MapGet("/api/auth/me", (ClaimsPrincipal principal) =>
        {
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("sub");
            return Results.Ok(new
            {
                id,
                email = principal.FindFirstValue(ClaimTypes.Email),
                name = principal.FindFirstValue(ClaimTypes.Name),
                role = principal.FindFirstValue(ClaimTypes.Role),
            });
        }).RequireAuthorization().WithTags("Auth");

        return app;
    }

    public static Guid? CurrentUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
