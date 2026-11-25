using System.Security.Cryptography;
using System.Text;

namespace Coaching.Infrastructure.Identity;

internal static class TokenHasher
{
    /// <summary>Token ngẫu nhiên base64url. Refresh token giữ 64 byte; token tài khoản dùng 32 byte.</summary>
    public static string NewToken(int byteCount = 64) => Base64UrlEncode(RandomNumberGenerator.GetBytes(byteCount));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
