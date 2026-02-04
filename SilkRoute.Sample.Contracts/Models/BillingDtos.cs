using Microsoft.AspNetCore.Http;

namespace SilkRoute.Sample.Contracts.Models;

public enum InvoiceStatus
{
    Issued = 0,
    Paid = 1,
    Overdue = 2,
    Cancelled = 3
}

public sealed class InvoiceDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public decimal Total { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTimeOffset IssuedAt { get; init; }
    public InvoiceStatus Status { get; init; }
}

public sealed class InvoiceListItemDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Currency { get; init; } = "USD";
    public InvoiceStatus Status { get; init; }
}

public sealed class InvoiceSearchQuery
{
    public Guid? CustomerId { get; init; }
    public InvoiceStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
}

public sealed class CreateInvoiceRequest
{
    public Guid CustomerId { get; init; }
    public List<CreateInvoiceLineRequest> Lines { get; init; } = new();
}

public sealed class CreateInvoiceLineRequest
{
    public string Description { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class BulkImportRequest
{
    public string SourceName { get; init; }
    public byte[] ArchiveBytes { get; init; }
}

public sealed class ImportResultDto
{
    public int ImportedCount { get; init; }
    public int FailedCount { get; init; }
    public string Message { get; init; }
}

public sealed class AttachmentDto
{
    public Guid AttachmentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public long Size { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}

public sealed class InvoiceAttachmentForm
{
    public IFormFile File { get; init; }
}