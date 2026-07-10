// app.config.ts — Application-wide providers wired here: HTTP client, interceptors,
//   router, NgRx store + effects + devtools.
// Why it exists: standalone Angular apps (no NgModule) use ApplicationConfig to register
//   all providers in one place instead of spreading them across modules.
// CONCEPTS DEMO:
//   - provideHttpClient(withInterceptors([...])) — registers the functional interceptor
//   - provideStore / provideEffects / provideStoreDevtools — NgRx wiring
//   - provideRouter(routes) — routing

import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { attemptReducer } from './store/attempt.reducer';
import { AttemptEffects } from './store/attempt.effects';

export const appConfig: ApplicationConfig = {
  providers: [
    provideCharts(withDefaultRegisterables()),
    provideBrowserGlobalErrorListeners(),

    // Zone-based change detection: components across the app set plain fields
    // (isLoading, errorMessage, list data) inside HTTP subscribe callbacks.
    // Without zone.js those updates never trigger a re-render — spinners hang
    // forever and clicks appear to need a second press. eventCoalescing batches
    // multiple synchronous events into a single change-detection pass.
    provideZoneChangeDetection({ eventCoalescing: true }),

    // CONCEPT DEMO: functional HTTP interceptor attached here.
    provideHttpClient(withInterceptors([authInterceptor])),

    // Router with all app routes.
    provideRouter(routes),

    // CONCEPT DEMO: NgRx — register the attempt slice reducer.
    provideStore({ attempt: attemptReducer }),

    // CONCEPT DEMO: NgRx Effects — register the AttemptEffects class.
    provideEffects([AttemptEffects]),

    // NgRx DevTools — visible in the Redux DevTools browser extension.
    provideStoreDevtools({
      maxAge: 25,
      logOnly: !isDevMode(),   // In production: log-only mode (no time-travel).
    }),
  ]
};
