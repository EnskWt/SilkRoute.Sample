using Microsoft.AspNetCore.Mvc;
using SilkRoute.Sample.BillingService.Api.InMemoryStores;
using SilkRoute.Sample.Contracts.MicroserviceClients;
using SilkRoute.Sample.Contracts.Models;

namespace SilkRoute.Sample.BillingService.Api.Controllers;

[ApiController]
public sealed class BillingController : ControllerBase, IBillingMicroserviceClient
{
    [HttpGet("api/billing/invoices/{invoiceId:guid}")]
    public async Task<InvoiceDto> GetInvoiceAsync(Guid invoiceId)
    {
        var invoice = await BillingStore.GetInvoiceAsync(invoiceId);
        if (invoice is null)
        {
            throw new InvalidOperationException($"Invoice '{invoiceId}' not found.");
        }

        return invoice;
    }

    [HttpGet("api/billing/invoices/{invoiceId:guid}/status")]
    public async Task<string> GetInvoiceStatusAsync(
        Guid invoiceId,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId)
    {
        var invoice = await BillingStore.GetInvoiceAsync(invoiceId);
        if (invoice is null)
        {
            return $"NotFound (corr={correlationId})";
        }

        return $"{invoice.Status} (corr={correlationId})";
    }

    [HttpGet("api/billing/invoices/search")]
    public async Task<PagedResult<InvoiceListItemDto>> SearchInvoicesAsync([FromQuery] InvoiceSearchQuery query)
    {
        return await BillingStore.SearchInvoicesAsync(query);
    }

    [HttpPost("api/billing/invoices")]
    public async Task<ActionResult<InvoiceDto>> CreateInvoiceAsync([FromBody] CreateInvoiceRequest? request)
    {
        if (request is null)
        {
            return BadRequest("Request body is missing.");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return BadRequest("Invoice must contain at least one line.");
        }

        var invoice = await BillingStore.CreateInvoiceAsync(request);
        return Ok(invoice);
    }

    [HttpPost("api/billing/assets/company-logo/bytes")]
    public async Task<IActionResult> UploadCompanyLogoBytesAsync([FromBody] byte[]? logoBytes)
    {
        if (logoBytes is null || logoBytes.Length == 0)
        {
            return BadRequest("Logo bytes are empty.");
        }

        await Task.Yield();
        BillingStore.SetCompanyLogo(logoBytes);

        return NoContent();
    }

    [HttpPost("api/billing/assets/company-logo/stream")]
    public async Task<IActionResult> UploadCompanyLogoStreamAsync([FromBody] Stream? logoStream)
    {
        if (logoStream is null)
        {
            return BadRequest("Logo stream is missing.");
        }

        await using var ms = new MemoryStream();
        await logoStream.CopyToAsync(ms);

        var bytes = ms.ToArray();
        if (bytes.Length == 0)
        {
            return BadRequest("Logo stream is empty.");
        }

        BillingStore.SetCompanyLogo(bytes);

        return NoContent();
    }

    [HttpGet("api/billing/invoices/{invoiceId:guid}/pdf/bytes")]
    public async Task<byte[]> DownloadInvoicePdfBytesAsync(Guid invoiceId)
    {
        return await BillingStore.ReadInvoicePdfBytesAsync();
    }

    [HttpGet("api/billing/invoices/{invoiceId:guid}/pdf/stream")]
    public async Task<Stream> DownloadInvoicePdfStreamAsync(Guid invoiceId)
    {
        var bytes = await BillingStore.ReadInvoicePdfBytesAsync();
        return new MemoryStream(bytes);
    }

    [HttpPost("api/billing/invoices/import")]
    public async Task<ActionResult<ImportResultDto>> ImportInvoicesAsync([FromBody] BulkImportRequest? request)
    {
        if (request is null)
        {
            return BadRequest("Request body is missing.");
        }

        if (request.ArchiveBytes is null || request.ArchiveBytes.Length == 0)
        {
            return BadRequest("ArchiveBytes are empty.");
        }

        var result = await BillingStore.ImportInvoicesAsync(request);
        return Ok(result);
    }

    [HttpPost("api/billing/invoices/{invoiceId:guid}/attachments")]
    public async Task<ActionResult<AttachmentDto>> UploadInvoiceAttachmentAsync(
        Guid invoiceId,
        [FromForm] InvoiceAttachmentForm? form)
    {
        if (form is null)
        {
            return BadRequest("Form is missing.");
        }

        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest("File is missing or empty.");
        }

        var dto = await BillingStore.SaveAttachmentAsync(invoiceId, form.File);
        return Ok(dto);
    }
}