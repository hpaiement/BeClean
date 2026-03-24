using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi;

namespace BeClean.Api.Swagger
{
    /// <summary>
    /// Operation filter to add the requirement of the custom header for swagger
    /// </summary>
    public class LanguageHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<IOpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Accept-Language",
                In = ParameterLocation.Header,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                Required = false // set to false if this is optional
            });
        }
    }
}
