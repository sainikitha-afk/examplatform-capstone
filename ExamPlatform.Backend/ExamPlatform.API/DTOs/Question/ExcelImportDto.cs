// ExcelImportDto — wraps the IFormFile parameter for POST /api/questions/import.
//
// WHY a wrapper class instead of a bare [FromForm] IFormFile parameter?
// Swashbuckle 10.x crashes during swagger.json generation when IFormFile is used
// as a direct method parameter with [FromForm]. The error is:
//   "Error reading parameter(s) … as [FromForm] attribute used with IFormFile"
//
// Wrapping IFormFile in a class property lets Swashbuckle inspect the property
// type and correctly generate the multipart/form-data request body schema,
// which in turn renders the "Choose file" button in Swagger UI.
using System.ComponentModel.DataAnnotations;

namespace ExamPlatform.API.DTOs.Question
{
    public class ExcelImportDto
    {
        // The uploaded .xlsx file. [Required] causes ASP.NET to return 400
        // before the action body executes if the form field is missing.
        [Required(ErrorMessage = "An Excel file (.xlsx) is required.")]
        public IFormFile File { get; set; } = null!;
    }
}
