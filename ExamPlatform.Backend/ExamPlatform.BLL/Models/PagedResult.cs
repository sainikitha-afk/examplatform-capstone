// PagedResult<T> — universal pagination envelope returned by BLL paginated service methods.
// The API layer maps this to PagedResultDto<TDto> (controller-level DTO) so the response JSON
// includes both the data slice and the pagination metadata in one body.
namespace ExamPlatform.BLL.Models
{
    // Generic over T so the same class works for User, Exam, Question, Certificate, etc.
    public class PagedResult<T>
    {
        // The current page's data slice. Empty enumerable — never null.
        public IEnumerable<T> Items { get; set; } = new List<T>();

        // 1-based page number the caller requested.
        public int Page { get; set; }

        // Number of items per page, clamped to [1, 100] by the service.
        public int PageSize { get; set; }

        // Total rows matching the query BEFORE Skip/Take — needed by clients
        // to render "Page 2 of 7" indicators.
        public int TotalCount { get; set; }

        // Derived: how many pages exist? Ceiling division so partial last pages are counted.
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        // Navigation helpers — clients use these to show/hide Next/Previous buttons.
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
