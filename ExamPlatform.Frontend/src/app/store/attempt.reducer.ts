// attempt.reducer.ts — Pure function that produces the next attempt state given an action.
// Why it exists: in NgRx, the reducer is the ONLY place that mutates state.
//   Components and effects never modify state directly.
// How Angular/NgRx uses it: registered in app.config.ts via provideStore({ attempt: attemptReducer }).
// CONCEPT DEMO: NgRx createReducer + on() — pattern-matched state transitions.

import { createReducer, on } from '@ngrx/store';
import { QuestionInAttemptModel } from '../models/question.model';
import { AttemptActions } from './attempt.actions';

// Shape of the attempt slice of the NgRx store.
export interface AttemptState {
  attemptId: number;
  examId: number;
  examTitle: string;
  // Institute badge for premium exams (verified institution). Live-derived by the API.
  isInstituteVerified: boolean;
  instituteName: string | null;
  instituteLogoUrl: string | null;
  questions: QuestionInAttemptModel[];
  currentQuestionIndex: number;
  // answers maps questionId → selected optionId (null = skipped/cleared).
  answers: Record<number, number | null>;
  // pendingSync holds answers not yet confirmed saved by the server.
  // Entries are added on every selection and removed when the API call succeeds.
  // If the connection drops, they stay here and are re-sent on reconnect —
  // this is what guarantees no answer is ever lost.
  pendingSync: Record<number, number | null>;
  // True once an auto-save has failed (connection problems detected server-side).
  isOffline: boolean;
  timeRemaining: number;      // seconds
  status: 'idle' | 'loading' | 'active' | 'submitting' | 'submitted' | 'error';
  errorMessage: string;
}

const initialState: AttemptState = {
  attemptId: 0,
  examId: 0,
  examTitle: '',
  isInstituteVerified: false,
  instituteName: null,
  instituteLogoUrl: null,
  questions: [],
  currentQuestionIndex: 0,
  answers: {},
  pendingSync: {},
  isOffline: false,
  timeRemaining: 0,
  status: 'idle',
  errorMessage: '',
};

export const attemptReducer = createReducer(
  initialState,

  // Loading starts — set status to 'loading'.
  on(AttemptActions.startAttempt, (state) => ({
    ...state,
    status: 'loading' as const,
    errorMessage: '',
  })),

  // Success — populate all state from the API response.
  on(AttemptActions.startAttemptSuccess, (state, { attempt }) => {
    // Rebuild the answers map from any pre-saved answers (resume flow).
    const answers: Record<number, number | null> = {};
    for (const q of attempt.questions) {
      answers[q.questionId] = q.selectedOptionId ?? null;
    }
    return {
      ...state,
      attemptId: attempt.attemptId,
      examId: attempt.examId,
      examTitle: attempt.examTitle,
      isInstituteVerified: attempt.isInstituteVerified,
      instituteName: attempt.instituteName,
      instituteLogoUrl: attempt.instituteLogoUrl,
      questions: attempt.questions,
      answers,
      pendingSync: {},        // fresh start/resume — server state is the source of truth
      isOffline: false,
      timeRemaining: attempt.remainingSeconds,
      currentQuestionIndex: 0,
      status: 'active' as const,
    };
  }),

  // Failure — store error message.
  on(AttemptActions.startAttemptFailure, (state, { error }) => ({
    ...state,
    status: 'error' as const,
    errorMessage: error,
  })),

  // Move to a different question in the navigator.
  on(AttemptActions.setCurrentQuestion, (state, { index }) => ({
    ...state,
    currentQuestionIndex: index,
  })),

  // Record the candidate's answer in the answers map AND mark it pending sync.
  on(AttemptActions.saveAnswer, (state, { questionId, optionId }) => ({
    ...state,
    answers: { ...state.answers, [questionId]: optionId },
    pendingSync: { ...state.pendingSync, [questionId]: optionId },
  })),

  // Server confirmed the save — drop it from pendingSync and clear the offline flag.
  on(AttemptActions.answerSynced, (state, { questionId }) => {
    const pendingSync = { ...state.pendingSync };
    delete pendingSync[questionId];
    return { ...state, pendingSync, isOffline: false };
  }),

  // Save failed after retries — keep the entry in pendingSync for the resync pass.
  on(AttemptActions.answerSyncFailed, (state) => ({
    ...state,
    isOffline: true,
  })),

  // Decrement the timer by 1 second.
  on(AttemptActions.tickTimer, (state) => ({
    ...state,
    timeRemaining: Math.max(0, state.timeRemaining - 1),
  })),

  // Submitting — disable UI.
  on(AttemptActions.submitAttempt, (state) => ({
    ...state,
    status: 'submitting' as const,
  })),

  // Submitted — mark complete (effect will navigate away).
  on(AttemptActions.submitAttemptSuccess, (state) => ({
    ...state,
    status: 'submitted' as const,
  })),

  // Submit failed (usually connection loss). CRITICAL: return to 'active', NOT
  // 'error' — the candidate must keep their exam screen and answers so they can
  // retry the submission once the connection is back.
  on(AttemptActions.submitAttemptFailure, (state, { error }) => ({
    ...state,
    status: 'active' as const,
    errorMessage: error,
  })),

  // Reset — wipe the slice back to initial state.
  on(AttemptActions.resetAttempt, () => ({ ...initialState })),
);
