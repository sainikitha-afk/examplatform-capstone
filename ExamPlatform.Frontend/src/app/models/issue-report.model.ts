// issue-report.model.ts — shapes for the candidate "flag / report a problem"
// feature and the examiner triage inbox. Classes with public constructor params
// and defaults (mentor style), so `new IssueReportModel()` is always valid.

// One report row as returned by the API.
export class IssueReportModel {
  constructor(
    public issueReportId: number = 0,
    public examId: number = 0,
    public examTitle: string = '',
    public attemptId: number | null = null,
    public questionId: number | null = null,
    public candidateName: string | null = null,   // examiner view only
    public questionText: string | null = null,     // examiner view only
    public message: string = '',
    public status: string = 'Open',                // Open | Reviewed | Resolved
    public resolutionNote: string | null = null,
    public createdAt: string = '',
    public resolvedAt: string | null = null
  ) {}
}

// Request body a candidate sends when flagging a problem.
export class CreateIssueReportModel {
  constructor(
    public examId: number = 0,
    public attemptId: number | null = null,
    public questionId: number | null = null,
    public message: string = ''
  ) {}
}
