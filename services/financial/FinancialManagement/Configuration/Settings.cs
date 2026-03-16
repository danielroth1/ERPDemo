namespace FinancialManagement.Configuration;

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
