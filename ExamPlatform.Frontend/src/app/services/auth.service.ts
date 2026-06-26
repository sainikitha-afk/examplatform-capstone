// AuthService — handles login and register HTTP calls, JWT storage, and currentUser broadcast.
// Why it exists: components should never talk to HTTP directly; they ask this service.
// How Angular uses it: marked @Injectable({providedIn:'root'}) so one instance is shared
//   across the whole app via dependency injection. AuthService is the single source of truth
//   for session start/end.
// CONCEPT DEMO: catchError operator (transforms HTTP errors into user-friendly messages).

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { LoginModel } from '../models/login.model';
import { RegisterModel } from '../models/register.model';
import { AuthResponseModel } from '../models/auth-response.model';
import { currentUser$ } from '../rxjs/auth.subject';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = 'http://localhost:5014/api/auth';

  // Constructor injection: Angular DI provides HttpClient and Router.
  // RULE: never use inject() — always constructor injection.
  constructor(private http: HttpClient, private router: Router) {}

  // Calls POST /api/auth/login. Returns Observable<AuthResponseModel>.
  // CONCEPT DEMO: catchError — if the server returns 401/400, we throw a friendly error.
  login(credentials: LoginModel): Observable<AuthResponseModel> {
    return this.http.post<AuthResponseModel>(`${this.baseUrl}/login`, credentials).pipe(
      catchError((err) => {
        const message = err.error?.message ?? 'Login failed. Check your credentials.';
        return throwError(() => new Error(message));
      })
    );
  }

  // Calls POST /api/auth/register. Returns Observable<AuthResponseModel>.
  // Backend auto-creates the account AND returns a token — no second login call needed.
  register(data: RegisterModel): Observable<AuthResponseModel> {
    return this.http.post<AuthResponseModel>(`${this.baseUrl}/register`, data).pipe(
      catchError((err) => {
        const message = err.error?.message ?? 'Registration failed.';
        return throwError(() => new Error(message));
      })
    );
  }

  // Persists the JWT and user info, pushes to the BehaviorSubject, then routes by role.
  // Called by both login and register screens after a successful response.
  saveSessionAndRoute(response: AuthResponseModel): void {
    sessionStorage.setItem('token', response.token);
    sessionStorage.setItem('userId', String(response.userId));
    sessionStorage.setItem('fullName', response.fullName);
    sessionStorage.setItem('username', response.username);
    sessionStorage.setItem('email', response.email);
    sessionStorage.setItem('role', response.role);

    // Push to BehaviorSubject — navigation and other subscribers update instantly.
    currentUser$.next(response);

    // Role-based redirect after successful auth.
    switch (response.role) {
      case 'Admin':     this.router.navigate(['/admin']);     break;
      case 'Examiner':  this.router.navigate(['/examiner']);  break;
      default:          this.router.navigate(['/candidate']); break;
    }
  }

  // Clears session, resets BehaviorSubject to null, navigates back to login.
  logout(): void {
    sessionStorage.clear();
    currentUser$.next(null);
    this.router.navigate(['/login']);
  }

  // Convenience helper used by guards.
  isLoggedIn(): boolean {
    return !!sessionStorage.getItem('token');
  }

  getRole(): string {
    return sessionStorage.getItem('role') ?? '';
  }
}
