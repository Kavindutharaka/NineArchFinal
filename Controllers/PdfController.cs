using Microsoft.AspNetCore.Mvc;
using NineArchTours.Models;
using NineArchTours.Services;

namespace NineArchTours.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly PdfService _pdfService;

        public PdfController(PdfService pdfService)
        {
            _pdfService = pdfService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] QuoteRequest request)
        {
            var pdfBytes = await _pdfService.GeneratePdfAsync(request);
            var safeName = string.IsNullOrWhiteSpace(request.ClientName) ? "Client" : request.ClientName.Replace(" ", "_");
            var fileName = $"9Arch_TourPackage_{safeName}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // Preview endpoint: returns the HTML that would be converted to PDF
        [HttpPost("preview")]
        public IActionResult Preview([FromBody] QuoteRequest request)
        {
            var html = _pdfService.BuildPdfHtml(request);
            return Content(html, "text/html");
        }
    }
}
