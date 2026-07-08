// auth.interceptor.ts — HTTP interceptor that attaches the JWT to every outgoing request.
// Why it exists: without this, every service method would have to manually add the
//   Authorization header. The interceptor does it once, automatically, for all requests.
// How Angular uses it: registered as an HttpInterceptorFn in app.config.ts via
//   provideHttpClient(withInterceptors([authInterceptor])). Angular calls this function
//   before every HTTP request leaves the browser.
// CONCEPT DEMO: HttpInterceptorFn — a functional (non-class) interceptor supported in Angular 15+.
// It ALSO handles two error cases globally so every screen behaves the same:
//   401 → the token is expired/invalid → clear the session and send the user to /login
//   429 → the backend rate limit was hit → show a friendly "slow down" message

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { currentUser$ } from '../rxjs/auth.subject';

// authInterceptor is a plain function — no class, no @Injectable needed.
// Angular's functional interceptor API passes (req, next) as arguments.
// inject() is allowed here (like in guards) because functional interceptors
// have no constructor to inject into.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const token = sessionStorage.getItem('token');

  // Clone the request and add the Authorization header (requests are immutable
  // in Angular — cloning is required). If no token, forward unchanged.
  const outgoing = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(outgoing).pipe(
    catchError((err: HttpErrorResponse) => {
      // Session expired or token invalid — but only if the user WAS logged in
      // (a failed login attempt is also a 401 and must stay on the login page).
      if (err.status === 401 && token) {
        sessionStorage.clear();
        currentUser$.next(null);
        alert('Your session has expired. Please sign in again.');
        router.navigate(['/login']);
      }

      // Backend rate limit exceeded — tell the user to slow down instead of crashing.
      if (err.status === 429) {
        alert('Too many requests — please wait a moment and try again.');
      }

      // Re-throw so each component's error handler still runs as before.
      return throwError(() => err);
    })
  );
};
