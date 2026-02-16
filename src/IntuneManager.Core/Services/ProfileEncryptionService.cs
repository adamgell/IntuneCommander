using Microsoft.AspNetCore.DataProtection;

namespace IntuneManager.Core.Services;

/// <summary>
/// Encrypts/decrypts profile data using ASP.NET DataProtection API.
/// Cross-platform; keys stored alongside the profile data.
/// </summary>
public class ProfileEncryptionService : IProfileEncryptionService
{
    private const string Purpose = "IntuneManager.Profiles.v1";
    private readonly IDataProtector _protector;

    public ProfileEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }
}
