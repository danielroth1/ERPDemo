namespace DashboardAnalytics.Configuration;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string ConsumerGroupId { get; set; } = string.Empty;
}
