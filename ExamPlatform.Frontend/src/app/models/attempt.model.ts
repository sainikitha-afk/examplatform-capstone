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
    public isPassed: boolean | null = null,
    public chartUrl: string | null = null,
    public topicScoresUrl: string | null = null
  ) {}
}

// ExamAttemptMonitorModel — one row in the examiner's attempt-monitoring view
// (GET /api/exams/{id}/attempts). Shows who took the exam, live progress, and
// how each attempt ended. AutoSubmitted = the server timer closed the attempt,
// usually because the candidate disconnected and never returned.
export class ExamAttemptMonitorModel {
  constructor(
    public attemptId: number = 0,
    public userId: number = 0,
    public candidateName: string = "",
    public candidateEmail: string = "",
    public startedAt: string = "",
    public submittedAt: string | null = null,
    public attemptStatus: string = "",   // "InProgress" | "Submitted" | "AutoSubmitted"
    public score: number | null = null,
    public isPassed: boolean | null = null,
    public timeTakenSeconds: number | null = null,
    public answeredCount: number = 0,
    // Proctoring: times the candidate left the exam tab/window.
    public tabSwitchCount: number = 0,
    // AI topic analytics: local path to the generated topic-scores JSON.
    public topicScoresUrl: string | null = null
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
