using Dapper;
using InventoryApp.Core.Enums;
using InventoryApp.Core.Interfaces;
using InventoryApp.Infrastructure.Persistence;

namespace InventoryApp.Infrastructure.Services;

public sealed class DocNoGenerator : IDocNoGenerator
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public DocNoGenerator(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<string> GenerateAsync(DocumentType documentType, DateTime docDate, CancellationToken cancellationToken = default)
    {
        _ = documentType; // Prefix is unified as P- by business rule.
        var dateToken = docDate.ToString("ddMMyyyy");
        var pattern = $"P-{dateToken}-%";

        const string sql = @"
SELECT doc_no FROM stock_documents
WHERE doc_no LIKE @pattern
ORDER BY doc_no DESC
LIMIT 1;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var last = await conn.ExecuteScalarAsync<string?>(sql, new { pattern });

        var next = 1;
        if (!string.IsNullOrWhiteSpace(last))
        {
            var suffix = last.Split('-').LastOrDefault();
            if (int.TryParse(suffix, out var current))
                next = current + 1;
        }

        return $"P-{dateToken}-{next:D4}";
    }
}
