using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.Playwright;


//Microsoft.Playwright.Program.Main(["install"]);
//return;


var builder = WebApplication.CreateBuilder(args);

// Add Playwright service as a singleton
builder.Services.AddSingleton(async _ =>
{
    var playwright = await Playwright.CreateAsync();
    // Launch Chromium browser in headless mode
    return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
});

// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Add API Key authentication definition
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key Authentication",
        Name = "X-API-KEY", // Header name
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    // Require API Key authentication globally
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" },
                Scheme = "ApiKeyScheme",
                Name = "X-API-KEY",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

});

var app = builder.Build();

// Use Swagger UI in development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Simple API Key authentication middleware
app.Use(async (context, next) =>
{
    // Check if the X-API-KEY header is present
    if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
    {
        context.Response.StatusCode = 401; // Unauthorized
        await context.Response.WriteAsync("API Key is missing");
        return;
    }

    // Get the API Key from environment variables
    var apiKey = Environment.GetEnvironmentVariable("API_KEY");
    if (string.IsNullOrEmpty(apiKey) || !apiKey.Equals(extractedApiKey))
    {
        context.Response.StatusCode = 401; // Unauthorized
        await context.Response.WriteAsync("Unauthorized client");
        return;
    }

    await next(); // Proceed to the next middleware/component
});

// Define the API endpoint for generating PDFs
app.MapPost("/generate-pdf", async (
    [FromServices] Task<IBrowser> browserTask, // Inject the Playwright browser
     string? Url,                    // URL of the page to generate PDF from
     string? HtmlContent,            // HTML content to render
     string? HideSelectors,    // CSS selectors to hide elements
     string? WatermarkText,          // Text for watermark
     IFormFile? WatermarkImageFile,  // Image file for watermark
     IFormFile? StampImageFile       // Image file for stamp
                                        ) => // Bind form data to PdfRequest model
{
    var browser = await browserTask;
    var context = await browser.NewContextAsync();
    var page = await context.NewPageAsync();

    // Load the page content
    if (!string.IsNullOrEmpty(Url))
    {
        // Navigate to the provided URL
        await page.GotoAsync(Url);
    }
    else if (!string.IsNullOrEmpty(HtmlContent))
    {
        // Set the HTML content directly
        await page.SetContentAsync(HtmlContent);
    }
    else
    {
        // Return a bad request response if neither URL nor HTML content is provided
        return Results.BadRequest("A URL or HTML content must be provided");
    }

    // Hide specified elements based on CSS selectors
    var hideSelectorsList = HideSelectors?
    .Split(new[] { ',', ';','|' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Trim())
    .ToList();
    if (hideSelectorsList != null && hideSelectorsList.Any())
    {
        foreach (var selector in hideSelectorsList)
        {
            // Execute JavaScript to hide elements matching the selector
            await page.EvaluateAsync($"() => {{ const elements = document.querySelectorAll('{selector}'); elements.forEach(el => el.style.display = 'none'); }}");
        }
    }

    // Add watermark if specified
    if (!string.IsNullOrEmpty(WatermarkText) || WatermarkImageFile != null)
    {
        // Generate CSS for the watermark
        var watermarkCss = await GenerateWatermarkCssAsync(WatermarkText, WatermarkImageFile);
        // Inject the CSS into the page
        await page.AddStyleTagAsync(new() { Content = watermarkCss });
    }

    // Add stamp if specified
    if (StampImageFile != null)
    {
        // Generate CSS for the stamp
        var stampCss = await GenerateStampCssAsync(StampImageFile);
        // Inject the CSS into the page
        await page.AddStyleTagAsync(new() { Content = stampCss });
    }

    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

    // Generate the PDF with specified options
    var pdfStream = await page.PdfAsync(new PagePdfOptions
    {
        Format = PaperFormat.A4,
        PrintBackground = true,
        Margin = new() { Top = "1cm", Bottom = "1cm", Left = "1cm", Right = "1cm" },
        Landscape = false
    });

    // Return the generated PDF file
    return Results.File(pdfStream, "application/pdf", "generated.pdf");
})
    .DisableAntiforgery(); // Disable anti-forgery token validation for this endpoint

app.Run();

// Helper methods

// Generate CSS for the watermark
async Task<string> GenerateWatermarkCssAsync(string watermark,IFormFile? image)
{

    var opacity = 0.2; // Set a reasonable opacity
    if (!string.IsNullOrEmpty(watermark))
    {
        // Create CSS for text watermark
        return $@"
            body::after {{
                content: '{watermark}';
                position: fixed;
                top: 50%; 
                left: 50%; 
                transform: translate(-50%, -50%);
                font-size: 5rem;
                font-weight: 800;
                color: rgba(0, 0, 0, {opacity});
                pointer-events: none;
                z-index: 9999;
                transform: rotate(-45deg);
            }}";
    }
    else if (image != null)
    {
        // Get the image URL (either from the uploaded file or the provided URL)
        var imageUrl = await GetImageUrlAsync(image);

        // Create CSS for image watermark
        return $@"
            body::after {{
                content: '';
                position: fixed;
                width: 100%;
                height: 100%;
                top: 0; left: 0;
                background: url('{imageUrl}') no-repeat center center;
                background-size: contain;
                opacity: {opacity};
                transform: rotate(-45deg);
                pointer-events: none;
            }}";
    }

    return string.Empty; // Return empty string if no watermark is specified
}

// Generate CSS for the stamp
async Task<string> GenerateStampCssAsync( IFormFile? image)
{

    // Get the image URL (either from the uploaded file or the provided URL)
    var imageUrl = await GetImageUrlAsync(image);
    // Create CSS for the stamp image
    return $@"
        body::before {{
            content: '';
            position: fixed;
            bottom: 0; 
            right: 0;
            width: 180px;
            height: 180px;
            background: url('{imageUrl}') no-repeat center center;
            background-size: contain;
            opacity: 1;
            pointer-events: none;
        }}";
}

// Convert uploaded image file to Base64 data URL or use the provided image URL
async Task<string> GetImageUrlAsync(IFormFile imageFile)
{
    if (imageFile != null)
    {
        using var memoryStream = new MemoryStream();
        await imageFile.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(bytes);
        var contentType = imageFile.ContentType;
        // Return the image as a Base64 data URL
        return $"data:{contentType};base64,{base64}";
    }
    return string.Empty; // Return empty string if no image is provided
}

