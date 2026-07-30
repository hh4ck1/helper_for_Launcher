using System.Security.Cryptography;
using System.Text.Json;

string repositoryRoot = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
string manifestPath = Path.Combine(repositoryRoot, "channel", "stable", "manifest.json");
string signaturePath = Path.Combine(repositoryRoot, "channel", "stable", "manifest.sig");
string publicKeyPath = Path.Combine(repositoryRoot, "public-key.pem");

byte[] manifestBytes = await File.ReadAllBytesAsync(manifestPath);
byte[] signature = Convert.FromBase64String(
    (await File.ReadAllTextAsync(signaturePath)).Trim());
using RSA rsa = RSA.Create();
rsa.ImportFromPem(await File.ReadAllTextAsync(publicKeyPath));
if (!rsa.VerifyData(
        manifestBytes,
        signature,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pss))
{
    throw new CryptographicException("Manifest signature is invalid.");
}

using JsonDocument document = JsonDocument.Parse(manifestBytes);
JsonElement root = document.RootElement;
string version = root.GetProperty("packVersion").GetString()
    ?? throw new InvalidDataException("packVersion is missing.");
string versionDirectory = Path.GetFullPath(
    Path.Combine(repositoryRoot, "packs", version));
string versionPrefix = versionDirectory.TrimEnd(Path.DirectorySeparatorChar) +
    Path.DirectorySeparatorChar;
int fileCount = 0;

foreach (JsonElement file in root.GetProperty("files").EnumerateArray())
{
    string relativePath = file.GetProperty("path").GetString()
        ?? throw new InvalidDataException("File path is missing.");
    if (Path.IsPathRooted(relativePath) || relativePath.Contains('\\'))
    {
        throw new InvalidDataException($"Invalid path: {relativePath}");
    }

    string localPath = Path.GetFullPath(
        Path.Combine(
            versionDirectory,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
    if (!localPath.StartsWith(versionPrefix, StringComparison.Ordinal))
    {
        throw new InvalidDataException($"Path escapes pack directory: {relativePath}");
    }

    var info = new FileInfo(localPath);
    if (!info.Exists)
    {
        throw new FileNotFoundException($"Missing file: {relativePath}", localPath);
    }
    if (info.Length != file.GetProperty("size").GetInt64())
    {
        throw new InvalidDataException($"Size mismatch: {relativePath}");
    }

    await using FileStream stream = info.OpenRead();
    string actualHash = Convert.ToHexString(
        await SHA256.HashDataAsync(stream)).ToLowerInvariant();
    string expectedHash = file.GetProperty("sha256").GetString()
        ?? throw new InvalidDataException($"SHA-256 is missing: {relativePath}");
    if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidDataException($"SHA-256 mismatch: {relativePath}");
    }
    fileCount++;
}

Console.WriteLine($"Pack {version} is valid: {fileCount} files.");
