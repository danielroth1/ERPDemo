namespace Orchestration.Configuration;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "erp-system";
    public string Audience { get; set; } = "erp-clients";
}
