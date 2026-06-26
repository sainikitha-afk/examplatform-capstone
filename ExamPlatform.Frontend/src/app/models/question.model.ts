// QuestionModel — frontend twins of QuestionDto and related DTOs.
// Why it exists: question bank (manage-questions) and exam-builder need typed question objects.
// How Angular uses it: questionapi.service.ts returns Observable<QuestionModel[]>;
// components bind to it in *ngFor tables and forms.
import { OptionModel, CreateOptionModel, ExamOptionModel } from './option.model';

export class QuestionModel {
  constructor(
    public questionId: number = 0,
    public examinerId: number = 0,
    public catId: number = 0,
    public questionText: string = "",
    public marks: number = 1,
    public difficulty: string = "Easy",   // "Easy" | "Medium" | "Hard"
    public createdAt: string = "",
    public isActive: boolean = true,
    public options: OptionModel[] = []
  ) {}
}

// CreateQuestionModel — request body for POST /api/questions.
export class CreateQuestionModel {
  constructor(
    public catId: number = 0,
    public questionText: string = "",
    public marks: number = 1,
    public difficulty: string = "Easy",
    public options: CreateOptionModel[] = [
      new CreateOptionModel("", false),
      new CreateOptionModel("", false),
      new CreateOptionModel("", false),
      new CreateOptionModel("", true)
    ]
  ) {}
}

// QuestionInAttemptModel — trimmed question returned inside AttemptResumeDto.
// No isCorrect field; includes selectedOptionId (null if the candidate has not answered yet).
export class QuestionInAttemptModel {
  constructor(
    public questionId: number = 0,
    public questionText: string = "",
    public marks: number = 1,
    public difficulty: string = "",
    public options: ExamOptionModel[] = [],
    public selectedOptionId: number | null = null
  ) {}
}
