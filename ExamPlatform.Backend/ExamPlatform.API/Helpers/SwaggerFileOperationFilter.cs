// SwaggerFileOperationFilter — previously needed to manually build the multipart/form-data
// schema for IFormFile parameters. As of the ExcelImportDto wrapper fix (Task 1B), IFormFile
// is now a PROPERTY on a model class (ExcelImportDto), not a bare [FromForm] parameter.
//
// Swashbuckle 10.x can auto-generate the multipart schema for an IFormFile class property —
// no filter intervention required. This class is kept as a registered (but empty) no-op so
// Program.cs does not need changes and the git history explains the rationale.
//
// If a future endpoint adds a raw [FromForm] IFormFile parameter again, implement the filter
// body here (see git history for the previous implementation approach).
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi;

namespace ExamPlatform.API.Helpers
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        // No-op: Swashbuckle 10.x handles IFormFile properties on DTO classes automatically.
        // The multipart/form-data schema is generated correctly without manual intervention
        // once the parameter is wrapped in a model class (ExcelImportDto.File).
        public void Apply(OpenApiOperation operation, OperationFilterContext context) { }
    }
}
