// AttemptResultModel — returned by GET /api/attempts/{id}/result.
// The score/pass-fail are available immediately after submission; the per-question
// breakdown (with correct answers) and the topic chart arrive only after the examiner
// publishes results — resultsPublished tells the screen which state to render.
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
    public chartUrl: string | null = null,
    public topicScoresUrl: string | null = null,
    // False until the examiner publishes: questions[] is empty and the topic chart
    // is unavailable — the screen shows a "review pending" notice instead.
    public resultsPublished: boolean = false,
    public questions: PerQuestionResultModel[] = []
  ) {}
}
