using Microsoft.AspNetCore.Http;
using SecureERP.Api.Middleware;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Infrastructure.Persistence.SessionContext;

namespace SecureERP.Tests.Security;

public sealed class SecurityContextMiddlewarePublicRoutesTests
{
    [Fact]
    public async Task ValidateSessionPath_WithoutBearer_ShouldReturnUnauthorized()
    {
        bool nextCalled = false;
        SecurityContextMiddleware middleware = new(async _ => { nextCalled = true; await Task.CompletedTask; });
        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/auth/validate-session";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(
            context,
            new RequestContextAccessor(),
            new StubValidateSessionHandler(),
            new StubAuthorizationEvaluator());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task MfaChallengePath_WithoutBearer_ShouldRemainPublic()
    {
        bool nextCalled = false;
        SecurityContextMiddleware middleware = new(async _ => { nextCalled = true; await Task.CompletedTask; });
        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/auth/mfa/challenge";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(
            context,
            new RequestContextAccessor(),
            new StubValidateSessionHandler(),
            new StubAuthorizationEvaluator());

        Assert.True(nextCalled);
    }

    private sealed class StubValidateSessionHandler : IValidateSessionHandler
    {
        public Task<ValidateSessionResult> HandleAsync(ValidateSessionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(ValidateSessionResult.Failure("SESSION_INVALID", "invalid"));
    }

    private sealed class StubAuthorizationEvaluator : IAuthorizationEvaluator
    {
        public Task<AuthorizationCheckResult> EvaluateAsync(AuthorizationCheckRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AuthorizationCheckResult(false, "N/A", "N/A"));
    }
}
