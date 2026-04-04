namespace InventoryManagement.Services;

public interface IFileStorageService
{
    Task UploadAsync(string bucket, string objectKey, Stream stream, string contentType, long size);
    Task DeleteAsync(string bucket, string objectKey);
    Task<string> GeneratePresignedDownloadUrlAsync(string bucket, string objectKey, int expirySeconds = 300);
    string GetPublicUrl(string bucket, string objectKey);
    Task EnsureBucketsExistAsync();
}
