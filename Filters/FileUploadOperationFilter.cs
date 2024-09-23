using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isMultipart = context.MethodInfo.CustomAttributes.Any(a => a.AttributeType == typeof(FromFormAttribute));

        if (isMultipart)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(
                                context.MethodInfo.GetParameters()
                                    .First(p => p.GetCustomAttributes(typeof(FromFormAttribute), false).Any())
                                    .ParameterType,
                                context.SchemaRepository)
                        }
                    }
            };
        }
    }
}