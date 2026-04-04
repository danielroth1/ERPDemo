using InventoryManagement.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace InventoryManagement.Services;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minio;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(IMinioClient minio, MinioSettings settings, ILogger<MinioFileStorageService> logger)
    {
        _minio = minio;
        _settings = settings;
        _logger = logger;
    }

    public async Task EnsureBucketsExistAsync()
    {
        foreach (var bucket in new[] { _settings.ImagesBucket, _settings.DocumentsBucket })
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
            if (!exists)
            {
                await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
                _logger.LogInformation("Created MinIO bucket: {Bucket}", bucket);
            }
        }

        // Set public read policy for images bucket so URLs can be embedded directly in <img> tags
        var imagePolicy = $$"""
            {
                "Version": "2012-10-17",
                "Statement": [{
                    "Effect": "Allow",
                    "Principal": {"AWS": ["*"]},
                    "Action": ["s3:GetObject"],
                    "Resource": ["arn:aws:s3:::{{_settings.ImagesBucket}}/*"]
                }]
            }
            """;
        await _minio.SetPolicyAsync(new SetPolicyArgs()
            .WithBucket(_settings.ImagesBucket)
            .WithPolicy(imagePolicy));

        _logger.LogInformation("MinIO buckets ready: {ImagesBucket}, {DocumentsBucket}",
            _settings.ImagesBucket, _settings.DocumentsBucket);
    }

    public async Task UploadAsync(string bucket, string objectKey, Stream stream, string contentType, long size)
    {
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(size)
            .WithContentType(contentType));
    }

    public async Task DeleteAsync(string bucket, string objectKey)
    {
        try
        {
            await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete object {Key} from bucket {Bucket}", objectKey, bucket);
        }
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(string bucket, string objectKey, int expirySeconds = 300)
    {
        return await _minio.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithExpiry(expirySeconds));
    }

    public string GetPublicUrl(string bucket, string objectKey)
    {
        return $"{_settings.PublicEndpoint.TrimEnd('/')}/{bucket}/{objectKey}";
    }
}
