// AnalyticsModel — frontend twins of CandidateDashboardDto and PlatformStatsDto.
// Why it exists: the candidate-analytics and admin-dashboard screens display aggregate stats.
// How Angular uses it: analyticsapi.service.ts returns these; dashboards bind to them via interpolation.

// ExamBestScoreModel — nested inside CandidateDashboardModel.
export class ExamBestScoreModel {
  constructor(
    public examId: number = 0,
    public examTitle: string = "",
    public bestScore: number = 0,
    public isPassed: boolean = false
  ) {}
}

// CandidateDashboardModel — returned by GET /api/analytics/me.
export class CandidateDashboardModel {
  constructor(
    public totalAttempts: number = 0,
    public averageScore: number = 0,
    public passRate: number = 0,           // percentage e.g. 66.67
    public certificatesEarned: number = 0,
    public bestScores: ExamBestScoreModel[] = []
  ) {}
}

// PlatformStatsModel — returned by GET /api/analytics/platform (Admin only).
export class PlatformStatsModel {
  constructor(
    public totalUsers: number = 0,
    public totalExaminers: number = 0,
    public totalCandidates: number = 0,
    public totalExams: number = 0,
    public totalAttempts: number = 0,
    public totalCertificatesIssued: number = 0,
    public examsCompletedLast30Days: number = 0
  ) {}
}
