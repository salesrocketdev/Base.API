namespace Base.Core.Security;

public readonly record struct PasswordVerificationResult(bool Succeeded, bool NeedsRehash)
{
    public static PasswordVerificationResult Failed => new(false, false);

    public static PasswordVerificationResult Success => new(true, false);

    public static PasswordVerificationResult SuccessRehashRequired => new(true, true);
}
