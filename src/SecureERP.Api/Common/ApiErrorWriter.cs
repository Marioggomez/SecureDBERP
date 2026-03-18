namespace SecureERP.Api.Common;

public static class ApiErrorWriter
{
    public const string ErrorCodeItemKey = "secureerp.error_code";

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string errorCode,
        string message,
        CancellationToken cancellationToken = default)
    {
        string correlationId = context.TraceIdentifier;
        context.Items[ErrorCodeItemKey] = errorCode;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers[ApiConstants.CorrelationIdHeader] = correlationId;

        await context.Response.WriteAsJsonAsync(
            new ApiErrorResponse(errorCode, message, correlationId),
            cancellationToken);
    }
}
