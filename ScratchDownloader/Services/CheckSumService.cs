using System;
using System.Threading.Tasks;
using ScratchDownloader.Models;

namespace ScratchDownloader.Services;

public class CheckSumService: ICheckSumService
{
    public async Task<bool> Check(string filePath, string expectedHash, string algorithm = "SHA256")
    {
        using var hashAlgorithm = System.Security.Cryptography.HashAlgorithm.Create(algorithm);
        if (hashAlgorithm == null)
            throw new System.Exception($"Hash algorithm {algorithm} is not supported.");

        using var stream = System.IO.File.OpenRead(filePath);
        var hashBytes = await hashAlgorithm.ComputeHashAsync(stream);
        var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        if (!string.Equals(actualHash, expectedHash, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return true;
    }
}