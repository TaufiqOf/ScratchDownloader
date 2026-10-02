using System.Threading.Tasks;

namespace ScratchDownloader.Models;

public interface ICheckSumService
{
    Task<bool> Check(string filePath, string expectedHash, string algorithm = "SHA256");
}