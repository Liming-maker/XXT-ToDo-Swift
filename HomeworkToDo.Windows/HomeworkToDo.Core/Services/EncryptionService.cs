using System.Security.Cryptography;

namespace HomeworkToDo.Core.Services;

public static class EncryptionService
{
    private static readonly byte[] Key = "u2oh6Vu^HWe4_AES"u8.ToArray();
    private static readonly byte[] Iv = "u2oh6Vu^HWe4_AES"u8.ToArray();

    public static string? Encrypt(string plainText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var input = System.Text.Encoding.UTF8.GetBytes(plainText);
            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return null;
        }
    }
}
