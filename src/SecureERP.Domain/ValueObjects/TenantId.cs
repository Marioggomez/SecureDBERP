using SecureERP.Domain.Exceptions;

namespace SecureERP.Domain.ValueObjects;

public readonly record struct TenantId
{
    private TenantId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static TenantId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("TENANT_ID_INVALID", "TenantId cannot be empty.");
        }

        return new TenantId(value);
    }

    public override string ToString() => Value.ToString();
}
