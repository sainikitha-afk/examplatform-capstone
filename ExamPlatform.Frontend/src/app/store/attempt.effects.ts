// attempt.effects.ts — NgRx effects: side-effectful logic triggered by actions.
// Why it exists: reducers must be pure functions; HTTP calls and navigation are side effects.
//   Effects listen for actions, perform async work, and dispatch new actions.
// How Angular/NgRx uses it: registered via provideEffects([AttemptEffects]) in app.config.ts.
// CONCEPT DEMO: NgRx Effects with constructor injection, switchMap, withLatestFrom, catchError.

import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { of, EMPTY } from 'rxjs';
import { switchMap, map, catchError, withLatestFrom, tap } from 'rxjs/operators';
import { AttemptApiService } from '../services/attemptapi.service';
import { AttemptActions } from './attempt.actions';
import { selectAttemptId } from './attempt.selectors';
import { SaveAnswerModel } from '../models/attempt.model';

@Injectable()
export class AttemptEffects {

  // Effect FIELDS declared here (no initializer).
  // They're assigned INSIDE the constructor — after Angular has injected actions$, store, etc.
  // This avoids TS2729 / runtime "Cannot read properties of undefined" — field initializers
  // run BEFORE the constructor body in TypeScript strict mode.
  startAttempt$;
  saveAnswer$;
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

    // saveAnswer$ — auto-saves a single answer to the backend whenever the candidate selects an option.
    // Uses withLatestFrom to get the current attemptId from the store without subscribing separately.
    // dispatch:false because this effect doesn't need to dispatch a follow-up action.
    this.saveAnswer$ = createEffect(() =>
      this.actions$.pipe(
        ofType(AttemptActions.saveAnswer),
        withLatestFrom(this.store.select(selectAttemptId)),
        switchMap(([action, attemptId]) => {
          if (!attemptId) return EMPTY;
          const body = new SaveAnswerModel(action.questionId, action.optionId);
          return this.attemptService.saveAnswer(attemptId, body).pipe(
            catchError(() => EMPTY)  // Auto-save failures are silently ignored — don't interrupt the exam.
          );
        })
      ),
      { dispatch: false }
    );

    // submitAttempt$ — calls POST /api/attempts/{id}/submit then dispatches success/failure.
    this.submitAttempt$ = createEffect(() =>
      this.actions$.pipe(
        ofType(AttemptActions.submitAttempt),
        switchMap(({ attemptId }) =>
          this.attemptService.submitAttempt(attemptId).pipe(
            map(summary => AttemptActions.submitAttemptSuccess({ attemptId: summary.attemptId })),
            catchError(err =>
              of(AttemptActions.submitAttemptFailure({ error: err?.error?.message ?? 'Submit failed.' }))
            )
          )
        )
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