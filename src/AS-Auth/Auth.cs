using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using LanguageExt;
using Microsoft.IdentityModel.Tokens;

namespace AS_Auth;

public static class Auth
{
    public const string AS_SOFTWARE_ISSUER = "auth.as-software.com";

    public static string Fingerprint(byte[] publicKeyData) =>
        KeyUtil.Fingerprint(publicKeyData);

    public static RsaSecurityKey GetPublicKey(byte[] publicKeyData) =>
        KeyUtil.GetPublicKey(publicKeyData);

    public static RsaSecurityKey GetPrivateKey(byte[] privateKeyData) =>
        KeyUtil.GetPrivateKey(privateKeyData);

    public static string IssueToken(RsaSecurityKey privateKey, string keyId, string clientId, HashSet<string> scopes, int expirationSeconds)
    {
        if (scopes == null || scopes.Count == 0)
        {
            throw new ArgumentException("Token scopes must be specified", nameof(scopes));
        }
        if (expirationSeconds <= 0 || expirationSeconds > 3600)
        {
            throw new ArgumentException("Token expiration is invalid", nameof(expirationSeconds));
        }

        var now = DateTimeOffsetProvider.UtcNow.ToUnixTimeSeconds();
        var scopesString = string.Join(" ", scopes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, GuidProvider.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iss, AS_SOFTWARE_ISSUER),
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim("scope", scopesString),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, (now + expirationSeconds).ToString(), ClaimValueTypes.Integer64)
        };

        return SignToken(privateKey, keyId, claims.ToList());
    }

    public static string ServiceAuthenticationAssertion(RsaSecurityKey privateKey, string keyId, string clientId, HashSet<string> scopes)
    {
        if (scopes == null || scopes.Count == 0)
        {
            throw new ArgumentException("Token scopes must be specified", nameof(scopes));
        }

        var now = DateTimeOffsetProvider.UtcNow.ToUnixTimeSeconds();
        var scopesString = string.Join(" ", scopes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, GuidProvider.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Aud, AS_SOFTWARE_ISSUER),
            new Claim(JwtRegisteredClaimNames.Iss, clientId),
            new Claim("scope", scopesString),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, (now + 60).ToString(), ClaimValueTypes.Integer64)
        };

        return SignToken(privateKey, keyId, claims.ToList());
    }

    private static string SignToken(RsaSecurityKey privateKey, string keyId, List<Claim> claims)
    {
        var signingCreds = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha512)
        {
            Key = { KeyId = keyId }
        };

        var header = new JwtHeader(signingCreds);
        var payload = new JwtPayload(claims);
        var token = new JwtSecurityToken(header, payload);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static JwtSecurityToken ReadToken(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    public static Either<Exception, JwtSecurityToken> ValidateTokenSignature(RsaSecurityKey publicKey, string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            RequireExpirationTime = false,
            RequireSignedTokens = true,
            RequireAudience = false,
            SaveSigninToken = false,
            TryAllIssuerSigningKeys = true,
            ValidateActor = false,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = false,
            ValidateTokenReplay = false,
            IssuerSigningKey = publicKey
        };

        Try<JwtSecurityToken> attempt = () =>
        {
            SecurityToken? jwt = null;
            new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out jwt);
            return jwt as JwtSecurityToken ?? throw new InvalidOperationException("Token validation failed");
        };
        return attempt.ToEither();
    }

    public static Either<Exception, JwtSecurityToken> ValidateToken(RsaSecurityKey publicKey, string token)
    {
        var now = DateTimeOffsetProvider.UtcNow;
        var jwt = ReadToken(token);

        var validSignature = ValidateTokenSignature(publicKey, token);
        if (validSignature.IsLeft)
        {
            return validSignature.MapLeft(ex => new Exception("Invalid token signature", ex));
        }

        if (jwt.Issuer != AS_SOFTWARE_ISSUER)
        {
            return new Exception("Invalid token issuer");
        }

        if (jwt.IssuedAt == null || jwt.IssuedAt == DateTime.MinValue)
        {
            return new Exception("Invalid token iat");
        }

        if (jwt.ValidTo == null || jwt.ValidTo == DateTime.MinValue)
        {
            return new Exception("Invalid token exp");
        }

        // Ensure issued at is in the past
        // Give a 1 minute buffer
        if (jwt.IssuedAt > now.AddMinutes(1))
        {
            return new Exception("Token issued at must be in the past");
        }

        // Ensure token is not expired
        // Give a 5 second buffer
        if (jwt.ValidTo < now.AddSeconds(-5))
        {
            return new Exception("Token is expired");
        }

        return jwt;
    }

    public static Either<Exception, JwtSecurityToken> ValidateServiceAssertion(RsaSecurityKey publicKey, HashSet<string> registeredScopes, string token)
    {
        var now = DateTimeOffsetProvider.UtcNow;
        var jwt = ReadToken(token);

        var validSignature = ValidateTokenSignature(publicKey, token);
        if (validSignature.IsLeft)
        {
            return validSignature.MapLeft(ex => new Exception("Service JWT assertion invalid signature", ex));
        }

        if (string.IsNullOrEmpty(jwt.Issuer))
        {
            return new Exception("Service JWT assertion requires iss");
        }

        if (jwt.Audiences == null || !jwt.Audiences.Any() || string.IsNullOrEmpty(jwt.Audiences.First()))
        {
            return new Exception("Service JWT assertion requires aud");
        }

        if (jwt.Audiences.First() != AS_SOFTWARE_ISSUER)
        {
            return new Exception("Service JWT assertion invalid aud");
        }

        if (jwt.IssuedAt == null || jwt.IssuedAt == DateTime.MinValue)
        {
            return new Exception("Service JWT assertion requires iat");
        }

        if (jwt.ValidTo == null || jwt.ValidTo == DateTime.MinValue)
        {
            return new Exception("Service JWT assertion requires exp");
        }

        // Ensure issued at is in the past
        // Give a 1 minute buffer
        if (jwt.IssuedAt > now.AddMinutes(1))
        {
            return new Exception("Service JWT assertion requires iat to be in the past");
        }

        // Ensure token is not expired
        // Give a 5 second buffer
        if (jwt.ValidTo < now.AddSeconds(-5))
        {
            return new Exception("Service JWT assertion is expired");
        }

        if ((jwt.ValidTo - jwt.IssuedAt).TotalMinutes > 5)
        {
            return new Exception("Service JWT assertions TTL must be 5 minutes or less");
        }

        var scopeClaim = jwt.Claims.FirstOrDefault(claim => claim.Type == "scope");
        if (scopeClaim == null || string.IsNullOrEmpty(scopeClaim.Value))
        {
            return new Exception("Service JWT assertion requires at least one scope");
        }

        var requestedScopes = new HashSet<string>(scopeClaim.Value.Split(' '));
        if (!requestedScopes.Any())
        {
            return new Exception("Service JWT assertion requires at least one scope");
        }
        if (!requestedScopes.IsSubsetOf(registeredScopes))
        {
            return new Exception("Service JWT assertion requested unregistered scopes");
        }

        return jwt;
    }
}
