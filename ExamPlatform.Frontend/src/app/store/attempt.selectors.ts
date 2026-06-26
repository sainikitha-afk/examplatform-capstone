// attempt.selectors.ts — Typed selectors for the attempt slice of the NgRx store.
// Why it exists: selectors are memoized — they only recompute when the relevant
//   slice of state actually changes. Components import these instead of reading
//   raw store state, keeping templates clean.
// How Angular/NgRx uses it: attempt-exam.ts calls this.store.select(selectXxx) to get
//   an Observable that the template or timer logic subscribes to.
// CONCEPT DEMO: createFeatureSelector + createSelector — composable, memoized projections.

import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AttemptState } from './attempt.reducer';

// Feature selector picks the 'attempt' slice registered in app.config.ts.
export const selectAttemptState = createFeatureSelector<AttemptState>('attempt');

// Individual field selectors — components import only what they need.
export const selectAttemptId       = createSelector(selectAttemptState, s => s.attemptId);
export const selectExamTitle       = createSelector(selectAttemptState, s => s.examTitle);
export const selectQuestions       = createSelector(selectAttemptState, s => s.questions);
export const selectCurrentIndex    = createSelector(selectAttemptState, s => s.currentQuestionIndex);
export const selectAnswers         = createSelector(selectAttemptState, s => s.answers);
export const selectTimeRemaining   = createSelector(selectAttemptState, s => s.timeRemaining);
export const selectAttemptStatus   = createSelector(selectAttemptState, s => s.status);
export const selectErrorMessage    = createSelector(selectAttemptState, s => s.errorMessage);

// Derived selector: the question object currently on screen.
export const selectCurrentQuestion = createSelector(
  selectQuestions,
  selectCurrentIndex,
  (questions, index) => questions[index] ?? null
);

// Derived selector: how many questions have been answered so far.
export const selectAnsweredCount = createSelector(
  selectQuestions,
  selectAnswers,
  (questions, answers) => questions.filter(q => answers[q.questionId] != null).length
);
