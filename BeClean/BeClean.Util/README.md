# BeClean.Util

Cryptographic utilities for .NET applications.

## Features

- **`CryptoHelper`** — AES-256 encryption and decryption with IV prepended to ciphertext (Base64 output), exposed as string extension methods `EncodeString` / `DecodeString`
- **File hashing** — SHA-256 hash computation for files via `ComputeFileHash`
- **Key management** — load the AES key from an environment variable (`SetEncryptionKeyEnvVar`) or set it directly (`SetEncryptionKey`)

## Requirements

- .NET 10+
