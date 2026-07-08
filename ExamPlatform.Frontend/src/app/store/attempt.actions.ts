// attempt.actions.ts — NgRx actions for the attempt-exam feature.
// Why it exists: actions are the only way to trigger state changes in NgRx.
//   Every user interaction in attempt-exam dispatches one of these.
// How Angular/NgRx uses it: the reducer and effects both listen for these action types.
// CONCEPT DEMO: NgRx createAction + props — typed action creators with payload shapes.

import { createAction, props } from '@ngrx/store';
import { AttemptResumeModel, AttemptSummaryModel } from '../models/attempt.model';

export const AttemptActions = {
  // Dispatched by attempt-exam.ts on ngOnInit — triggers the startAttempt$ effect.
  startAttempt: createAction(
    '[Attempt] Start Attempt',
    props<{ examId: number }>()
  ),

  // Dispatched by the effect when POST /api/attempts/start succeeds.
  startAttemptSuccess: createAction(
    '[Attempt] Start Attempt Success',
    props<{ attempt: AttemptResumeModel }>()
  ),

  // Dispatched by the effect when the API call fails.
  startAttemptFailure: createAction(
    '[Attempt] Start Attempt Failure',
    props<{ error: string }>()
  ),

  // Dispatched when the candidate clicks a question in the navigator panel.
  setCurrentQuestion: createAction(
    '[Attempt] Set Current Question',
    props<{ index: number }>()
  ),

  // Dispatched when the candidate selects an answer option.
  // The reducer updates the answers map; the saveAnswer$ effect auto-saves to the API.
  saveAnswer: createAction(
    '[Attempt] Save Answer',
    props<{ questionId: number; optionId: number | null }>()
  ),

  // Dispatched by the saveAnswer$ effect when the server confirms an auto-save.
  answerSynced: createAction(
    '[Attempt] Answer Synced',
    props<{ questionId: number }>()
  ),

  // Dispatched by the saveAnswer$ effect when an auto-save fails after retries
  // (typically: connection lost). The answer stays in pendingSync for later resync.
  answerSyncFailed: createAction(
    '[Attempt] Answer Sync Failed',
    props<{ questionId: number }>()
  ),

  // Dispatched when the browser fires 'online' (or the user clicks "Sync now") —
  // the syncPendingAnswers$ effect re-saves every answer still awaiting confirmation.
  syncPendingAnswers: createAction('[Attempt] Sync Pending Answers'),

  // Dispatched every second by the setInterval in attempt-exam.ts.
  tickTimer: createAction('[Attempt] Tick Timer'),

  // Dispatched when the candidate clicks Submit, or when the timer reaches zero.
  submitAttempt: createAction(
    '[Attempt] Submit Attempt',
    props<{ attemptId: number }>()
  ),

  // Dispatched by the effect after a successful submit — triggers navigation.
  submitAttemptSuccess: createAction(
    '[Attempt] Submit Attempt Success',
    props<{ attemptId: number }>()
  ),

  // Dispatched if submit fails.
  submitAttemptFailure: createAction(
    '[Attempt] Submit Attempt Failure',
    props<{ error: string }>()
  ),

  // Dispatched on ngOnDestroy to wipe the store slice clean.
  resetAttempt: createAction('[Attempt] Reset'),
};
