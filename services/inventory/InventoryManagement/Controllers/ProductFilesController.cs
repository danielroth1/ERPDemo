using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Configuration;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;
using InventoryManagement.Models.DTOs;
using InventoryManagement.Services;

namespace InventoryManagement.Controllers;

[ApiController]
[Route("api/v1/productfiles/{id}")]
[Authorize]
public class ProductFilesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IFileStorageService _storage;
    private readonly MinioSettings _minioSettings;
    private readonly ILogger<ProductFilesController> _logger;

    private static readonly string[] AllowedImageTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private const long MaxImageSize = 10 * 1024 * 1024;      // 10 MB
    private const long MaxDocumentSize = 50 * 1024 * 1024;   // 50 MB

    public ProductFilesController(
        AppDbContext dbContext,
        IFileStorageService storage,
        MinioSettings minioSettings,
        ILogger<ProductFilesController> logger)
    {
        _dbContext = dbContext;
        _storage = storage;
        _minioSettings = minioSettings;
        _logger = logger;
    }

    // POST /api/v1/productfiles/{id}/files/image
    [HttpPost("image")]
    [Authorize(Roles = "Admin")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<ApiResponse<string>>> UploadImage(string id, IFormFile file)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound(ApiResponse<string>.ErrorResponse("Product not found"));

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<string>.ErrorResponse("No file uploaded"));

        if (!AllowedImageTypes.Contains(file.ContentType))
            return BadRequest(ApiResponse<string>.ErrorResponse(
                "Invalid image type. Allowed types: JPEG, PNG, WebP, GIF"));

        if (file.Length > MaxImageSize)
            return BadRequest(ApiResponse<string>.ErrorResponse("Image exceeds the 10 MB size limit"));

        // Delete previous image from storage
        if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            var oldKey = ParseStorageKey(product.ImageUrl);
            _logger.LogInformation("Deleting old image for product {ProductId}: {StorageKey}", id, oldKey);
            await _storage.DeleteAsync(_minioSettings.ImagesBucket, oldKey);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var objectKey = $"products/{id}/image{ext}";

        await using var stream = file.OpenReadStream();
        _logger.LogInformation("Uploading new image for product {ProductId}: {FileName} ({ContentType}, {Size} bytes)",
            id, file.FileName, file.ContentType, file.Length);
        await _storage.UploadAsync(_minioSettings.ImagesBucket, objectKey, stream, file.ContentType, file.Length);

        product.ImageUrl = _storage.GetPublicUrl(_minioSettings.ImagesBucket, objectKey);
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Image uploaded for product {ProductId}", id);
        return Ok(ApiResponse<string>.SuccessResponse(product.ImageUrl));
    }

    // DELETE /api/v1/productfiles/{id}/files/image
    [HttpDelete("image")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<EmptyResponse>>> DeleteImage(string id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound(ApiResponse<EmptyResponse>.ErrorResponse("Product not found"));

        if (string.IsNullOrEmpty(product.ImageUrl))
            return BadRequest(ApiResponse<EmptyResponse>.ErrorResponse("Product has no image"));

        var objectKey = ParseStorageKey(product.ImageUrl);
        await _storage.DeleteAsync(_minioSettings.ImagesBucket, objectKey);

        product.ImageUrl = null;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(ApiResponse<EmptyResponse>.SuccessResponse(new EmptyResponse()));
    }

    // POST /api/v1/productfiles/{id}/files/documents
    [HttpPost("documents")]
    [Authorize(Roles = "Admin")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<ApiResponse<ProductDocumentDto>>> UploadDocument(
        string id, IFormFile file)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound(ApiResponse<ProductDocumentDto>.ErrorResponse("Product not found"));

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<ProductDocumentDto>.ErrorResponse("No file uploaded"));

        if (file.Length > MaxDocumentSize)
            return BadRequest(ApiResponse<ProductDocumentDto>.ErrorResponse(
                "Document exceeds the 50 MB size limit"));

        var documentId = Guid.NewGuid().ToString();
        var ext = Path.GetExtension(file.FileName);
        var objectKey = $"products/{id}/documents/{documentId}{ext}";
        var uploadedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.Identity?.Name ?? "unknown";

        await using var stream = file.OpenReadStream();
        await _storage.UploadAsync(_minioSettings.DocumentsBucket, objectKey, stream, file.ContentType, file.Length);

        var doc = new ProductDocument
        {
            Id = documentId,
            ProductId = id,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            StorageKey = objectKey,
            SizeBytes = file.Length,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        _dbContext.ProductDocuments.Add(doc);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Document uploaded for product {ProductId}: {FileName}",
            id, doc.OriginalFileName);

        return Ok(ApiResponse<ProductDocumentDto>.SuccessResponse(MapToDto(doc)));
    }

    // GET /api/v1/productfiles/{id}/files/documents
    [HttpGet("documents")]
    public async Task<ActionResult<ApiResponse<List<ProductDocumentDto>>>> ListDocuments(string id)
    {
        var productExists = await _dbContext.Products.AnyAsync(p => p.Id == id);
        if (!productExists)
            return NotFound(ApiResponse<List<ProductDocumentDto>>.ErrorResponse("Product not found"));

        var docs = await _dbContext.ProductDocuments
            .Where(d => d.ProductId == id)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<ProductDocumentDto>>.SuccessResponse(docs.Select(MapToDto).ToList()));
    }

    // GET /api/v1/productfiles/{id}/files/documents/{docId}/download
    // Returns a 302 redirect to a short-lived presigned MinIO URL (5 min).
    // Auth is enforced here; documents bucket is private.
    [HttpGet("documents/{docId}/download")]
    public async Task<ActionResult> DownloadDocument(string id, string docId)
    {
        var doc = await _dbContext.ProductDocuments
            .FirstOrDefaultAsync(d => d.Id == docId && d.ProductId == id);

        if (doc == null) return NotFound();

        var presignedUrl = await _storage.GeneratePresignedDownloadUrlAsync(
            _minioSettings.DocumentsBucket, doc.StorageKey, expirySeconds: 300);

        return Redirect(presignedUrl);
    }

    // DELETE /api/v1/productfiles/{id}/files/documents/{docId}
    [HttpDelete("documents/{docId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<EmptyResponse>>> DeleteDocument(string id, string docId)
    {
        var doc = await _dbContext.ProductDocuments
            .FirstOrDefaultAsync(d => d.Id == docId && d.ProductId == id);

        if (doc == null)
            return NotFound(ApiResponse<EmptyResponse>.ErrorResponse("Document not found"));

        await _storage.DeleteAsync(_minioSettings.DocumentsBucket, doc.StorageKey);
        _dbContext.ProductDocuments.Remove(doc);
        await _dbContext.SaveChangesAsync();

        return Ok(ApiResponse<EmptyResponse>.SuccessResponse(new EmptyResponse()));
    }

    private static ProductDocumentDto MapToDto(ProductDocument doc) => new()
    {
        Id = doc.Id,
        ProductId = doc.ProductId,
        OriginalFileName = doc.OriginalFileName,
        ContentType = doc.ContentType,
        SizeBytes = doc.SizeBytes,
        UploadedBy = doc.UploadedBy,
        UploadedAt = doc.UploadedAt
    };

    // Extracts the object key from a public URL: {endpoint}/{bucket}/{key...}
    private static string ParseStorageKey(string publicUrl)
    {
        var uri = new Uri(publicUrl);
        // AbsolutePath = "/{bucket}/{key...}", split off bucket part
        var path = uri.AbsolutePath.TrimStart('/');
        var slash = path.IndexOf('/');
        return slash >= 0 ? path[(slash + 1)..] : path;
    }
}
