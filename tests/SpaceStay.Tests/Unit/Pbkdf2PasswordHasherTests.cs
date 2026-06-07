using SpaceStay.Infra.Security;
using Xunit;

namespace SpaceStay.Tests.Unit;

// Testes do hashing de senha (PBKDF2 + salt), base da Parte 5.
public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_nao_armazena_senha_em_texto_puro()
    {
        var hash = _hasher.Hash("Senha@123");
        Assert.NotEqual("Senha@123", hash);
        Assert.StartsWith("pbkdf2$", hash);
    }

    [Fact]
    public void Verify_aceita_senha_correta_e_rejeita_errada()
    {
        var hash = _hasher.Hash("Senha@123");
        Assert.True(_hasher.Verify(hash, "Senha@123"));
        Assert.False(_hasher.Verify(hash, "senha errada"));
    }

    [Fact]
    public void Hashes_da_mesma_senha_sao_diferentes_por_causa_do_salt()
    {
        var h1 = _hasher.Hash("Senha@123");
        var h2 = _hasher.Hash("Senha@123");
        Assert.NotEqual(h1, h2);                 // salt aleatório por hash
        Assert.True(_hasher.Verify(h1, "Senha@123"));
        Assert.True(_hasher.Verify(h2, "Senha@123"));
    }

    [Fact]
    public void Verify_retorna_false_para_hash_malformado()
    {
        Assert.False(_hasher.Verify("nao-e-um-hash", "qualquer"));
        Assert.False(_hasher.Verify("", "qualquer"));
    }
}
