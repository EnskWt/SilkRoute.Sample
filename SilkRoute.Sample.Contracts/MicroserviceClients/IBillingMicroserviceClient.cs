using Microsoft.AspNetCore.Mvc;
using SilkRoute.Public.Abstractions;
using SilkRoute.Sample.Contracts.Models;

namespace SilkRoute.Sample.Contracts.MicroserviceClients;

public interface IBillingMicroserviceClient : IMicroserviceClient
{
    [HttpGet("api/billing/invoices/{invoiceId:guid}")] 
    Task<InvoiceDto> GetInvoiceAsync([FromRoute] Guid invoiceId);
    
    [HttpGet("api/billing/invoices/{invoiceId:guid}/status")] 
    Task<string> GetInvoiceStatusAsync(
        [FromRoute] Guid invoiceId,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId);
    
    [HttpGet("api/billing/invoices/search")] 
    Task<PagedResult<InvoiceListItemDto>> SearchInvoicesAsync([FromQuery] InvoiceSearchQuery query);
    
    [HttpPost("api/billing/invoices")] 
    Task<ActionResult<InvoiceDto>> CreateInvoiceAsync([FromBody] CreateInvoiceRequest request);
    
    [HttpPost("api/billing/assets/company-logo/bytes")] 
    Task<IActionResult> UploadCompanyLogoBytesAsync([FromBody] byte[] logoBytes);
    
    [HttpPost("api/billing/assets/company-logo/stream")] 
    Task<IActionResult> UploadCompanyLogoStreamAsync([FromBody] Stream logoStream);
    
    [HttpGet("api/billing/invoices/{invoiceId:guid}/pdf/bytes")]
    Task<byte[]> DownloadInvoicePdfBytesAsync([FromRoute] Guid invoiceId);
    
    [HttpGet("api/billing/invoices/{invoiceId:guid}/pdf/stream")] 
    Task<Stream> DownloadInvoicePdfStreamAsync([FromRoute] Guid invoiceId);
    
    [HttpPost("api/billing/invoices/import")] 
    Task<ActionResult<ImportResultDto>> ImportInvoicesAsync([FromBody] BulkImportRequest request);
    
    [HttpPost("api/billing/invoices/{invoiceId:guid}/attachments")] 
    Task<ActionResult<AttachmentDto>> UploadInvoiceAttachmentAsync(
        [FromRoute] Guid invoiceId,
        [FromForm] InvoiceAttachmentForm form);
}