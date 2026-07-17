// ExamModel — frontend twins of ExamSummaryDto, ExamDetailDto and their variants.
// Why it exists: exam screens (browse, manage, attempt) all need typed exam objects.
// How Angular uses it: examapi.service.ts methods return Observable<ExamSummaryModel[]> etc.;
// components render exam data via interpolation and *ngFor.

// ExamSummaryModel — lightweight shape used in browse and manage lists.
export class ExamSummaryModel {
  constructor(
    public examId: number = 0,
    public title: string = "",
    public categoryId: number = 0,
    public examinerId: number = 0,
    public durationMinutes: number = 0,
    public totalMarks: number = 0,
    public passingMarks: number = 0,
    public startTime: string = "",
    public endTime: string = "",
    public examStatus: string = "",  // "Draft" | "Published" | "ResultsPublished"
    public isPremium: boolean = false,
    public price: number | null = null,
    public isFeatured: boolean = false,
    // Institute badge on browse/manage cards — set only for premium exams whose
    // examiner has an Admin-verified institution.
    public isInstituteVerified: boolean = false,
    public instituteName: string | null = null,
    public instituteLogoUrl: string | null = null
  ) {}
}

// ExamOptionForCandidateModel — no isCorrect (deliberately hidden from candidate).
export class ExamOptionForCandidateModel {
  constructor(
    public optionId: number = 0,
    public optionText: string = ""
  ) {}
}

// ExamQuestionForCandidateModel — used inside ExamDetailForCandidateModel.
export class ExamQuestionForCandidateModel {
  constructor(
    public examQuestionId: number = 0,
    public questionId: number = 0,
    public questionOrder: number = 0,
    public questionText: string = "",
    public marks: number = 0,
    public difficulty: string = "",
    public options: ExamOptionForCandidateModel[] = []
  ) {}
}

// ExamOptionFullModel — includes isCorrect for examiner/admin view.
export class ExamOptionFullModel {
  constructor(
    public optionId: number = 0,
    public optionText: string = "",
    public isCorrect: boolean = false
  ) {}
}

// ExamQuestionFullModel — used inside ExamDetailModel (examiner/admin).
export class ExamQuestionFullModel {
  constructor(
    public examQuestionId: number = 0,
    public questionId: number = 0,
    public questionOrder: number = 0,
    public questionText: string = "",
    public catId: number = 0,        // exams can mix categories — shown per question
    public marks: number = 0,
    public difficulty: string = "",
    public options: ExamOptionFullModel[] = []
  ) {}
}

// ExamDetailModel — full exam with questions; examiner/admin get isCorrect in options.
export class ExamDetailModel {
  constructor(
    public examId: number = 0,
    public title: string = "",
    public instructions: string = "",
    public categoryId: number = 0,
    public examinerId: number = 0,
    public durationMinutes: number = 0,
    public totalMarks: number = 0,
    public passingMarks: number = 0,
    public startTime: string = "",
    public endTime: string = "",
    public examStatus: string = "",
    public createdAt: string = "",
    public questions: ExamQuestionFullModel[] = [],
    public isPremium: boolean = false,
    public price: number | null = null,
    public isFeatured: boolean = false,
    // Candidate-view only field (absent/ignored for the examiner/admin DTO):
    // true when the candidate holds an Active purchase, or the exam isn't
    // premium at all.
    public hasAccess: boolean = true,
    // Institute branding — set only when the owning examiner has an
    // Admin-Verified institution. Drives the "Institute Verified" badge.
    public isInstituteVerified: boolean = false,
    public instituteName: string | null = null,
    public instituteLogoUrl: string | null = null
  ) {}
}

// CreateExamModel — request body for POST /api/exams.
export class CreateExamModel {
  constructor(
    public catId: number = 0,
    public title: string = "",
    public instructions: string = "",
    public durationMinutes: number = 60,
    public totalMarks: number = 100,
    public passingMarks: number = 40,
    public startTime: string = "",
    public endTime: string = "",
    public isPremium: boolean = false,
    public price: number | null = null
  ) {}
}

// UpdateExamModel — request body for PUT /api/exams/{id}. CatId is intentionally omitted.
export class UpdateExamModel {
  constructor(
    public title: string = "",
    public instructions: string = "",
    public durationMinutes: number = 60,
    public totalMarks: number = 100,
    public passingMarks: number = 40,
    public startTime: string = "",
    public endTime: string = ""
  ) {}
}
