using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.Playwright;
using PdfGeneratorApi.Models;
// Alias for Position enum to avoid naming conflicts
using Position = PdfGeneratorApi.Models.Position;

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

    // Support file uploads in Swagger
    c.OperationFilter<FileUploadOperationFilter>();
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
    [FromForm] PdfRequest request) => // Bind form data to PdfRequest model
{
    var browser = await browserTask;
    var context = await browser.NewContextAsync();
    var page = await context.NewPageAsync();

    // Load the page content
    if (!string.IsNullOrEmpty(request.Url))
    {
        // Navigate to the provided URL
        await page.GotoAsync(request.Url);
    }
    else if (!string.IsNullOrEmpty(request.HtmlContent))
    {
        // Set the HTML content directly
        await page.SetContentAsync(request.HtmlContent);
    }
    else
    {
        // Return a bad request response if neither URL nor HTML content is provided
        return Results.BadRequest("A URL or HTML content must be provided");
    }

    // Hide specified elements based on CSS selectors
    if (request.HideSelectors != null && request.HideSelectors.Any())
    {
        foreach (var selector in request.HideSelectors)
        {
            // Execute JavaScript to hide elements matching the selector
            await page.EvaluateAsync($"() => {{ const elements = document.querySelectorAll('{selector}'); elements.forEach(el => el.style.display = 'none'); }}");
        }
    }

    // Add watermark if specified
    if (!string.IsNullOrEmpty(request.WatermarkText) || request.WatermarkImageFile != null || !string.IsNullOrEmpty(request.WatermarkImageUrl))
    {
        // Generate CSS for the watermark
        var watermarkCss = await GenerateWatermarkCssAsync(request);
        // Inject the CSS into the page
        await page.AddStyleTagAsync(new() { Content = watermarkCss });
    }

    // Add stamp if specified
    if (request.StampImageFile != null || !string.IsNullOrEmpty(request.StampImageUrl))
    {
        // Generate CSS for the stamp
        var stampCss = await GenerateStampCssAsync(request);
        // Inject the CSS into the page
        await page.AddStyleTagAsync(new() { Content = stampCss });
    }

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
async Task<string> GenerateWatermarkCssAsync(PdfRequest request)
{
    var position = request.WatermarkPosition ?? Position.Center;
    var opacity = 0.2; // Set a reasonable opacity

    var positionCss = GetPositionCss(position);

    if (!string.IsNullOrEmpty(request.WatermarkText))
    {
        // Create CSS for text watermark
        return $@"
            body::after {{
                content: '{request.WatermarkText}';
                position: fixed;
                {positionCss}
                font-size: 50px;
                color: rgba(0, 0, 0, {opacity});
                pointer-events: none;
                z-index: 9999;
                transform: rotate(-45deg);
            }}";
    }
    else if (request.WatermarkImageFile != null || !string.IsNullOrEmpty(request.WatermarkImageUrl))
    {
        // Get the image URL (either from the uploaded file or the provided URL)
        var imageUrl = await GetImageUrlAsync(request.WatermarkImageFile, request.WatermarkImageUrl);

        // Create CSS for image watermark
        return $@"
            body::after {{
                content: '';
                position: fixed;
                {positionCss}
                width: 100%;
                height: 100%;
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
async Task<string> GenerateStampCssAsync(PdfRequest request)
{
    var position = request.StampPosition ?? Position.RightBottom;
    // Get the image URL (either from the uploaded file or the provided URL)
    var imageUrl = await GetImageUrlAsync(request.StampImageFile, request.StampImageUrl);
    var positionCss = GetPositionCss(position);

    // Create CSS for the stamp image
    return $@"
        body::before {{
            content: '';
            position: fixed;
            {positionCss}
            width: 100px;
            height: 100px;
            background: url('{imageUrl}') no-repeat center center;
            background-size: contain;
            opacity: 1;
            pointer-events: none;
        }}";
}

// Convert uploaded image file to Base64 data URL or use the provided image URL
async Task<string> GetImageUrlAsync(IFormFile imageFile, string imageUrl)
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
    else if (!string.IsNullOrEmpty(imageUrl))
    {
        // Use the provided image URL
        return imageUrl;
    }

    return string.Empty; // Return empty string if no image is provided
}

// Get CSS positioning based on the Position enum
string GetPositionCss(Position position)
{
    return position switch
    {
        Position.LeftTop => "top: 0; left: 0;",
        Position.LeftBottom => "bottom: 0; left: 0;",
        Position.RightTop => "top: 0; right: 0;",
        Position.RightBottom => "bottom: 0; right: 0;",
        Position.Center => "top: 50%; left: 50%; transform: translate(-50%, -50%);",
        _ => "bottom: 0; right: 0;", // Default to right bottom if position is unspecified
    };
}
