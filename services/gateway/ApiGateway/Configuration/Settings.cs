namespace ApiGateway.Configuration;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public class ServiceEndpoints
{
    public string UserManagement { get; set; } = string.Empty;
    public string Inventory { get; set; } = string.Empty;
    public string Sales { get; set; } = string.Empty;
    public string Financial { get; set; } = string.Empty;
    public string Dashboard { get; set; } = string.Empty;
}
