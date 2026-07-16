// candidate-dashboard.spec.ts — the redesigned action-oriented dashboard.
//
// The interesting behaviours: the resume banner appears only when an attempt is actually
// in progress; a submitted-but-unpublished attempt reads "Results pending" (never a score);
// and the AI insight card renders from the deterministic snapshot with an honest fallback
// when there is nothing to coach on.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { CandidateDashboard } from './candidate-dashboard';

describe('CandidateDashboard', () => {
  let fixture: ComponentFixture<CandidateDashboard>;
  let component: CandidateDashboard;
  let httpMock: HttpTestingController;

  const attemptsUrl = 'http://localhost:5014/api/attempts/mine';
  const snapshotUrl = 'http://localhost:5014/api/coach/snapshot';

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

  const emptySnapshot = { ...snapshotWithData, hasData: false, totalAttempts: 0, topics: [], strongTopics: [], focusAreas: [] };

  function attempt(overrides: Record<string, unknown> = {}) {
    return {
      attemptId: 1,
      examId: 10,
      examTitle: 'C# Certification Assessment',
      startedAt: '2026-07-10T09:00:00Z',
      submittedAt: '2026-07-10T10:00:00Z',
      attemptStatus: 'Submitted',
      score: 40,
      isPassed: true,
      topicScoresUrl: null,
      ...overrides,
    };
  }

  function create(attempts: object[], snapshot: object = snapshotWithData): void {
    fixture = TestBed.createComponent(CandidateDashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();   // ngOnInit fires both requests

    httpMock.expectOne(r => r.url === attemptsUrl).flush({ items: attempts, totalCount: attempts.length });
    httpMock.expectOne(snapshotUrl).flush(snapshot);
    fixture.detectChanges();
  }

  function text(): string {
    return fixture.nativeElement.textContent as string;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CandidateDashboard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
    sessionStorage.setItem('fullName', 'Diya');
  });

  afterEach(() => {
    sessionStorage.clear();
    httpMock.verify();
  });

  it('shows the resume banner when an attempt is in progress', () => {
    create([attempt({ attemptStatus: 'InProgress', submittedAt: null, score: null, isPassed: null })]);

    expect(text()).toContain('You have an exam in progress');
    expect(text()).toContain('Continue Exam');
  });

  it('hides the resume banner when nothing is in progress', () => {
    create([attempt()]);

    expect(text()).not.toContain('You have an exam in progress');
  });

  it('reads "Results pending" for a submitted attempt whose results are not published', () => {
    // The backend withholds score/isPassed until the examiner publishes results.
    create([attempt({ score: null, isPassed: null })]);

    expect(text()).toContain('Results pending');
    expect(text()).not.toContain('Passed');
  });

  it('shows pass and fail pills with the score once results are out', () => {
    create([
      attempt({ attemptId: 1, score: 40, isPassed: true }),
      attempt({ attemptId: 2, examTitle: 'General', score: 10, isPassed: false }),
    ]);

    expect(text()).toContain('Passed · 40');
    expect(text()).toContain('Failed · 10');
  });

  it('renders the AI insight card with strengths and focus areas from the snapshot', () => {
    create([attempt()]);

    expect(text()).toContain('Your AI Coach insight');
    expect(text()).toContain('LINQ Operations');
    expect(text()).toContain('Exception Handling');
    expect(text()).toContain('Get a study plan');
  });

  it('falls back to an honest empty insight when no results are published yet', () => {
    create([], emptySnapshot);

    expect(text()).toContain('your coach will');
    expect(text()).toContain('Find an exam to take');
  });

  it('shows the empty activity state for a brand-new candidate', () => {
    create([], emptySnapshot);

    expect(text()).toContain('No exams taken yet');
  });

  it('still renders activity when the insight request fails', () => {
    fixture = TestBed.createComponent(CandidateDashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();

    httpMock.expectOne(r => r.url === attemptsUrl).flush({ items: [attempt()], totalCount: 1 });
    httpMock.expectOne(snapshotUrl).flush({ error: 'down' }, { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();

    expect(text()).toContain('C# Certification Assessment');
    expect(component.isLoadingInsight()).toBe(false);
  });
});
