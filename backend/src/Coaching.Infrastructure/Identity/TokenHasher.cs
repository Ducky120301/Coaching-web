using System.Security.Cryptography;
using System.Text;

namespace Coaching.Infrastructure.Identity;

internal static class TokenHasher
{
    public static string NewToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
