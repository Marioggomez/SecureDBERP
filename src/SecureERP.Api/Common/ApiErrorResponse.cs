namespace SecureERP.Api.Common;

public sealed record ApiErrorResponse(
    string ErrorCode,
    string Message,
    string CorrelationId);
