// AttemptResultModel — returned by GET /api/attempts/{id}/result after results are published.
// Why it exists: the exam-result screen needs typed per-question breakdown data.
// How Angular uses it: exam-result component gets the attemptId from the route param and
// fetches this model; *ngFor renders the per-question table.

// PerQuestionResultModel — one row in the result breakdown.
export class PerQuestionResultModel {
  constructor(
    public questionId: number = 0,
    public questionText: string = "",
    public selectedOptionText: string | null = null,
    public correctOptionText: string | null = null,
    public marksAwarded: number | null = null,
    public isCorrect: boolean | null = null
  ) {}
}

// AttemptResultModel — top-level result object.
export class AttemptResultModel {
  constructor(
    public attemptId: number = 0,
    public examId: number = 0,
    public examTitle: string = "",
    public score: number | null = null,
    public passingMarks: number = 0,
    public isPassed: boolean | null = null,
    public timeTakenSeconds: number | null = null,
    public questions: PerQuestionResultModel[] = []
  ) {}
}
