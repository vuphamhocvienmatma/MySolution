

namespace Infrastructure.Configuration;

public class ElasticsearchSettings
{
    public const string SectionName = "Elasticsearch";

    [Required]
    [Url]
    public required string Uri { get; init; }
}