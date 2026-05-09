using InventoryApp.Core.Enums;

namespace InventoryApp.Core.Interfaces;

public interface IDocNoGenerator
{
    Task<string> GenerateAsync(DocumentType documentType, DateTime docDate, CancellationToken cancellationToken = default);
}
