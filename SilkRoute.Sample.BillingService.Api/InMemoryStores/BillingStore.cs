using System.Collections.Concurrent;
using SilkRoute.Sample.Contracts.Models;

namespace SilkRoute.Sample.BillingService.Api.InMemoryStores;

internal static class BillingStore
{
    public static readonly ConcurrentDictionary<Guid, InvoiceDto> Invoices = new();
    public static readonly ConcurrentDictionary<Guid, AttachmentDto> Attachments = new();

    public static byte[]? CompanyLogoBytes { get; private set; }

    static BillingStore()
    {
        Seed();
    }

    public static void SetCompanyLogo(byte[] bytes)
    {
        CompanyLogoBytes = bytes;
    }

    public static Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId)
    {
        Invoices.TryGetValue(invoiceId, out var invoice);
        return Task.FromResult(invoice);
    }

    public static Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        var id = Guid.NewGuid();
        var total = request.Lines.Sum(l => l.Quantity * l.UnitPrice);

        var invoice = new InvoiceDto
        {
            Id = id,
            Number = $"INV-{id.ToString("N")[..8].ToUpperInvariant()}",
            CustomerId = request.CustomerId,
            Total = total,
            Currency = "USD",
            IssuedAt = DateTimeOffset.UtcNow,
            Status = InvoiceStatus.Issued
        };

        Invoices[id] = invoice;

        return Task.FromResult(invoice);
    }

    public static Task<PagedResult<InvoiceListItemDto>> SearchInvoicesAsync(InvoiceSearchQuery query)
    {
        var items = Invoices.Values
            .Where(i => query.CustomerId == null || i.CustomerId == query.CustomerId)
            .Where(i => query.Status == null || i.Status == query.Status)
            .Select(i => new InvoiceListItemDto
            {
                Id = i.Id,
                Number = i.Number,
                Total = i.Total,
                Currency = i.Currency,
                Status = i.Status
            })
            .ToList();

        var total = items.Count;

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        var paged = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<InvoiceListItemDto>
        {
            Items = paged,
            TotalCount = total
        };

        return Task.FromResult(result);
    }

    public static Task<ImportResultDto> ImportInvoicesAsync(BulkImportRequest request)
    {
        var imported = Math.Clamp(request.ArchiveBytes!.Length / 128, 1, 25);

        for (var i = 0; i < imported; i++)
        {
            var id = Guid.NewGuid();
            Invoices[id] = new InvoiceDto
            {
                Id = id,
                Number = $"IMP-{id.ToString("N")[..8].ToUpperInvariant()}",
                CustomerId = Guid.NewGuid(),
                Total = 49.99m + i,
                Currency = "USD",
                IssuedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
                Status = InvoiceStatus.Issued
            };
        }

        var result = new ImportResultDto
        {
            ImportedCount = imported,
            FailedCount = 0,
            Message = $"Imported {imported} invoices from '{request.SourceName ?? "unknown"}'."
        };

        return Task.FromResult(result);
    }

    public static async Task<byte[]> ReadInvoicePdfBytesAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "InvoiceFile.pdf");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"PDF asset not found at '{path}'.");
        }

        return await File.ReadAllBytesAsync(path);
    }

    public static async Task<AttachmentDto> SaveAttachmentAsync(
        Guid invoiceId,
        IFormFile file)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var id = Guid.NewGuid();

        var dto = new AttachmentDto
        {
            AttachmentId = id,
            FileName = string.IsNullOrWhiteSpace(file.FileName) ? "attachment.bin" : file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Size = file.Length,
            UploadedAt = DateTimeOffset.UtcNow
        };

        Attachments[id] = dto;

        return dto;
    }

    private static void Seed()
    {
        var customerA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var customerB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var inv1 = new InvoiceDto
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Number = "INV-SEED-0001",
            CustomerId = customerA,
            Total = 120.00m,
            Currency = "USD",
            IssuedAt = DateTimeOffset.UtcNow.AddDays(-10),
            Status = InvoiceStatus.Issued
        };

        var inv2 = new InvoiceDto
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Number = "INV-SEED-0002",
            CustomerId = customerA,
            Total = 89.50m,
            Currency = "USD",
            IssuedAt = DateTimeOffset.UtcNow.AddDays(-6),
            Status = InvoiceStatus.Paid
        };

        var inv3 = new InvoiceDto
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Number = "INV-SEED-0003",
            CustomerId = customerB,
            Total = 310.10m,
            Currency = "USD",
            IssuedAt = DateTimeOffset.UtcNow.AddDays(-3),
            Status = InvoiceStatus.Overdue
        };

        Invoices[inv1.Id] = inv1;
        Invoices[inv2.Id] = inv2;
        Invoices[inv3.Id] = inv3;

        var att1 = new AttachmentDto
        {
            AttachmentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            FileName = "seed-attachment-1.txt",
            ContentType = "text/plain",
            Size = 128,
            UploadedAt = DateTimeOffset.UtcNow.AddDays(-9)
        };

        var att2 = new AttachmentDto
        {
            AttachmentId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            FileName = "seed-attachment-2.pdf",
            ContentType = "application/pdf",
            Size = 2048,
            UploadedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var att3 = new AttachmentDto
        {
            AttachmentId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            FileName = "seed-attachment-3.bin",
            ContentType = "application/octet-stream",
            Size = 4096,
            UploadedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        Attachments[att1.AttachmentId] = att1;
        Attachments[att2.AttachmentId] = att2;
        Attachments[att3.AttachmentId] = att3;
    }
}