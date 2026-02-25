using NineArchTours.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;

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
            // Load pdfbg.png as base64
            var bgPath = Path.Combine(_env.WebRootPath, "assets", "pdfbg.png");
            var bgBase64 = "";
            if (File.Exists(bgPath))
            {
                bgBase64 = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(bgPath))}";
            }

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

            // Build accommodation option pages
            var accommodationHtml = "";
            var hasOpt1 = request.Option1 != null && request.Option1.Hotels.Count > 0;
            var hasOpt2 = request.Option2 != null && request.Option2.Hotels.Count > 0;

            if (hasOpt1)
            {
                accommodationHtml += BuildOptionPage(request.Option1!, request.CurrencyCode, bgBase64);
                if (hasOpt2)
                    accommodationHtml += BuildOptionPage(request.Option2!, request.CurrencyCode, bgBase64);
            }

            // Vehicle section - from the request data directly since we're using SQL for data
            var vehicleHtml = "";

            // Cost section
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
        body {{ font-family:'Open Sans','Segoe UI',sans-serif; color:#333; font-size:11pt; line-height:1.5; }}

        .page {{
            width:210mm; min-height:297mm; position:relative;
            page-break-after:always; overflow:hidden;
        }}
        .page-bg {{
            position:absolute; top:0; left:0; width:100%; height:100%; z-index:0;
            {(string.IsNullOrEmpty(bgBase64) ? "" : $"background-image:url('{bgBase64}');")}
            background-size:cover; background-position:center; background-repeat:no-repeat;
        }}
        .page-content {{
            position:relative; z-index:1;
            padding:110px 50px 130px 50px; min-height:297mm;
        }}

        /* COVER */
        .cover-content {{
            position:relative; z-index:1;
            display:flex; flex-direction:column; align-items:center; justify-content:center;
            min-height:297mm; text-align:center; padding:120px 60px 140px 60px;
        }}
        .tour-badge {{
            background:linear-gradient(135deg,#0066cc,#004499); color:white;
            padding:8px 30px; border-radius:25px; font-size:10pt;
            font-weight:600; letter-spacing:2px; text-transform:uppercase; margin-bottom:30px;
        }}
        .cover-content h1 {{ font-size:28pt; font-weight:700; color:#0a1628; margin-bottom:8px; line-height:1.2; }}
        .cover-content .subtitle {{ font-size:14pt; color:#0066cc; font-weight:600; margin-bottom:25px; }}
        .cover-content .route-text {{ font-size:11pt; color:#555; margin-bottom:30px; }}
        .cover-divider {{ width:80px; height:3px; background:linear-gradient(90deg,#0066cc,#00aaff); margin:0 auto 30px; border-radius:2px; }}
        .cover-details {{ display:flex; flex-wrap:wrap; justify-content:center; gap:20px; margin-top:10px; }}
        .cover-detail-item {{
            background:#f0f7ff; border:1px solid #d0e3f7; border-radius:10px;
            padding:14px 22px; min-width:140px; text-align:center;
        }}
        .cover-detail-item .detail-label {{ font-size:8pt; color:#777; text-transform:uppercase; letter-spacing:1px; font-weight:600; }}
        .cover-detail-item .detail-value {{ font-size:11pt; color:#0a1628; font-weight:700; margin-top:4px; }}
        .cover-client {{ margin-top:35px; font-size:12pt; color:#333; }}
        .cover-client strong {{ color:#0066cc; }}

        /* DAY BLOCKS */
        .day-block {{ margin-bottom:30px; page-break-inside:avoid; }}
        .day-header {{
            background:linear-gradient(135deg,#0066cc,#004499); color:white;
            padding:10px 20px; border-radius:8px 8px 0 0; font-size:13pt; font-weight:700;
        }}
        .day-body {{
            display:flex; gap:20px; border:1px solid #e0e0e0; border-top:none;
            border-radius:0 0 8px 8px; padding:18px; background:rgba(255,255,255,0.85);
        }}
        .day-images {{
            flex:0 0 195px; display:flex; flex-direction:column; gap:3px;
            height:155px; border-radius:8px; overflow:hidden;
        }}
        .day-images img {{ width:100%; flex:1; min-height:0; object-fit:cover; }}
        .day-text {{ flex:1; }}
        .day-text ul {{ list-style:none; padding:0; margin:0 0 10px 0; }}
        .day-text ul li {{ padding:2px 0 2px 18px; position:relative; font-size:9.5pt; line-height:1.6; }}
        .day-text ul li::before {{ content:'\2022'; position:absolute; left:0; color:#0066cc; font-weight:bold; font-size:12pt; line-height:1.3; }}
        .day-highlights {{ font-size:9pt; color:#0066cc; font-weight:600; margin-top:6px; }}
        .day-meals {{ font-size:9pt; color:#666; margin-top:4px; }}
        .day-hotel {{ margin-top:8px; padding:8px 12px; background:#f5f9ff; border-left:3px solid #0066cc; border-radius:4px; font-size:9pt; }}
        .day-hotel strong {{ color:#0a1628; }}
        .day-hotel .star {{ color:#f5a623; }}

        /* ACCOMMODATION / OPTIONS */
        .accommodation-title {{ font-size:16pt; font-weight:700; color:#0a1628; margin-bottom:6px; text-align:center; }}
        .option-badge {{
            display:inline-block; background:linear-gradient(135deg,#0066cc,#004499); color:white;
            padding:4px 18px; border-radius:20px; font-size:9pt; font-weight:600;
            letter-spacing:1px; margin-bottom:16px;
        }}
        .hotel-card {{
            display:flex; gap:0; margin-bottom:18px; border:1px solid #e0e0e0;
            border-radius:10px; overflow:hidden; background:rgba(255,255,255,0.9);
            page-break-inside:avoid;
        }}
        .hotel-card-images {{
            flex:0 0 180px; display:flex; flex-direction:column; min-height:130px;
            gap:2px; overflow:hidden;
        }}
        .hotel-card-images .hotel-img-main {{ width:180px; height:80px; object-fit:cover; display:block; }}
        .hotel-img-row {{ display:flex; gap:2px; flex:1; }}
        .hotel-img-row img {{ flex:1; width:89px; height:46px; object-fit:cover; display:block; }}
        .hotel-card-info {{ flex:1; padding:14px 16px; }}
        .hotel-card-info h3 {{ font-size:11pt; color:#0a1628; margin-bottom:2px; }}
        .hotel-card-info .hotel-meta {{ font-size:8.5pt; color:#777; margin-bottom:6px; }}
        .hotel-card-info .hotel-meta .star {{ color:#f5a623; }}
        .hotel-card-info p {{ font-size:9pt; color:#555; line-height:1.5; }}
        .hotel-card-info .hotel-food {{ margin-top:5px; font-size:8.5pt; color:#666; }}

        /* VEHICLE & INCLUSIONS */
        .vehicle-section {{ margin-bottom:25px; }}
        .vehicle-section h2, .inclusions-section h2 {{
            font-size:14pt; color:#0a1628; margin-bottom:12px;
            padding-bottom:6px; border-bottom:2px solid #0066cc;
        }}
        .vehicle-table {{ width:100%; border-collapse:collapse; background:rgba(255,255,255,0.9); border-radius:8px; overflow:hidden; }}
        .vehicle-table td {{ padding:8px 14px; font-size:10pt; border-bottom:1px solid #eee; }}
        .vehicle-table td.label {{ font-weight:600; color:#333; width:220px; }}
        .inclusions-section {{ margin-bottom:25px; }}
        .two-col {{ display:flex; gap:30px; }}
        .two-col > div {{ flex:1; }}
        .two-col h3 {{ font-size:11pt; color:#0066cc; margin-bottom:8px; }}
        .two-col ul {{ list-style:none; padding:0; }}
        .two-col ul li {{ padding:3px 0 3px 18px; position:relative; font-size:9.5pt; line-height:1.6; }}
        .inclusions-list li::before {{ content:'\2713'; position:absolute; left:0; color:#28a745; font-weight:bold; }}
        .exclusions-list li::before {{ content:'\2717'; position:absolute; left:0; color:#dc3545; font-weight:bold; }}
        .cost-banner {{
            background:linear-gradient(135deg,#0066cc,#004499); color:white;
            padding:18px 30px; border-radius:10px; text-align:center; margin-top:25px;
        }}
        .cost-banner .cost-label {{ font-size:10pt; font-weight:400; opacity:0.9; }}
        .cost-banner .cost-value {{ font-size:24pt; font-weight:700; margin-top:4px; }}
        .footer-note {{ margin-top:25px; text-align:center; font-size:8pt; color:#999; }}

        @media print {{ .page {{ page-break-after:always; }} }}
    </style>
</head>
<body>

    <!-- PAGE 1: COVER -->
    <div class='page'>
        <div class='page-bg'></div>
        <div class='cover-content'>
            <div class='tour-badge'>Personalized Luxury Tour</div>
            <h1>Sri Lanka Tour Itinerary</h1>
            <div class='subtitle'>{Esc(request.TourTitle)}</div>
            <div class='cover-divider'></div>
            <div class='route-text'>Route: {Esc(routeSummary)}</div>
            <div class='cover-details'>
                <div class='cover-detail-item'>
                    <div class='detail-label'>Duration</div>
                    <div class='detail-value'>{nightsLabel}</div>
                </div>
                <div class='cover-detail-item'>
                    <div class='detail-label'>Hotel Category</div>
                    <div class='detail-value'>{Esc(request.HotelCategory)}</div>
                </div>
                <div class='cover-detail-item'>
                    <div class='detail-label'>Meal Plan</div>
                    <div class='detail-value'>{Esc(request.MealPlan)}</div>
                </div>
                <div class='cover-detail-item'>
                    <div class='detail-label'>Arrival</div>
                    <div class='detail-value'>{Esc(request.ArrivalDate)}</div>
                </div>
            </div>
            <div class='cover-client'>
                Prepared for <strong>{Esc(request.ClientName)}</strong>
            </div>
        </div>
    </div>

    <!-- ITINERARY PAGES -->
    {daysHtml}

    <!-- ACCOMMODATION / OPTION PAGES -->
    {accommodationHtml}

    <!-- LAST PAGE -->
    <div class='page'>
        <div class='page-bg'></div>
        <div class='page-content'>
            {vehicleHtml}
            <div class='inclusions-section'>
                <h2>Tour Package Details</h2>
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
            </div>
            {costHtml}
            <div class='footer-note'>
                This quotation is valid for 7 days from the date of issue.<br/>
                9 Arch Travels &mdash; Your Gateway to Sri Lanka
            </div>
        </div>
    </div>

</body>
</html>";
        }

        private string BuildDaysHtml(List<DayPlan> days)
        {
            if (days.Count == 0) return "";

            var pages = new List<string>();
            var currentPageDays = new List<string>();
            int daysOnPage = 0;

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

                var hotelHtml = !string.IsNullOrEmpty(day.HotelName) && day.HotelName != "N/A"
                    ? $@"<div class='day-hotel'>
                            <strong>Overnight:</strong> {Esc(day.HotelName)}
                            {(!string.IsNullOrEmpty(day.HotelStarRating) ? $"<span class='star'> ★ {Esc(day.HotelStarRating)}</span>" : "")}
                            {(!string.IsNullOrEmpty(day.HotelLocation) ? $" &mdash; {Esc(day.HotelLocation)}" : "")}
                        </div>" : "";

                // Use location images (3 random) for day display
                var dayImages = GetRandomFromList(day.LocationImages, 3);
                var imagesHtml = dayImages.Count > 0
                    ? $"<div class='day-images'>{string.Join("\n", dayImages.Select(u => $"<img src='{u}' alt='Day {day.DayNumber}' />"))}</div>"
                    : "";

                var dayTitle = !string.IsNullOrEmpty(day.LocationName) ? day.LocationName
                    : !string.IsNullOrEmpty(day.HotelLocation) ? day.HotelLocation
                    : $"Day {day.DayNumber}";

                var dayBlock = $@"
                    <div class='day-block'>
                        <div class='day-header'>Day {day.DayNumber} &mdash; {Esc(dayTitle)}</div>
                        <div class='day-body'>
                            {imagesHtml}
                            <div class='day-text'>
                                <ul>{string.Join("\n", descItems)}</ul>
                                {(!string.IsNullOrEmpty(day.Highlights) ? $"<div class='day-highlights'>Highlights: {Esc(day.Highlights)}</div>" : "")}
                                <div class='day-meals'>Meals: {Esc(day.Meals)}</div>
                                {hotelHtml}
                            </div>
                        </div>
                    </div>";

                currentPageDays.Add(dayBlock);
                daysOnPage++;

                if (daysOnPage >= 2)
                {
                    pages.Add(WrapPage(string.Join("\n", currentPageDays)));
                    currentPageDays.Clear();
                    daysOnPage = 0;
                }
            }

            if (currentPageDays.Count > 0)
                pages.Add(WrapPage(string.Join("\n", currentPageDays)));

            return string.Join("\n", pages);
        }

        private string BuildOptionPage(PackageOption option, string currencyCode, string bgBase64)
        {
            var cards = option.Hotels.Select(h =>
            {
                var imagesHtml = BuildHotelImagesHtml(h.HotelImages);
                return $@"
                <div class='hotel-card'>
                    {imagesHtml}
                    <div class='hotel-card-info'>
                        <h3>{Esc(h.HotelName)} &mdash; {Esc(h.HotelLocation)}</h3>
                        <div class='hotel-meta'>
                            {(!string.IsNullOrEmpty(h.HotelStarRating) ? $"<span class='star'>★</span> {Esc(h.HotelStarRating)}" : "")}
                        </div>
                        <p>{Esc(h.HotelDescription)}</p>
                        {(!string.IsNullOrEmpty(h.FoodType) ? $"<div class='hotel-food'>Meals: {Esc(h.FoodType)}</div>" : "")}
                    </div>
                </div>";
            }).ToList();

            var pages = new List<string>();
            for (int i = 0; i < cards.Count; i += 3)
            {
                var batch = cards.Skip(i).Take(3);
                var isFirst = i == 0;

                var header = isFirst
                    ? $@"<div style='text-align:center;margin-bottom:20px;'>
                            <div class='option-badge'>{Esc(option.Title)}</div>
                            <h2 class='accommodation-title'>Accommodation Options</h2>
                        </div>"
                    : $@"<div style='text-align:center;margin-bottom:20px;'>
                            <div class='option-badge'>{Esc(option.Title)} (Continued)</div>
                        </div>";

                var costHtml = (isFirst && option.Cost > 0)
                    ? $@"<div class='cost-banner'>
                            <div class='cost-label'>Package Cost for this Option</div>
                            <div class='cost-value'>{Esc(currencyCode)} {option.Cost:N2}</div>
                        </div>" : "";

                pages.Add($@"
                    <div class='page'>
                        <div class='page-bg'></div>
                        <div class='page-content'>
                            {header}
                            {string.Join("\n", batch)}
                            {costHtml}
                        </div>
                    </div>");
            }

            return string.Join("\n", pages);
        }

        private string BuildHotelImagesHtml(List<string> images)
        {
            var picked = GetRandomFromList(images, 3);
            if (picked.Count == 0) return "";

            if (picked.Count == 1)
                return $"<div class='hotel-card-images'><img class='hotel-img-main' src='{picked[0]}' style='height:100%;' /></div>";

            var main = picked[0];
            var thumbs = picked.Skip(1).Take(2).ToList();
            var thumbsHtml = string.Join("\n", thumbs.Select(u => $"<img src='{u}' />"));

            return $@"<div class='hotel-card-images'>
                        <img class='hotel-img-main' src='{main}' />
                        <div class='hotel-img-row'>{thumbsHtml}</div>
                    </div>";
        }

        private static List<string> GetRandomFromList(List<string>? urls, int count)
        {
            if (urls == null || urls.Count == 0) return new();
            return urls.OrderBy(_ => Guid.NewGuid()).Take(count).ToList();
        }

        private string WrapPage(string content) => $@"
            <div class='page'>
                <div class='page-bg'></div>
                <div class='page-content'>{content}</div>
            </div>";

        private static string Esc(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                       .Replace("\"", "&quot;").Replace("'", "&#39;");
        }
    }
}
