using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using SilkRoute.Sample.Contracts.MicroserviceClients;
using SilkRoute.Sample.Contracts.Models;

namespace SilkRoute.Sample.SalesService.Api.Controllers;

[ApiController]
[Route("api/sales/billing")]
public sealed class SalesBillingGatewayController : ControllerBase
{
    private readonly IBillingMicroserviceClient _billing;

    public SalesBillingGatewayController(IBillingMicroserviceClient billing)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    [HttpPost("orders")]
    public async Task<ActionResult<PlaceOrderResponse>> PlaceOrderAsync([FromBody] CreateOrderRequest? request)
    {
        if (request is null)
        {
            return BadRequest("Request body is missing.");
        }

        if (request.Lines.Count == 0)
        {
            return BadRequest("Order must contain at least one line.");
        }

        var invoiceRequest = new CreateInvoiceRequest
        {
            CustomerId = request.CustomerId,
            Lines = request.Lines.Select(l => new CreateInvoiceLineRequest
            {
                Description = l.Sku,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        var invoiceResult = await _billing.CreateInvoiceAsync(invoiceRequest);
        var invoice = invoiceResult.Value;

        if (invoice is null)
        {
            return StatusCode(502, "Billing did not return an invoice payload.");
        }

        return Ok(new PlaceOrderResponse
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.Number,
            Total = invoice.Total,
            Currency = invoice.Currency
        });
    }

    [HttpGet("invoices/{invoiceId:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _billing.GetInvoiceAsync(invoiceId);
        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(invoice);
    }

    [HttpGet("invoices/{invoiceId:guid}/status")]
    public async Task<ActionResult<string>> GetInvoiceStatusAsync(Guid invoiceId)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var status = await _billing.GetInvoiceStatusAsync(invoiceId, correlationId);
        return Ok(status);
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> SearchInvoicesAsync(
        [FromQuery] Guid? customerId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new InvoiceSearchQuery
        {
            CustomerId = customerId,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await _billing.SearchInvoicesAsync(query);
        return Ok(result);
    }

    [HttpGet("invoices/{invoiceId:guid}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdfAsync(
        Guid invoiceId,
        [FromQuery] string mode = "bytes")
    {
        if (string.Equals(mode, "stream", StringComparison.OrdinalIgnoreCase))
        {
            var stream = await _billing.DownloadInvoicePdfStreamAsync(invoiceId);
            return File(stream, MediaTypeNames.Application.Pdf, $"invoice-{invoiceId:N}.pdf");
        }

        var bytes = await _billing.DownloadInvoicePdfBytesAsync(invoiceId);
        return File(bytes, MediaTypeNames.Application.Pdf, $"invoice-{invoiceId:N}.pdf");
    }

    [HttpPost("invoices/{invoiceId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AttachmentDto>> UploadAttachmentAsync(
        Guid invoiceId,
        IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File is missing or empty.");
        }

        var form = new InvoiceAttachmentForm { File = file };

        var result = await _billing.UploadInvoiceAttachmentAsync(invoiceId, form);
        var dto = result.Value;

        if (dto is null)
        {
            return StatusCode(502, "Billing did not return an attachment payload.");
        }

        return Ok(dto);
    }

    [HttpPost("invoices/import")]
    public async Task<ActionResult<ImportResultDto>> ImportInvoicesAsync(
        IFormFile? archive,
        [FromForm] string? sourceName)
    {
        if (archive is null || archive.Length == 0)
        {
            return BadRequest("Archive file is missing or empty.");
        }

        await using var ms = new MemoryStream();
        await archive.CopyToAsync(ms);

        var request = new BulkImportRequest
        {
            SourceName = sourceName,
            ArchiveBytes = ms.ToArray()
        };

        var result = await _billing.ImportInvoicesAsync(request);
        var dto = result.Value;

        if (dto is null)
        {
            return StatusCode(502, "Billing did not return an import result payload.");
        }

        return Ok(dto);
    }

    [HttpPost("admin/company-logo")]
    public async Task<IActionResult> UploadCompanyLogoAsync(
        IFormFile? logo,
        [FromQuery] string mode = "bytes")
    {
        if (logo is null || logo.Length == 0)
        {
            return BadRequest("Logo file is missing or empty.");
        }

        if (string.Equals(mode, "stream", StringComparison.OrdinalIgnoreCase))
        {
            await using var s = logo.OpenReadStream();
            return await _billing.UploadCompanyLogoStreamAsync(s);
        }

        await using var ms = new MemoryStream();
        await logo.CopyToAsync(ms);
        return await _billing.UploadCompanyLogoBytesAsync(ms.ToArray());
    }
}