using System.Security.Cryptography;
using SpaceStay.Core.Abstractions;

namespace SpaceStay.Infra.Security;

// Hash de senha com PBKDF2 (HMAC-SHA256) e salt aleatório por usuário, usando a
// criptografia nativa do .NET. Formato gravado: "pbkdf2$iterações$saltB64$hashB64".
// A verificação compara em tempo constante para não vazar informação pelo tempo de resposta.
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;        // 128 bits
    private const int KeySize = 32;         // 256 bits
    private const int Iterations = 100_000; // custo do PBKDF2
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string hash, string password)
    {
        try
        {
            var parts = hash.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2") return false;

            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var key = Convert.FromBase64String(parts[3]);

            var attempt = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, key.Length);
            return CryptographicOperations.FixedTimeEquals(attempt, key);
        }
        catch
        {
            return false; // hash mal-formado, falha de verificação
        }
    }
}
