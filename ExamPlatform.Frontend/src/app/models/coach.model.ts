// CoachModel — frontend twins of the AI Learning Coach DTOs.
// Why it exists: the /ai-coach screen renders structured coaching (strengths, focus areas,
//   a study plan) rather than a wall of chat text, so it needs typed shapes to bind to.
// How Angular uses it: coachapi.service.ts returns these; ai-coach.ts binds to them.
//
// Note what these do NOT carry: no user id, no attempt id, no exam id, no topicScoresUrl.
// The backend never sends internal identifiers or file paths to the browser for this feature.

// One topic and the percentage the BACKEND computed for it (never the AI's arithmetic).
export class CoachTopicModel {
  constructor(
    public topic: string = '',
    public score: number = 0            // percentage, e.g. 85
  ) {}
}

export type CoachPriority = 'High' | 'Medium' | 'Low';

// A weak topic worth attention. topic/score/priority are backend-computed; only the reason
// is AI-written prose.
export class CoachFocusAreaModel {
  constructor(
    public topic: string = '',
    public score: number = 0,
    public priority: CoachPriority = 'Medium',
    public reason: string = ''
  ) {}
}

// One day of the study plan. Day numbers are assigned by the backend, starting at 1.
export class CoachStudyDayModel {
  constructor(
    public day: number = 0,
    public focus: string = '',
    public tasks: string[] = []
  ) {}
}

// Returned by POST /api/coach/ask and POST /api/coach/help.
export class CoachAnswerModel {
  constructor(
    public answer: string = '',
    public summary: string = '',
    public strongTopics: CoachTopicModel[] = [],
    public focusAreas: CoachFocusAreaModel[] = [],
    public studyPlan: CoachStudyDayModel[] = [],
    public suggestedQuestions: string[] = [],
    // False when the candidate has no published results yet, or when the answer came from
    // general platform guidance. The UI hides the performance cards in that case.
    public hasPerformanceData: boolean = false,
    public generatedAt: string = ''
  ) {}
}

// Returned by GET /api/coach/snapshot — deterministic, no AI call involved.
export class CoachSnapshotModel {
  constructor(
    public firstName: string = '',
    public hasData: boolean = false,
    public totalAttempts: number = 0,
    public averageScore: number = 0,
    public passRate: number = 0,
    public certificatesEarned: number = 0,
    public topics: CoachTopicModel[] = [],
    public strongTopics: CoachTopicModel[] = [],
    public focusAreas: CoachFocusAreaModel[] = []
  ) {}
}

// One exchange, sent back with the next question so follow-ups make sense.
// Conversational context only — the backend never trusts it for identity or authorization.
export interface CoachTurn {
  question: string;
  answer: string;
}

// One entry in the on-screen transcript. Kept in component memory only — never written to
// sessionStorage or localStorage (it contains the candidate's performance discussion).
export interface CoachMessage {
  role: 'user' | 'coach';
  text: string;
  result?: CoachAnswerModel;
}
