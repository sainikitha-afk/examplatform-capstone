// coachapi.service.spec.ts — unit tests for CoachApiService.
//
// Two things matter here: the browser talks ONLY to our backend (never to Gemini directly),
// and every failure is turned into a sentence a candidate can act on — no raw backend error,
// stack trace or provider payload is ever passed through to a component.

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CoachApiService } from './coachapi.service';

describe('CoachApiService', () => {
  let service: CoachApiService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/coach';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CoachApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loadSnapshot GETs /snapshot', () => {
    service.loadSnapshot().subscribe();
    const req = httpMock.expectOne(`${baseUrl}/snapshot`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('ask POSTs the message and history to our own backend, never to Gemini', () => {
    service.ask('What should I study next?', [{ question: 'q', answer: 'a' }]).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/ask`);
    expect(req.request.method).toBe('POST');
    expect(req.request.url.startsWith('http://localhost:5014')).toBe(true);
    expect(req.request.body).toEqual({
      message: 'What should I study next?',
      history: [{ question: 'q', answer: 'a' }],
    });
    req.flush({ answer: 'Focus on LINQ.' });
  });

  it('ask sends no identity fields — the backend derives the user from the JWT', () => {
    service.ask('hello').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/ask`);
    const body = req.request.body as Record<string, unknown>;
    expect(Object.keys(body).sort()).toEqual(['history', 'message']);
    expect(body['userId']).toBeUndefined();
    expect(body['role']).toBeUndefined();
    expect(body['topicScoresUrl']).toBeUndefined();
    req.flush({ answer: 'hi' });
  });

  it('help POSTs to /help (used by Examiners and Admins)', () => {
    service.help('How do certificates work?').subscribe();
    const req = httpMock.expectOne(`${baseUrl}/help`);
    expect(req.request.method).toBe('POST');
    req.flush({ answer: 'Certificates are issued when…' });
  });

  it('surfaces a friendly message on 503 instead of the raw error, and reassures about exam data', () => {
    let message = '';
    service.ask('hi').subscribe({ error: (err: Error) => (message = err.message) });

    httpMock.expectOne(`${baseUrl}/ask`).flush(
      {
        status: 503,
        error:
          'The AI Learning Coach is temporarily unavailable. Your exam data and existing analytics are unaffected. Please try again shortly.',
      },
      { status: 503, statusText: 'Service Unavailable' }
    );

    expect(message).toContain('temporarily unavailable');
    expect(message).toContain('unaffected');
  });

  it('surfaces a retry message on 429', () => {
    let message = '';
    service.ask('hi').subscribe({ error: (err: Error) => (message = err.message) });

    httpMock
      .expectOne(`${baseUrl}/ask`)
      .flush({ status: 429, error: 'rate limited' }, { status: 429, statusText: 'Too Many Requests' });

    expect(message).toContain('too quickly');
  });

  it('surfaces the candidate-only explanation on 403', () => {
    let message = '';
    service.ask('hi').subscribe({ error: (err: Error) => (message = err.message) });

    httpMock.expectOne(`${baseUrl}/ask`).flush(
      { status: 403, error: 'Personalized learning coaching is available to candidates only.' },
      { status: 403, statusText: 'Forbidden' }
    );

    expect(message).toContain('candidates only');
  });

  it('never leaks a raw 500 body to the caller', () => {
    let message = '';
    service.ask('hi').subscribe({ error: (err: Error) => (message = err.message) });

    httpMock
      .expectOne(`${baseUrl}/ask`)
      .flush('NullReferenceException at LearningCoachService.line42', {
        status: 500,
        statusText: 'Server Error',
      });

    expect(message).not.toContain('NullReferenceException');
    expect(message).toContain('Something went wrong');
  });

  it('explains an unreachable backend', () => {
    let message = '';
    service.ask('hi').subscribe({ error: (err: Error) => (message = err.message) });

    httpMock.expectOne(`${baseUrl}/ask`).error(new ProgressEvent('network error'));

    expect(message).toContain('Cannot reach the server');
  });
});
