using System.Security.Cryptography;
using System.Text;

namespace Parlamento.Infrastructure.Services;

internal static class PasswordHashingService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "pbkdf2";

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string? storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword))
        {
            return false;
        }

        if (storedPassword.StartsWith($"{Prefix}$", StringComparison.Ordinal))
        {
            return VerifyPbkdf2(password, storedPassword);
        }

        return SlowEquals(storedPassword, password) ||
            SlowEquals(storedPassword, Sha256Hex(password));
    }

    public static bool NeedsRehash(string? storedPassword)
    {
        return storedPassword == null ||
            !storedPassword.StartsWith($"{Prefix}${Iterations}$", StringComparison.Ordinal);
    }

    private static bool VerifyPbkdf2(string password, string storedPassword)
    {
        var parts = storedPassword.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool SlowEquals(string left, string right)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));
    }
}
