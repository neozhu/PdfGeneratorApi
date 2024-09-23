using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace PdfGeneratorApi.Models;

public class PdfRequest
{
    public string? Url { get; set; }
    public string? HtmlContent { get; set; }
    public List<string>? HideSelectors { get; set; }

    public string? WatermarkText { get; set; }
    public string? WatermarkImageUrl { get; set; }

    [SwaggerSchema(Format = "binary")]
    public IFormFile? WatermarkImageFile { get; set; }
    [EnumDataType(typeof(Position))]
    public Position? WatermarkPosition { get; set; }

    public string? StampImageUrl { get; set; }

    [SwaggerSchema(Format = "binary")]
    public IFormFile? StampImageFile { get; set; }
    [EnumDataType(typeof(Position))]
    public Position? StampPosition { get; set; }
}

