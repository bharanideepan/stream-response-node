using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Security.Cryptography;
using System.Text;

namespace AS_Auth;

internal static class KeyUtil
{
    public static RsaSecurityKey GetPrivateKey(byte[] privateKeyData)
    {
        var rsaParams = DotNetUtilities.ToRSAParameters(ReadKey<RsaPrivateCrtKeyParameters>(privateKeyData));
        return GetRSASecurityKey(rsaParams);
    }

    public static RsaSecurityKey GetPublicKey(byte[] publicKeyData)
    {
        var rsaParams = DotNetUtilities.ToRSAParameters(ReadKey<RsaKeyParameters>(publicKeyData));
        return GetRSASecurityKey(rsaParams);
    }

    public static string Fingerprint(byte[] publicKeyData)
    {
        var keyParams = ReadKey<RsaKeyParameters>(publicKeyData);
        var der = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyParams).GetDerEncoded();
        using var sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(der);
        return Convert.ToBase64String(bytes);
    }

    private static T ReadKey<T>(byte[] keyData)
    {
        using var stream = new MemoryStream(keyData);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var pemReader = new PemReader(reader);
        return (T)pemReader.ReadObject();
    }

    private static RsaSecurityKey GetRSASecurityKey(RSAParameters parameters)
    {
        var rsa = RSA.Create();
        rsa.ImportParameters(parameters);
        return new RsaSecurityKey(rsa);
    }
}
