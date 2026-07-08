// guards.spec.ts — unit tests for authGuard and roleGuard.
// Why it exists: these two functions ARE the frontend security layer — private
// URLs must be unreachable without a token, and role mix-ups must redirect.
// Functional guards use inject(), so they must run inside an injection context.
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, UrlTree, provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { roleGuard } from './role.guard';

// Builds a fake route snapshot carrying the data: { role } the guard reads.
function routeWithRole(role?: string): ActivatedRouteSnapshot {
  const snapshot = new ActivatedRouteSnapshot();
  snapshot.data = role ? { role } : {};
  return snapshot;
}

describe('authGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    sessionStorage.clear();
  });

  it('allows navigation when a token exists', () => {
    sessionStorage.setItem('token', 'jwt');
    const result = TestBed.runInInjectionContext(() =>
      authGuard(routeWithRole(), {} as never));
    expect(result).toBe(true);
  });

  it('redirects to /login when there is no token', () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard(routeWithRole(), {} as never)) as UrlTree;
    expect(result instanceof UrlTree).toBe(true);
    expect(result.toString()).toBe('/login');
  });
});

describe('roleGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    sessionStorage.clear();
  });

  it('allows navigation when the stored role matches the route requirement', () => {
    sessionStorage.setItem('role', 'Admin');
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRole('Admin'), {} as never));
    expect(result).toBe(true);
  });

  it('redirects a Candidate away from an Admin route to their own dashboard', () => {
    sessionStorage.setItem('role', 'Candidate');
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRole('Admin'), {} as never)) as UrlTree;
    expect(result.toString()).toBe('/candidate');
  });

  it('redirects an Examiner away from a Candidate route to /examiner', () => {
    sessionStorage.setItem('role', 'Examiner');
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRole('Candidate'), {} as never)) as UrlTree;
    expect(result.toString()).toBe('/examiner');
  });

  it('allows any authenticated user when the route declares no role', () => {
    sessionStorage.setItem('role', 'Candidate');
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRole(), {} as never));
    expect(result).toBe(true);
  });
});
