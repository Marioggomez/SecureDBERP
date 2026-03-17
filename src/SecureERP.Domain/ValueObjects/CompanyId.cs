using SecureERP.Domain.Exceptions;

namespace SecureERP.Domain.ValueObjects;

public readonly record struct CompanyId
{
    private CompanyId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static CompanyId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("COMPANY_ID_INVALID", "CompanyId cannot be empty.");
        }

        return new CompanyId(value);
    }

    public override string ToString() => Value.ToString();
}
