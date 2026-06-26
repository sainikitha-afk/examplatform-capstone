// PagedResultModel — generic wrapper returned by all paginated list endpoints.
// Why it exists: every list API (users, exams, questions, attempts, etc.) returns this shape.
//   One generic model handles them all instead of repeating the pagination fields.
// How Angular uses it: services type their Observable as Observable<PagedResultModel<T>>;
//   components read .items for the rows and .totalPages / .hasNextPage for pager controls.
export class PagedResultModel<T> {
  constructor(
    public items: T[] = [],
    public page: number = 1,
    public pageSize: number = 10,
    public totalCount: number = 0,
    public totalPages: number = 0,
    public hasNextPage: boolean = false,
    public hasPreviousPage: boolean = false
  ) {}
}
