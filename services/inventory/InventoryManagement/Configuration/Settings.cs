namespace InventoryManagement.Configuration;

public class PostgresSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public class MinioSettings
{
    public string Endpoint { get; set; } = "localhost:9002";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public bool UseSSL { get; set; } = false;
    public string ImagesBucket { get; set; } = "erp-item-images";
    public string DocumentsBucket { get; set; } = "erp-item-documents";
    public string PublicEndpoint { get; set; } = "http://localhost:9002";
}
