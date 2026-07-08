// attempt.effects.ts — NgRx effects: side-effectful logic triggered by actions.
// Why it exists: reducers must be pure functions; HTTP calls and navigation are side effects.
//   Effects listen for actions, perform async work, and dispatch new actions.
// How Angular/NgRx uses it: registered via provideEffects([AttemptEffects]) in app.config.ts.
// CONCEPT DEMO: NgRx Effects with constructor injection, switchMap, withLatestFrom, catchError.

import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { Observable, of, from, EMPTY, forkJoin } from 'rxjs';
import { switchMap, concatMap, mergeMap, map, catchError, withLatestFrom, tap, retry } from 'rxjs/operators';
import { AttemptApiService } from '../services/attemptapi.service';
import { AttemptActions } from './attempt.actions';
import { selectAttemptId, selectPendingSync } from './attempt.selectors';
import { SaveAnswerModel, AttemptSummaryModel } from '../models/attempt.model';

@Injectable()
export class AttemptEffects {

  // Effect FIELDS declared here (no initializer).
  // They're assigned INSIDE the constructor — after Angular has injected actions$, store, etc.
  // This avoids TS2729 / runtime "Cannot read properties of undefined" — field initializers
  // run BEFORE the constructor body in TypeScript strict mode.
  startAttempt$;
  saveAnswer$;
  syncPendingAnswers$;
  submitAttempt$;
  navigateAfterSubmit$;

  // Constructor injection: Angular DI provides Actions stream, Store, services, and Router.
  constructor(
    private actions$: Actions,
    private store: Store,
    private attemptService: AttemptApiService,
    private router: Router
  ) {

    // startAttempt$ — calls POST /api/attempts/start when the candidate navigates to /attempt/:examId.
    this.startAttempt$ = createEffect(() =>
      this.actions$.pipe(
        ofType(AttemptActions.startAttempt),
        switchMap(({ examId }) =>
          this.attemptService.startAttempt(examId).pipe(
            map(attempt => AttemptActions.startAttemptSuccess({ attempt })),
            catchError(err =>
              of(AttemptActions.startAttemptFailure({ error: err?.error?.message ?? 'Could not start exam.' }))
            )
          )
        )
      )
    );

    // saveAnswer$ — auto-saves a single answer whenever the candidate selects an option.
    // concatMap (not switchMap): saves must run in order and never cancel each other —
    // switchMap would abort an in-flight save when the candidate answers quickly.
    // retry: transient network blips get 2 automatic retries before we flag the
    // answer as unsynced; it then stays in pendingSync until connection returns.
    this.saveAnswer$ = createEffect(() =>
      this.actions$.pipe(
        ofType(AttemptActions.saveAnswer),
        withLatestFrom(this.store.select(selectAttemptId)),
        concatMap(([action, attemptId]) => {
          if (!attemptId) return EMPTY;
          const body = new SaveAnswerModel(action.questionId, action.optionId);
          return this.attemptService.saveAnswer(attemptId, body).pipe(
            retry({ count: 2, delay: 1500 }),
            map(() => AttemptActions.answerSynced({ questionId: action.questionId })),
            catchError(() => of(AttemptActions.answerSyncFailed({ questionId: action.questionId })))
          );
        })
      )
    );

    // syncPendingAnswers$ — fired when the browser comes back online (or the candidate
    // clicks "Sync now"). Re-sends every answer still in pendingSync so nothing is lost
    // after a disconnection.
    this.syncPendingAnswers$ = createEffect(() =>
      this.actions$.pipe(
        ofType(AttemptActions.syncPendingAnswers),
        withLatestFrom(this.store.select(selectAttemptId), this.store.select(selectPendingSync)),
        switchMap(([, attemptId, pending]) => {
          if (!attemptId) return EMPTY;
          const entries = Object.entries(pending);
          if (entries.length === 0) return EMPTY;
          return from(entries).pipe(
            mergeMap(([questionId, optionId]) => {
              const qId = Number(questionId);
              return this.attemptService.saveAnswer(attemptId, new SaveAnswerModel(qId, optionId)).pipe(
                map(() => AttemptActions.answerSynced({ questionId: qId })),
                catchError(() => of(AttemptActions.answerSyncFailed({ questionId: qId })))
              );
            })
          );
        })
      )
    );

    // submitAttempt$ — flushes any unsynced answers FIRST, then submits.
    // This guarantees that every option the candidate selected — even ones chosen
    // while offline — reaches the server before evaluation runs.
    this.submitAttempt$ = createEffect(() =>
      this.actions$.pipe(
        ofType(AttemptActions.submitAttempt),
        withLatestFrom(this.store.select(selectPendingSync)),
        switchMap(([{ attemptId }, pending]) => {
          const flushes = Object.entries(pending).map(([questionId, optionId]) =>
            this.attemptService.saveAnswer(attemptId, new SaveAnswerModel(Number(questionId), optionId)).pipe(
              retry({ count: 2, delay: 1000 }),
              // 409 = server already auto-submitted (timer expired) — not a flush
              // failure; let the submit call below resolve the final state.
              catchError(err => {
                if (err?.status === 409) return of(null);
                throw err;
              })
            )
          );
          // Typed as Observable<unknown>: the flush emission value is irrelevant —
          // it only gates the submit call — and the explicit type keeps the
          // forkJoin/of union from confusing the pipe overload resolution.
          const flush$: Observable<unknown> = flushes.length > 0 ? forkJoin(flushes) : of(null);

          return flush$.pipe(
            switchMap(() => this.attemptService.submitAttempt(attemptId)),
            map((summary: AttemptSummaryModel) => AttemptActions.submitAttemptSuccess({ attemptId: summary.attemptId })),
            catchError(err => {
              // 409 = already submitted (server-side auto-submit won the race).
              // That IS a completed attempt — treat as success and show the result.
              if (err?.status === 409) {
                return of(AttemptActions.submitAttemptSuccess({ attemptId }));
              }
              return of(AttemptActions.submitAttemptFailure({
                error: err?.error?.message
                  ?? 'Could not submit — please check your connection and press Submit again. Your answers are saved.'
              }));
            })
          );
        })
      )
    );

    // navigate after submit — waits for submitAttemptSuccess then routes to the result page.
    this.navigateAfterSubmit$ = createEffect(() =>
      this.actions$.pipe(
        ofType(AttemptActions.submitAttemptSuccess),
        tap(({ attemptId }) => this.router.navigate(['/result', attemptId]))
      ),
      { dispatch: false }
    );
  }
}