using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Application.Modules.Workflow.Abstractions;
using SecureERP.Application.Modules.Workflow.DTOs;
using SecureERP.Domain.Modules.Security;
using SecureERP.Domain.Modules.Workflow;
using System.Security.Cryptography;
using System.Text;

namespace SecureERP.Application.Modules.Workflow.Commands;

public sealed class CreateApprovalInstanceHandler : ICreateApprovalInstanceHandler
{
    private readonly IWorkflowPilotRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly IOperationalSecurityService _operationalSecurityService;
    private readonly IAuthRepository _authRepository;

    public CreateApprovalInstanceHandler(
        IWorkflowPilotRepository repository,
        IRequestContextAccessor requestContextAccessor,
        IOperationalSecurityService operationalSecurityService,
        IAuthRepository authRepository)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
        _operationalSecurityService = operationalSecurityService;
        _authRepository = authRepository;
    }

    public async Task<CreateApprovalInstanceResponse> HandleAsync(
        CreateApprovalInstanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EntityCode))
        {
            return CreateApprovalInstanceResponse.Failure("WORKFLOW_ENTITY_CODE_REQUIRED", "Entity code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
        {
            return CreateApprovalInstanceResponse.Failure("WORKFLOW_PAYLOAD_REQUIRED", "Payload is required.");
        }

        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null)
        {
            return CreateApprovalInstanceResponse.Failure("SESSION_CONTEXT_REQUIRED", "User context is required.");
        }

        OperationalSecurityDecision guard = await _operationalSecurityService.GuardAsync(
            "WORKFLOW.APPROVAL_INSTANCE.CREATE",
            request.IpAddress,
            $"USER:{context.UserId.Value}",
            context.TenantId,
            context.CompanyId,
            cancellationToken);
        if (!guard.IsAllowed)
        {
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    "AUTH_ABUSE_DETECTED",
                    "WARNING",
                    "DENIED",
                    $"Workflow approval create blocked by operational policy ({guard.Code}).",
                    context.TenantId,
                    context.CompanyId,
                    context.UserId,
                    context.SessionId,
                    null,
                    ParseCorrelationId(context.CorrelationId),
                    request.IpAddress,
                    null),
                cancellationToken);
            return CreateApprovalInstanceResponse.Failure("AUTH_REQUEST_REJECTED", "Operation rejected.");
        }

        byte[] payloadHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.Payload.Trim()));
        long id = await _repository.CreateApprovalInstanceAsync(
            new ApprovalInstanceToCreate(
                request.OrganizationUnitId,
                request.ApprovalProfileId,
                request.EntityCode.Trim(),
                request.ObjectId,
                request.CurrentLevel,
                request.ApprovalStateId,
                context.UserId.Value,
                DateTime.UtcNow,
                request.UtcExpiresAt,
                request.Reason,
                payloadHash,
                true),
            cancellationToken);

        return CreateApprovalInstanceResponse.Success(id);
    }

    private static Guid? ParseCorrelationId(string? correlationId)
        => Guid.TryParse(correlationId, out Guid value) ? value : null;
}
