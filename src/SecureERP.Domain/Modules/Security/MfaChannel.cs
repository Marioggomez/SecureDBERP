namespace SecureERP.Domain.Modules.Security;

public enum MfaChannel : short
{
    Totp = 1,
    EmailOtp = 2,
    RecoveryCode = 3,
    SmsOtpFallback = 4
}
