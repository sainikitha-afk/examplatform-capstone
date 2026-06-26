// OptionModel — a single answer choice inside a question.
// Why it exists: questions have 4 options each; this model carries optionId, text, and (for
// examiner/admin views) whether it is the correct answer.
// How Angular uses it: rendered in *ngFor inside question forms and attempt screens.

// ExamOptionModel — candidate view: no isCorrect field (backend deliberately omits it).
export class ExamOptionModel {
  constructor(
    public optionId: number = 0,
    public optionText: string = ""
  ) {}
}

// OptionModel — examiner / admin view: includes isCorrect for the answer key.
export class OptionModel {
  constructor(
    public optionId: number = 0,
    public optionText: string = "",
    public isCorrect: boolean = false
  ) {}
}

// CreateOptionModel — used when building a question creation/update request body.
export class CreateOptionModel {
  constructor(
    public optionText: string = "",
    public isCorrect: boolean = false
  ) {}
}
