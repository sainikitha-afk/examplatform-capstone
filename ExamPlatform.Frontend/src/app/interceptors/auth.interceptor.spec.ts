// auth.interceptor.spec.ts — unit tests for the functional auth interceptor.
// Verifies: the Bearer token is attached, 401 ends the session gracefully,
// and 429 (rate limit) surfaces a friendly message instead of crashing.
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    sessionStorage.clear();
    // jsdom has no alert(); replace it so 401/429 handling can be asserted.
    vi.stubGlobal('alert', vi.fn());
  });

  afterEach(() => {
    httpMock.verify();
    vi.unstubAllGlobals();
  });

  it('attaches Authorization: Bearer <token> when a token is stored', () => {
    sessionStorage.setItem('token', 'jwt-abc');
    http.get('/api/anything').subscribe();

    const req = httpMock.expectOne('/api/anything');
    expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-abc');
    req.flush({});
  });

  it('sends no Authorization header when logged out', () => {
    http.get('/api/anything').subscribe();

    const req = httpMock.expectOne('/api/anything');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('a 401 while logged in clears the session and redirects to /login', () => {
    sessionStorage.setItem('token', 'expired-jwt');
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get('/api/anything').subscribe({ error: () => {} });
    httpMock.expectOne('/api/anything')
            .flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem('token')).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('a 401 with NO token (failed login) does not redirect', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.post('/api/auth/login', {}).subscribe({ error: () => {} });
    httpMock.expectOne('/api/auth/login')
            .flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('a 429 shows a rate-limit message and re-throws to the caller', () => {
    let callerSawError = false;
    http.get('/api/anything').subscribe({ error: () => callerSawError = true });
    httpMock.expectOne('/api/anything')
            .flush({}, { status: 429, statusText: 'Too Many Requests' });

    expect(alert).toHaveBeenCalledWith(
      'Too many requests — please wait a moment and try again.');
    expect(callerSawError).toBe(true);
  });
});
