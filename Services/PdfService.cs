using NineArchTours.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace NineArchTours.Services
{
    public class PdfService
    {
        private readonly IWebHostEnvironment _env;

        public PdfService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<byte[]> GeneratePdfAsync(QuoteRequest request)
        {
            var html = BuildPdfHtml(request);

            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 60000
            });

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "0mm",
                    Bottom = "0mm",
                    Left = "0mm",
                    Right = "0mm"
                }
            });

            return pdfBytes;
        }

        public string BuildPdfHtml(QuoteRequest request)
        {
            // Load base background 
            var bgPath = Path.Combine(_env.WebRootPath, "assets", "pdfbg.png");
            var bgBase64 = GetBase64Image(bgPath);

            var nightsLabel = request.NumberOfNights > 0
                ? $"{request.NumberOfNights} Nights / {request.NumberOfNights + 1} Days"
                : $"{request.Days.Count - 1} Nights / {request.Days.Count} Days";

            var routeSummary = string.Join(" → ",
                request.Days
                    .Where(d => !string.IsNullOrEmpty(d.HotelLocation))
                    .Select(d => d.HotelLocation)
                    .Distinct());

            var daysHtml = BuildDaysHtml(request.Days);
            var inclusionsHtml = string.Join("\n", request.Inclusions.Select(i => $"<li>{Esc(i)}</li>"));
            var exclusionsHtml = string.Join("\n", request.Exclusions.Select(e => $"<li>{Esc(e)}</li>"));

            var accommodationHtml = "";
            var hasOpt1 = request.Option1 != null && request.Option1.Hotels.Count > 0;
            var hasOpt2 = request.Option2 != null && request.Option2.Hotels.Count > 0;

            if (hasOpt1)
            {
                accommodationHtml += BuildOptionTablePage(request.Option1!, request.CurrencyCode, "Option 1");
                if (hasOpt2)
                    accommodationHtml += BuildOptionTablePage(request.Option2!, request.CurrencyCode, "Option 2");
            }

            var costHtml = "";
            if (!hasOpt1 && request.TotalCost > 0)
            {
                costHtml = $@"
                <div class='cost-banner'>
                    <div class='cost-label'>Total Tour Cost</div>
                    <div class='cost-value'>{Esc(request.CurrencyCode)} {request.TotalCost:N2}</div>
                </div>";
            }

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Open+Sans:wght@300;400;600;700&display=swap');
        * {{ margin:0; padding:0; box-sizing:border-box; }}
        body {{ font-family:'Open Sans','Segoe UI',sans-serif; color:#333; font-size:11pt; line-height:1.6; }}
        
        .page {{
            width: 210mm; 
            height: 297mm; /* Force exact A4 height */
            position: relative;
            page-break-after: always; 
            overflow: hidden; 
            background: #fff;
        }}
        .page-bg {{
            position: absolute; top:0; left:0; width:100%; height:100%; z-index:0;
            {(string.IsNullOrEmpty(bgBase64) ? "" : $"background-image:url('{bgBase64}');")}
            background-size: cover; background-position: center; background-repeat: no-repeat;
        }}
        
        /* THE MAGIC CONTAINER */
        .page-content {{
            position: relative; 
            z-index: 1; 
            padding: 160px 60px 100px 60px; /* Safe zones from top/bottom graphics */
            height: 100%; /* Fills the 297mm page */
            display: flex;
            flex-direction: column;
            justify-content: center; /* VERTICALLY CENTERS EVERYTHING */
        }}

        /* COVER PAGE */
        .cover-content {{
            position: relative; 
            z-index: 1; 
            display: flex; 
            flex-direction: column; 
            align-items: center; 
            justify-content: center; 
            height: 100%;
            text-align: center;
            padding: 160px 60px 100px 60px;
        }}
        .cover-title {{ font-size:26pt; font-weight:700; color:#0a1628; margin-bottom:10px; }}
        .cover-duration {{ font-size:16pt; color:#333; margin-bottom:5px; font-weight:600; }}
        .cover-subtitle {{ font-size:14pt; color:#0066cc; font-weight:600; margin-bottom:5px; }}
        .cover-badge {{ font-size:12pt; color:#555; font-style:italic; margin-bottom:30px; }}
        .cover-details p {{ font-size:12pt; color:#444; margin-bottom:8px; }}

        /* DAY PAGES */
        .day-image-grid {{
            display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 8px;
            height: 230px; margin-top: 22px; border-radius: 8px; overflow: hidden; flex-shrink: 0;
        }}
        .day-image-grid img {{ width: 100%; height: 100%; object-fit: cover; display: block; }}
        /* VEHICLE BOX ON COVER */
        .vehicle-box {{ margin-top:22px; display:inline-flex; align-items:center; gap:14px; background:#f0f6ff; border:1px solid #c8dff7; border-radius:8px; padding:12px 20px; font-size:11pt; color:#333; }}
        .vehicle-box .vdetail {{ text-align:left; }}
        .vehicle-box .vmodel {{ font-weight:700; color:#0a1628; font-size:12pt; }}
        .vehicle-box .vmeta {{ font-size:9.5pt; color:#555; margin-top:3px; }}
        .day-title {{ font-size: 16pt; font-weight: 700; color: #0a1628; margin-bottom: 15px; border-bottom: 2px solid #0066cc; padding-bottom: 5px; }}
        .day-text ul {{ list-style: none; padding: 0; margin: 0 0 15px 0; }}
        .day-text ul li {{ padding: 4px 0 4px 20px; position: relative; font-size: 10.5pt; }}
        .day-text ul li::before {{ content: '\2022'; position: absolute; left: 0; color: #0066cc; font-weight: bold; font-size: 14pt; line-height: 1; top: 2px; }}
        .day-meta {{ font-size: 10.5pt; color: #555; margin-top: 10px; }}
        .day-meta strong {{ color: #0066cc; }}

        /* ACCOMMODATION TABLE */
        .acc-title {{ font-size:16pt; font-weight:700; color:#0a1628; margin-bottom:20px; }}
        .acc-table {{ width: 100%; border-collapse: collapse; margin-bottom: 30px; font-size: 10pt; }}
        .acc-table th {{ background: #0066cc; color: white; padding: 12px; text-align: left; font-weight: 600; border: 1px solid #0055aa; }}
        .acc-table td {{ padding: 12px; border: 1px solid #ddd; color: #333; }}
        .acc-table tr:nth-child(even) {{ background: #f9fbfd; }}
        
        /* INCLUSIONS / EXCLUSIONS */
        .section-title {{ font-size:16pt; font-weight:700; color:#0a1628; margin-bottom:15px; border-bottom:2px solid #0066cc; padding-bottom:5px; }}
        .two-col {{ display:flex; gap:40px; margin-top: 20px; }}
        .two-col > div {{ flex:1; }}
        .two-col h3 {{ font-size:12pt; color:#0066cc; margin-bottom:12px; }}
        .two-col ul {{ list-style:none; padding:0; }}
        .two-col ul li {{ padding:5px 0 5px 22px; position:relative; font-size:10pt; }}
        .inclusions-list li::before {{ content:'\2713'; position:absolute; left:0; color:#28a745; font-weight:bold; font-size: 12pt; top: 2px; }}
        .exclusions-list li::before {{ content:'\2717'; position:absolute; left:0; color:#dc3545; font-weight:bold; font-size: 12pt; top: 2px; }}

        .cost-banner {{ background:#f4f8fc; border-left: 5px solid #0066cc; padding:15px 20px; margin-top:25px; }}
        .cost-value {{ font-size:18pt; font-weight:700; color: #0a1628; }}

        @media print {{ .page {{ page-break-after:always; }} }}
    </style>
</head>
<body>

    <div class='page'>
        <div class='page-bg'></div>
        <div class='cover-content'>
            <div class='cover-title'>Sri Lanka Tour Itinerary</div>
            <div class='cover-duration'>{nightsLabel}</div>
            <div class='cover-subtitle'>{Esc(request.TourTitle)}</div>
            <div class='cover-badge'>(Personalized Luxury Tour)</div>
            
            <div class='cover-details'>
                <p><strong>Route:</strong> {Esc(routeSummary)}</p>
                <p><strong>Hotel Category:</strong> {Esc(request.HotelCategory)} &nbsp;|&nbsp; <strong>Meal Plan:</strong> {Esc(request.MealPlan)}</p>
                <br/><br/>
                <p style='font-size: 14pt;'>Prepared for <strong>{Esc(request.ClientName)}</strong></p>
            </div>
            {(!string.IsNullOrEmpty(request.VehicleModel) ? BuildVehicleBox(request) : "")}
        </div>
    </div>

    {daysHtml}

    {accommodationHtml}

    <div class='page'>
        <div class='page-bg'></div>
        <div class='page-content'>
            <h2 class='section-title'>Tour Package Details</h2>
            <div class='two-col'>
                <div>
                    <h3>Inclusions</h3>
                    <ul class='inclusions-list'>{inclusionsHtml}</ul>
                </div>
                <div>
                    <h3>Exclusions</h3>
                    <ul class='exclusions-list'>{exclusionsHtml}</ul>
                </div>
            </div>
            {costHtml}
        </div>
    </div>

</body>
</html>";
        }

        private string BuildVehicleBox(QuoteRequest request)
        {
            var imgHtml = "";
            if (!string.IsNullOrEmpty(request.VehicleImage))
            {
                var b64 = ToBase64Url(request.VehicleImage);
                if (!string.IsNullOrEmpty(b64))
                    imgHtml = $"<img src='{b64}' style='width:90px;height:60px;object-fit:cover;border-radius:6px;flex-shrink:0;' />";
            }

            return $@"
            <div class='vehicle-box'>
                {imgHtml}
                <div class='vdetail'>
                    <div class='vmodel'>{Esc(request.VehicleModel)}</div>
                    <div class='vmeta'>Seats: {request.VehicleSeating} &nbsp;|&nbsp; Air-Conditioned: {Esc(request.VehicleAirCon)}</div>
                </div>
            </div>";
        }

        private string BuildDaysHtml(List<DayPlan> days)
        {
            if (days.Count == 0) return "";

            var pages = new List<string>();
            var lastDayNumber = days.Max(d => d.DayNumber);

            foreach (var day in days)
            {
                var descItems = day.ItineraryDescription
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line =>
                    {
                        var text = line.TrimStart('-', ' ', '•');
                        return $"<li>{Esc(text.Trim())}</li>";
                    });

                // No images on the last (departure) day
                var isLastDay = day.DayNumber == lastDayNumber;
                var dayImages = isLastDay
                    ? new List<string>()
                    : GetRandomFromList(day.LocationImages, 3)
                        .Select(p => ToBase64Url(p))
                        .Where(d => !string.IsNullOrEmpty(d))
                        .ToList();

                var imagesHtml = dayImages.Count > 0
                    ? $"<div class='day-image-grid'>{string.Join("\n", dayImages.Select(b64 => $"<img src='{b64}' />"))}</div>"
                    : "";

                var dayTitle = !string.IsNullOrEmpty(day.LocationName) ? day.LocationName
                    : !string.IsNullOrEmpty(day.HotelLocation) ? day.HotelLocation
                    : $"Day {day.DayNumber}";

                var dayBlock = $@"
                <div class='page'>
                    <div class='page-bg'></div>
                    <div class='page-content'>

                        <div class='day-title'>Day {day.DayNumber} &mdash; {Esc(dayTitle)}</div>
                        <div class='day-text'>
                            <ul>{string.Join("\n", descItems)}</ul>
                        </div>

                        <div class='day-meta'>
                            {(!string.IsNullOrEmpty(day.Highlights) ? $"<p><strong>Highlights:</strong> {Esc(day.Highlights)}</p>" : "")}
                            <p><strong>Meals:</strong> {Esc(day.Meals)}</p>
                            {(!string.IsNullOrEmpty(day.HotelName) && day.HotelName != "N/A"
                                ? $"<p><strong>Overnight:</strong> {Esc(day.HotelName)} {(!string.IsNullOrEmpty(day.HotelStarRating) ? $"({Esc(day.HotelStarRating)} Star)" : "")}</p>"
                                : "")}
                        </div>

                        {imagesHtml}
                    </div>
                </div>";

                pages.Add(dayBlock);
            }

            return string.Join("\n", pages);
        }

        private string BuildOptionTablePage(PackageOption option, string currencyCode, string optionTitle)
        {
            var rows = option.Hotels.Select(h => $@"
                <tr>
                    <td>{Esc(h.HotelLocation)}</td>
                    <td>{(!string.IsNullOrEmpty(h.HotelStarRating) ? $"{Esc(h.HotelStarRating)} Star" : "-")}</td>
                    <td><strong>{Esc(h.HotelName)}</strong></td>
                    <td>{Esc(h.FoodType)}</td>
                </tr>
            ").ToList();

            var costHtml = option.Cost > 0
                ? $@"<div class='cost-banner'>
                        <div><strong>Total Package Cost ({optionTitle}):</strong> {Esc(currencyCode)} {option.Cost:N2}</div>
                    </div>" : "";

            return $@"
                <div class='page'>
                    <div class='page-bg'></div>
                    <div class='page-content'>
                        <h2 class='acc-title'>Accommodation Details - {Esc(option.Title)}</h2>
                        
                        <table class='acc-table'>
                            <thead>
                                <tr>
                                    <th>Location</th>
                                    <th>Star Class</th>
                                    <th>Hotel Name</th>
                                    <th>Meal Plan</th>
                                </tr>
                            </thead>
                            <tbody>
                                {string.Join("\n", rows)}
                            </tbody>
                        </table>
                        
                        {costHtml}
                    </div>
                </div>";
        }

        // Convert a web-relative path like /images/locations/x/y.jpg to a base64 data URI
        private string ToBase64Url(string webRelPath)
        {
            if (string.IsNullOrEmpty(webRelPath)) return "";
            var clean = webRelPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, clean);
            return GetBase64Image(fullPath);
        }

        private string GetBase64Image(string path)
        {
            if (File.Exists(path))
            {
                var ext = Path.GetExtension(path).TrimStart('.').ToLower();
                if (ext == "jpg") ext = "jpeg";
                return $"data:image/{ext};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}";
            }
            return "";
        }

        private static List<string> GetRandomFromList(List<string>? urls, int count)
        {
            if (urls == null || urls.Count == 0) return new();
            var picked = urls.OrderBy(_ => Guid.NewGuid()).Take(count).ToList();
            while (picked.Count > 0 && picked.Count < count) {
                 picked.Add(picked[0]); 
            }
            return picked;
        }

        private static string Esc(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                       .Replace("\"", "&quot;").Replace("'", "&#39;");
        }
    }
}