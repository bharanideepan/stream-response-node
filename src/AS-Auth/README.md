# AS-Auth

Authentication and authorization for AS services.

## Configuration

| Environment Variable | Description | Example |
| --- | --- | --- |
| AS_AUTH_SIGN_KEY_PATH | Private token signing key path | |
| AS_AUTH_VERIFY_KEY_PATH | Public token verifying key path | |

## Token Format
Tokens are JSON Web Token (or "JWT" -- pronounced "jot") format
[JWT RFC](https://tools.ietf.org/html/rfc7519), 
[Good Introduction](https://jwt.io/introduction)

## High Level Overview
Authentication happens by verifying a signed JWT token. The auth service provides a JWT token that can be used for authorization to the rest of the system.
The service wanting to be authorized must make a request to the auth service providing an _assertion_, that the auth service will validate. If valid, the requesting service will be issued a JWT token that it can pass with other requests in order to be authorized for different actions ("scopes"). Because this is done with asymmetric key cryptography, anyone wanting to verify a JWT token can do so with the public verification key.

## Scopes
Scopes are used to identify permissions. An endpoint may require that a user is authorized for a scope or set of scopes. If the JWT token does not contain those scopes, the request is unauthorized.

### Registering Scopes
Scopes must be registered in order to be requested in an assertion.
Currently, to register a scope, you must create an entry in the `scope` table in the database. Give it a name, which is prefixed with 1-n namespaces separated by colons, and suffixed by the permission, e.g. 'documents:view'. Also give it a description so, when auditing scopes, we know what it is used for, e.g. "Authorizes a user to view documents.".

## Service Clients
Service clients are used to identify a service, rather than a person. These are for programmatic communication.

### Registering a Service Client
Service clients must be registered in order to get a JWT token.
Currently, to register a service client, you must create an entry in the `service_client` table in the database. Give it a name (human-readable service name), client_id (service identifier, e.g. `document_service`), and public key data.
For help on generating service client cryptography keys, see [Generating Keys](#Generating-Keys) below
To grant a service client scope permissions, you must create entries in the `service_client_scope` join table.

## Service Tokens
The service wanting authorize will first authenticate with a JWT token containing the desired scopes (a subset of the registered scopes available to the service client) signed with it's private key (paired with the registered public key for the service client).
It will post that to the authentication endpoint and receive back a JWT token to pass in subsequent requests.

### Request
```
{
  "grant_type": "urn:ietf:params:oauth:grant-type:jwt-bearer"
  "assertion": ""
}
```
The `grant_type` must be `urn:ietf:params:oauth:grant-type:jwt-bearer`.
The `assertion` is a JWT token that the service signs with it's registered key-pair. The JWT-valid signing algorithm to be used in RS512.

The assertion contains the following claims:
- `kid`: The public key ID whose private counter-part signed the claims
- `jti`: UUID - JWT ID
- `iss`: The issuer is service client `client_id` which has been registered with the auth service
- `aud`: The audience must be 'auth.as-software.com'
- `scope`: A space-separated string of scope identifiers (per [RFC-6749 Section 3.3](https://www.rfc-editor.org/rfc/rfc6749#section-3.3))
- `iat`: Issued at time (epoch seconds)
- `exp`: Expiration time (epoch seconds). Should be less than or equal to 1 minute from iat.

For example, the assertion JWT (pre-signing), may contain something like:
Header:
```
{
  "alg": "RS512",
  "typ": "JWT",
  "kid": "MTIzNDU2Nzg5MA=="
}
```
Payload:
```
{
  "jti": "70850b66-7c43-4387-afcf-cc78ac21312b",
  "iss": "documents_service",
  "aud": "auth.as-software.com",
  "scope": "documents:create documents:view documents:sign"
  "iat": 1673038144,
  "exp": 1673038179
}
```

### Response
The auth service will respond with a JSON body containing
- `access_token`: The JWT token
- `token_type`: Currently, will always be `Bearer`
- `expires_in`: Number of seconds in which the token expires

The JWT specified in `access_token` will contain the following claims:
- `kid`: The public key ID whose private counter-part signed the claims
- `jti`: UUID - JWT ID
- `sub`: The subject, as specified in the `client_id` of the service client
- `iss`: auth.as-software.com
- `scopes`: Array of scopes
- `iat`: Issued at time (epoch seconds)
- `exp`: Expiration time (epoch seconds)

#### Example
Header:
```
{
  "alg": "RS512",
  "typ": "JWT",
  "kid": "MTIzNDU2Nzg5MA=="
}
```
Payload:
```
{
  "jti": "3dd01178-b26b-4cf9-b91c-f637ca667725",
  "sub": "documents_service",
  "iss": "auth.as-software.com",
  "scopes": ["documents:create", "documents:view", "documents:sign"]
  "iat": 1673038785,
  "exp": 1673039085
}
```

## Using This Library

### General
Read your public/private keys into UTF8 byte array.

Fingerprint the public key with `Fingerprint`, passing the public key byte array.

Obtain `RsaSecurityKey` objects via `GetPublicKey` and `GetPrivateKey`.

I recommend statically getting the fingerprint and keys on startup and storing them to reduce time to retrieve those.

### Service Client

#### Obtaining a Token
1. To retrieve a service assertion, call `ServiceAuthenticationAssertion`, passing your service's private key, public key fingerprint, your registered client ID, and the desired list of scopes.
1. Pass this service assertion as outlined in the Request section above to retrieve a JWT.

## Generating Keys

### Setup

#### Windows

##### Via Git for Windows
- Run `openssl` in Git Bash term
- Use `<Git install path>\usr\bin\openssl.exe`

##### WSL
- Use package manager to install latest OpenSSL

### Preference Rankings

#### Algorithm
1. RSA
    - Much broader support currently
2. Ed25519 if supported
3. ECDSA if someone / a client has a really good reason / need

Never DSA.

#### Bit Length
1. 4096
1. 3072
1. 2048
    - NIST guideline states 2048 is OK through 2030, but I reckon it's better to be ahead of the curve. See [NIST Special Publication Recommendation for
Key Management §5.6.3](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-57pt1r5.pdf)

#### Key Encoding
1. ASN.1 ([X.680-X.693](https://www.itu.int/rec/T-REC-X.680/)) PEM ([RFC 7468](https://datatracker.ietf.org/doc/html/rfc7468))

Not OpenSSH.

#### Information Syntax Specification
1. PKCS#8 ([RFC 5208](https://datatracker.ietf.org/doc/html/rfc5208))
2. PKCS#1 (RSA Only) ([RFC 8017](https://datatracker.ietf.org/doc/html/rfc8017))

- PKCS#12 ([RFC 7292](https://datatracker.ietf.org/doc/html/rfc7292)) if bundling with a certificate

#### Fingerprint representation
1. SHA256 Base64
2. SHA256 Hex with colons
3. SHA256 Hex without colons
4. MD5 Hex with colons
5. MD5 Hex without colons

Special mention:
Visual art is always rad af.

### OpenSSL

#### RSA PKCS#8 PEM (Preferred)
Private: `openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:4096 -out private.pem`

Public: `openssl pkey -in private.pem -pubout -out public.pem`

#### RSA PKCS#1 PEM
Private: `openssl genrsa -out private-pkcs1.pem -traditional 4096`

Public: `openssl rsa -in private-pkcs1.pem -RSAPublicKey_out -out public-pkcs1.pem`

#### Ed25519 PKCS#8 PEM
Private: `openssl genpkey -algorithm Ed25519 -out private.pem`

Public: `openssl pkey -in private.pem -pubout -out public.pem`

### ssh-keygen

#### RSA OpenSSH
`ssh-keygen -t rsa -b 4096`

#### Ed25519 OpenSSH
`ssh-keygen -t ed25519`

## Fingerprinting

You do not want a private key fingerprint and we should never be giving third parties any form of "fingerprint" of our private keys. Some services will append the function used to calculate the fingerprint, e.g. `SHA256:<fingerprint>` for a SHA256 fingerprint.

### OpenSSL

#### RSA / Ed25519 SHA256 Base64 (Preferred)
Public: `openssl pkey -pubin -in public.pem -outform DER | openssl sha256 -binary | openssl base64 -A`

From Private: `openssl pkey -in private.pem -pubout -outform DER | openssl sha256 -binary | openssl base64 -A`

#### RSA / Ed25519 SHA256 with colons
Public: `openssl pkey -pubin -in public.pem -outform DER | openssl sha256 -c`

#### RSA OpenSSH SHA256 Base64
Note that this output will contain an extra padding character '=' where ssh-keygen strips that off in it's -lf output

Public: `awk '{print $2}' public.pub | openssl base64 -d -A | openssl sha256 -binary | openssl base64 -A`

#### RSA OpenSSH SHA256 with colons
Public: `awk '{print $2}' public.pub | openssl base64 -d -A | openssl sha256 -c`

### ssh-keygen

#### RSA OpenSSH SHA256 Base64
Note that this output will have it's last padding character '=' stripped where OpenSSL/other services will not strip it (e.g. AWS will keep it)

Public: `ssh-keygen -lf public.pub -E sha256`

## Signing and Verification

### OpenSSL

#### SHA256 Signature
`openssl dgst -sign private.pem -keyform PEM -sha256 -out file.sign -binary {file}`

Base64 representation: `openssl base64 -in file.sign`

If you need to calculate the digest and signature separately, you can do so via:
1. Calculate digest: `openssl dgst -binary -sha256 file > hash`
2. Sign digest: `openssl pkeyutl -sign -in hash -inkey private.pem -pkeyopt digest:sha256 -keyform PEM -out file.sign`

#### SHA256 Verification
First, if Base64 encoded:
`openssl base64 -d -in <signature> -out file.sign`

Then
`openssl dgst -sha256 -verify public.pem -signature file.sign {file}`

You should look for: `Verified OK`

## Describing to non-technical folks
[Auth0 blog post featuring classic lock box and key metaphor](https://auth0.com/blog/how-to-explain-public-key-cryptography-digital-signatures-to-anyone/)

## Interim SQL
Use these commands until there is a better way.

### Create AuthScope
```
USE [AS];

INSERT INTO AuthScope (Name, Description)
VALUES ('documents:view', 'Allows user to view documents');
```

### Create AuthServiceClient
```
USE [AS];

INSERT INTO AuthServiceClient (Name, ClientID, PublicKey)
Values ('Documents Service', 'documents_service', '-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAztRK75cizNkMu0tdbFI4
2PSCn6WQ4mxU/ucDH43oXaFDsGtytq19J7GEPcrhisgSGif6Fw5U/tkDXd0lfRZk
Rsp6bQa/mUKvRVk19fmSvDJ6X1a3/6IMQKBp2C9D05hgrap/t8v8HFQn4/nxOhlU
DYUt53Jymwg+98YgrfZYlPe0l3eesOvN72hmdz0d2QYkLL9VkcorzohhmeGWtF1a
rrEje4HYv8m301qmiezT9QxIN/Sk6jR828Q+KIGwSf4+AwP6Rg82FzaWTMbaxz2b
4+DZof1Ui4KYulGAHJt33o+QfdaUaFAyaFPmJITBLOgQObEyufnE8jnQmS9K7iOx
4klXfSDo4AAapHptzdbmCMwcSPyyrDT3zt54QzBYfdwyvo8g1pDMnFxw2bPjF8CK
bUOcCbiPkMfZmGAd/s6kVsev9B306h2kIW4ykuYwqMEEE55TxxgRG+93aTLFMKS0
u6zcClNDJFCOuhXlcFYMH6c4QaVMDr5bAzs1Wn8uyHBgiFP8SwtKqRSCpxns0Ecy
x7PmP8mtADgIgAFb9epKNkFo6q3KG9LhpswEqU+IDE3mybhwEAiz4sHjV/WeGHDB
ttT4ZegA3Bk1vg+snSawmvfgs1EAE3FsfG9DglOcQowT52YpJU9bdVC8S/sKkpJB
Xdz+EV/Br35yQXAWJWBjCEcCAwEAAQ==
-----END PUBLIC KEY-----');
```

### Assign scopes to service client
```
USE [AS];

INSERT INTO AuthServiceClientScope (AuthServiceClientID, AuthScopeID)
Values ('EB2A7941-7ABE-45AA-9DBA-4464D1087DA9', 'F9FC30ED-0D3B-406E-B58A-50493B0357A0'), ('EB2A7941-7ABE-45AA-9DBA-4464D1087DA9', 'AB2ECAEF-C829-4936-88F1-CA559BCE123D');
```

