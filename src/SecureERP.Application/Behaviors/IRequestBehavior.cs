namespace SecureERP.Application.Behaviors;

public interface IRequestBehavior<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default);
}
