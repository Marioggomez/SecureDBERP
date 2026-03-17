namespace SecureERP.Application.Abstractions;

public interface IRequestValidator<in TRequest>
{
    ValidationResult Validate(TRequest request);
}
