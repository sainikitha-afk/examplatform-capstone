// AttemptModel — frontend twins of attempt-related DTOs from the backend.
// Why it exists: the attempt-exam screen needs a typed object for the active attempt,
// and the history screen needs typed summaries.
// How Angular uses it: attemptapi.service.ts returns these; NgRx store holds AttemptResumeModel
// during an active attempt.
import { QuestionInAttemptModel } from './question.model';

// AttemptResumeModel — returned by POST /api/attempts/start and GET /api/attempts/{id}.
// Contains all questions (without answer keys) and the server-computed remaining seconds.
export class AttemptResumeModel {
  constructor(
    public attemptId: number = 0,
    public examId: number = 0,
    public examTitle: string = "",
    public startedAt: string = "",
    public remainingSeconds: number = 0,
    public attemptStatus: string = "",   // "InProgress" | "Submitted"
    public questions: QuestionInAttemptModel[] = []
  ) {}
}

// AttemptSummaryModel — one row in the attempt history list (GET /api/attempts/mine).
export class AttemptSummaryModel {
  constructor(
    public attemptId: number = 0,
    public examId: number = 0,
    public examTitle: string = "",
    public startedAt: string = "",
    public submittedAt: string | null = null,
    public attemptStatus: string = "",
    public score: number | null = null,
    public isPassed: boolean | null = null
  ) {}
}

// StartAttemptModel — request body for POST /api/attempts/start.
export class StartAttemptModel {
  constructor(
    public examId: number = 0
  ) {}
}

// SaveAnswerModel — request body for PUT /api/attempts/{id}/answers.
export class SaveAnswerModel {
  constructor(
    public questionId: number = 0,
    public selectedOptionId: number | null = null
  ) {}
}
