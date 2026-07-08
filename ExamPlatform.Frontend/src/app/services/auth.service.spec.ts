// auth.service.spec.ts — unit tests for AuthService.
// Why it exists: login/register error mapping, session storage, and role-based
// routing are the security core of the app — they must be proven by tests.
// How it works: HttpTestingController intercepts every HTTP call so no real
// backend is needed; we assert the URL, method, body, and response handling.
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { LoginModel } from '../models/login.model';
import { RegisterModel } from '../models/register.model';
import { AuthResponseModel } from '../models/auth-response.model';
import { currentUser$ } from '../rxjs/auth.subject';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: Router;

  const fakeResponse: AuthResponseModel = new AuthResponseModel(
    'jwt-token-123', 7, 'Riya Sharma', 'riya', 'riya@test.com', 'Candidate');

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    sessionStorage.clear();
    currentUser$.next(null);
  });

  afterEach(() => httpMock.verify()); // no unexpected/unflushed requests

  it('login posts credentials to /api/auth/login and returns the response', () => {
    let received: AuthResponseModel | undefined;
    service.login(new LoginModel('riya', 'secret')).subscribe({ next: r => received = r });

    const req = httpMock.expectOne('http://localhost:5014/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(fakeResponse);

    expect(received!.token).toBe('jwt-token-123');
    expect(received!.role).toBe('Candidate');
  });

  it('login maps a 401 to a friendly "invalid credentials" error', () => {
    let message = '';
    service.login(new LoginModel('riya', 'wrong')).subscribe({
      error: (e: Error) => message = e.message
    });

    httpMock.expectOne('http://localhost:5014/api/auth/login')
            .flush({ error: 'nope' }, { status: 401, statusText: 'Unauthorized' });

    expect(message).toBe('Invalid username or password.');
  });

  it('login maps a network failure (status 0) to a "server unreachable" error', () => {
    let message = '';
    service.login(new LoginModel('riya', 'secret')).subscribe({
      error: (e: Error) => message = e.message
    });

    httpMock.expectOne('http://localhost:5014/api/auth/login')
            .error(new ProgressEvent('error'), { status: 0 });

    expect(message).toContain('Cannot reach the server');
  });

  it('register maps a 409 to a friendly "already exists" error', () => {
    let message = '';
    service.register(new RegisterModel('Riya Sharma', 'riya', 'riya@test.com', 'secret123'))
      .subscribe({ error: (e: Error) => message = e.message });

    httpMock.expectOne('http://localhost:5014/api/auth/register')
            .flush({}, { status: 409, statusText: 'Conflict' });

    expect(message).toContain('already exists');
  });

  it('saveSessionAndRoute stores the session and routes by role', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    service.saveSessionAndRoute(fakeResponse);

    expect(sessionStorage.getItem('token')).toBe('jwt-token-123');
    expect(sessionStorage.getItem('role')).toBe('Candidate');
    expect(navigateSpy).toHaveBeenCalledWith(['/candidate']);

    service.saveSessionAndRoute({ ...fakeResponse, role: 'Admin' });
    expect(navigateSpy).toHaveBeenCalledWith(['/admin']);

    service.saveSessionAndRoute({ ...fakeResponse, role: 'Examiner' });
    expect(navigateSpy).toHaveBeenCalledWith(['/examiner']);
  });

  it('logout clears the session, resets currentUser$, and returns to /login', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    sessionStorage.setItem('token', 'jwt');
    currentUser$.next(fakeResponse);

    service.logout();

    expect(sessionStorage.getItem('token')).toBeNull();
    expect(currentUser$.value).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('isLoggedIn and getRole read from sessionStorage', () => {
    expect(service.isLoggedIn()).toBe(false);
    expect(service.getRole()).toBe('');

    sessionStorage.setItem('token', 'jwt');
    sessionStorage.setItem('role', 'Examiner');

    expect(service.isLoggedIn()).toBe(true);
    expect(service.getRole()).toBe('Examiner');
  });
});
