// PagedResultDto<T> — the JSON envelope returned by every paginated GET endpoint.
// Controllers map BLL.Models.PagedResult<TEntity> → PagedResultDto<TDto> so the
// wire format contains DTOs (not domain entities) alongside the paging metadata.
//
// Computed properties (TotalPages, HasNextPage, HasPreviousPage) serialize
// automatically because they are public getters — no extra mapping needed.
namespace ExamPlatform.API.DTOs.Common
{
    // Generic over T so the same wrapper works for every resource type
    // (UserSummaryDto, ExamSummaryDto, QuestionDto, etc.).
    public class PagedResultDto<T>
    {
        // The data slice for the current page. Never null.
        public IEnumerable<T> Items { get; set; } = new List<T>();

        // 1-based page number that was returned.
        public int Page { get; set; }

        // Number of items per page that was used (after clamping to [1, 100]).
        public int PageSize { get; set; }

        // Total rows matching the query BEFORE Skip/Take — needed to render
        // "Page 2 of 7" indicators and calculate whether a next page exists.
        public int TotalCount { get; set; }

        // Ceiling division — how many pages are there in total?
        public int TotalPages { get; set; }

        // Navigation helpers for the client's Previous / Next buttons.
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
