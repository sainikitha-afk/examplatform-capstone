// question-card.ts — Child component: renders one question with its answer options.
// Why it exists: attempt-exam renders many questions; extracting one question into its own
//   component keeps each file small and demonstrates parent-child communication.
// How Angular uses it: attempt-exam imports QuestionCard and uses <app-question-card> in its template.
// CONCEPTS DEMO:
//   @Input() — receives a question object from the parent.
//   @Output() + EventEmitter — emits the candidate's choice back to the parent.
//   [ngClass] — highlights the currently selected option.

import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { QuestionInAttemptModel } from '../models/question.model';
import { ExamOptionModel } from '../models/option.model';

@Component({
  selector: 'app-question-card',
  imports: [CommonModule],
  templateUrl: './question-card.html',
  styleUrl: './question-card.css',
})
export class QuestionCard {
  // @Input: the parent passes the full question object.
  @Input() question: QuestionInAttemptModel = new QuestionInAttemptModel();

  // @Input: the parent passes the currently saved optionId (from the NgRx answers map).
  @Input() selectedOptionId: number | null = null;

  // @Output: emits { questionId, optionId } whenever the candidate selects an option.
  // The parent (attempt-exam) listens with (answerSelected)="handleAnswerSelected($event)".
  @Output() answerSelected = new EventEmitter<{ questionId: number; optionId: number }>();

  handleOptionSelect(option: ExamOptionModel): void {
    this.answerSelected.emit({
      questionId: this.question.questionId,
      optionId: option.optionId
    });
  }

  isSelected(optionId: number): boolean {
    return this.selectedOptionId === optionId;
  }
}
