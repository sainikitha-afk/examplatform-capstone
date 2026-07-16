// ai-coach.spec.ts — the AI Learning Coach screen.
//
// Covers the states the feature promises (welcome / loading / success / no-data / error),
// the input validation that stops wasted AI calls, and the role split: a Candidate gets
// personalized coaching, an Examiner gets general guidance and is told why.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { AiCoach } from './ai-coach';

describe('AiCoach', () => {
  let fixture: ComponentFixture<AiCoach>;
  let component: AiCoach;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5014/api/coach';

  const snapshotWithData = {
    firstName: 'Diya',
    hasData: true,
    totalAttempts: 4,
    averageScore: 15,
    passRate: 25,
    certificatesEarned: 1,
    topics: [{ topic: 'LINQ Operations', score: 100 }],
    strongTopics: [{ topic: 'LINQ Operations', score: 100 }],
    focusAreas: [
      { topic: 'Exception Handling', score: 30, priority: 'High', reason: 'Lowest topic score.' },
    ],
  };

  const emptySnapshot = {
    firstName: 'Diya',
    hasData: false,
    totalAttempts: 0,
    averageScore: 0,
    passRate: 0,
    certificatesEarned: 0,
    topics: [],
    strongTopics: [],
    focusAreas: [],
  };

  const coachAnswer = {
    answer: 'Your weakest area is Exception Handling.',
    summary: 'Focus first on exception handling.',
    strongTopics: [{ topic: 'LINQ Operations', score: 100 }],
    focusAreas: [
      { topic: 'Exception Handling', score: 30, priority: 'High', reason: 'Lowest topic score.' },
    ],
    studyPlan: [{ day: 1, focus: 'Exception fundamentals', tasks: ['Review try/catch/finally'] }],
    suggestedQuestions: ['Create a 7-day study plan'],
    hasPerformanceData: true,
    generatedAt: '2026-07-13T10:00:00Z',
  };

  // The component reads the role from sessionStorage, exactly as the guards and navigation do.
  function createComponent(role: string): void {
    sessionStorage.setItem('role', role);

    fixture = TestBed.createComponent(AiCoach);
    component = fixture.componentInstance;
    fixture.detectChanges();   // triggers ngOnInit
  }

  function text(): string {
    return fixture.nativeElement.textContent as string;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AiCoach],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    sessionStorage.clear();
    httpMock.verify();
  });

  // ── Role behaviour ──────────────────────────────────────────────────

  it('loads the candidate snapshot on entry', () => {
    createComponent('Candidate');

    const req = httpMock.expectOne(`${baseUrl}/snapshot`);
    expect(req.request.method).toBe('GET');
    req.flush(snapshotWithData);
    fixture.detectChanges();

    expect(component.isCandidate).toBe(true);
    expect(component.snapshot?.hasData).toBe(true);
    expect(text()).toContain('Hi Diya!');
  });

  it('does not load candidate data for an Examiner, and explains that coaching is candidate-only', () => {
    createComponent('Examiner');

    // No snapshot request may be made for a non-candidate.
    httpMock.expectNone(`${baseUrl}/snapshot`);

    expect(component.isCandidate).toBe(false);
    expect(text()).toContain('Personalized learning coaching is for candidates');
  });

  it('routes an Examiner question to /help, not /ask', () => {
    createComponent('Examiner');

    component.draft = 'How do I publish results?';
    component.send();

    const req = httpMock.expectOne(`${baseUrl}/help`);
    expect(req.request.method).toBe('POST');
    req.flush({ ...coachAnswer, hasPerformanceData: false });
  });

  it('routes a Candidate question to /ask', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What should I study next?';
    component.send();

    httpMock.expectOne(`${baseUrl}/ask`).flush(coachAnswer);
  });

  // ── Validation ──────────────────────────────────────────────────────

  it('cannot send a blank or whitespace-only question', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = '';
    expect(component.canSend).toBe(false);

    component.draft = '     \n\t ';
    expect(component.canSend).toBe(false);

    component.send();
    httpMock.expectNone(`${baseUrl}/ask`);   // no wasted AI call
  });

  it('rejects an oversized question and shows the limit', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'a'.repeat(component.maxMessageLength + 1);
    fixture.detectChanges();

    expect(component.isTooLong).toBe(true);
    expect(component.canSend).toBe(false);
    expect(text()).toContain("That's too long");

    component.send();
    httpMock.expectNone(`${baseUrl}/ask`);
  });

  it('disables Send while a request is in flight (no double submit)', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What should I study next?';
    component.send();
    fixture.detectChanges();

    expect(component.isSending).toBe(true);
    expect(component.canSend).toBe(false);

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.coach-send');
    expect(button.disabled).toBe(true);

    // A second send while the first is pending must not fire another request.
    component.draft = 'again';
    component.send();
    httpMock.expectOne(`${baseUrl}/ask`).flush(coachAnswer);
  });

  // ── Success rendering ───────────────────────────────────────────────

  it('renders the answer, strengths, focus areas and study plan', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What should I study next?';
    component.send();
    httpMock.expectOne(`${baseUrl}/ask`).flush(coachAnswer);
    fixture.detectChanges();

    const rendered = text();
    expect(rendered).toContain('Your weakest area is Exception Handling.');
    expect(rendered).toContain('Focus first on exception handling.');   // summary
    expect(rendered).toContain('Your strengths');
    expect(rendered).toContain('LINQ Operations');
    expect(rendered).toContain('Focus areas');
    expect(rendered).toContain('High');                                  // backend priority
    expect(rendered).toContain('Your study plan');
    expect(rendered).toContain('Exception fundamentals');
    expect(rendered).toContain('Review try/catch/finally');
  });

  it('shows a loading state while the coach is thinking', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What should I study next?';
    component.send();
    fixture.detectChanges();

    expect(text()).toContain('Analysing your completed assessments');

    httpMock.expectOne(`${baseUrl}/ask`).flush(coachAnswer);
  });

  it('sends bounded history so follow-ups keep their thread', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What are my weakest topics?';
    component.send();
    httpMock.expectOne(`${baseUrl}/ask`).flush(coachAnswer);

    component.draft = 'Now make it a 7-day plan';
    component.send();

    const second = httpMock.expectOne(`${baseUrl}/ask`);
    const body = second.request.body as { history: { question: string; answer: string }[] };
    expect(body.history.length).toBe(1);
    expect(body.history[0].question).toBe('What are my weakest topics?');
    // The coach's SUMMARY is echoed back, not the whole answer — cheaper and sufficient.
    expect(body.history[0].answer).toBe('Focus first on exception handling.');
    second.flush(coachAnswer);
  });

  // ── No-data state ───────────────────────────────────────────────────

  it('shows the no-data state when nothing has been published yet', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(emptySnapshot);
    fixture.detectChanges();

    expect(text()).toContain('Complete an exam and wait for your results to be published');
    // And the suggested prompts switch to platform questions, since there is nothing to coach on.
    expect(component.suggestedPrompts).toContain("Why can't I see my result yet?");
  });

  // ── Failure states ──────────────────────────────────────────────────

  it('shows a graceful unavailable state on 503 and offers a retry', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What should I study next?';
    component.send();

    httpMock.expectOne(`${baseUrl}/ask`).flush(
      {
        status: 503,
        error:
          'The AI Learning Coach is temporarily unavailable. Your exam data and existing analytics are unaffected. Please try again shortly.',
      },
      { status: 503, statusText: 'Service Unavailable' }
    );
    fixture.detectChanges();

    expect(text()).toContain('temporarily unavailable');
    expect(text()).toContain('unaffected');
    expect(text()).toContain('Try again');
    expect(component.isSending).toBe(false);
  });

  it('shows a retry message on 429', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What should I study next?';
    component.send();
    httpMock
      .expectOne(`${baseUrl}/ask`)
      .flush({ status: 429, error: 'rate limited' }, { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();

    expect(text()).toContain('too quickly');
  });

  it('never renders a raw backend error', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What should I study next?';
    component.send();
    httpMock
      .expectOne(`${baseUrl}/ask`)
      .flush('NullReferenceException at CoachController.line42', {
        status: 500,
        statusText: 'Server Error',
      });
    fixture.detectChanges();

    expect(text()).not.toContain('NullReferenceException');
    expect(text()).toContain('Something went wrong');
  });

  it('retry resends the failed question without duplicating it in the transcript', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.draft = 'What should I study next?';
    component.send();
    httpMock
      .expectOne(`${baseUrl}/ask`)
      .flush({ status: 503, error: 'unavailable' }, { status: 503, statusText: 'Service Unavailable' });

    component.retry();
    httpMock.expectOne(`${baseUrl}/ask`).flush(coachAnswer);
    fixture.detectChanges();

    const questions = component.messages.filter((m) => m.role === 'user');
    expect(questions.length).toBe(1);
    expect(component.messages.filter((m) => m.role === 'coach').length).toBe(1);
  });

  // ── Suggested prompts ───────────────────────────────────────────────

  it('a suggested prompt submits immediately', () => {
    createComponent('Candidate');
    httpMock.expectOne(`${baseUrl}/snapshot`).flush(snapshotWithData);

    component.usePrompt('What are my weakest topics?');

    const req = httpMock.expectOne(`${baseUrl}/ask`);
    expect((req.request.body as { message: string }).message).toBe('What are my weakest topics?');
    req.flush(coachAnswer);
  });
});
