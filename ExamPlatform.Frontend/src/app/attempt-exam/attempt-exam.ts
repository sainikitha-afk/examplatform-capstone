// attempt-exam.ts — The live exam-taking screen (lazy-loaded for performance).
// Why it exists: this is the most complex screen; it manages a real-time countdown,
//   auto-saves answers, and orchestrates the question navigator.
// How Angular/NgRx uses it: dispatches NgRx actions; selectors feed the template.
// CONCEPTS DEMO:
//   - NgRx Store: dispatching actions, selecting state
//   - signal(): timeDisplay and answeredCount update reactively each second
//   - setInterval + ngOnDestroy cleanup (prevents memory leak)
//   - @Input/@Output with QuestionCard child component
//   - [ngClass] on question navigator squares
//   - Subscription cleanup in ngOnDestroy

import { Component, OnInit, OnDestroy, HostListener, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable, Subscription } from 'rxjs';
import { QuestionCard } from '../question-card/question-card';
import { AttemptActions } from '../store/attempt.actions';
import {
  selectAttemptId, selectExamTitle, selectQuestions,
  selectCurrentIndex, selectAnswers, selectTimeRemaining,
  selectAttemptStatus, selectErrorMessage,
  selectIsOffline, selectPendingSyncCount
} from '../store/attempt.selectors';
import { QuestionInAttemptModel } from '../models/question.model';
import { AttemptApiService } from '../services/attemptapi.service';
import { IssueReportApiService } from '../services/issuereportapi.service';
import { CreateIssueReportModel } from '../models/issue-report.model';

@Component({
  selector: 'app-attempt-exam',
  imports: [CommonModule, FormsModule, QuestionCard],
  templateUrl: './attempt-exam.html',
  styleUrl: './attempt-exam.css',
})
export class AttemptExam implements OnInit, OnDestroy {

  // ── NgRx Observables — assigned in constructor (not as field initialisers)
  // because 'store' is not available until after the constructor runs.
  examTitle$:    Observable<string>;
  questions$:    Observable<QuestionInAttemptModel[]>;
  currentIndex$: Observable<number>;
  answers$:      Observable<Record<number, number | null>>;
  status$:       Observable<string>;
  errorMessage$: Observable<string>;

  // ── Signals — component-local reactive values; template reads with ().
  // CONCEPT DEMO: signal() — updated every second by the timer interval.
  timeDisplay  = signal(0);
  answeredCount = signal(0);

  // Connection-resilience state: browser offline OR server unreachable, plus
  // how many answers are still waiting to be confirmed by the server.
  isOffline        = signal(!navigator.onLine);
  pendingSyncCount = signal(0);

  status: string = 'idle';

  // Snapshots kept for use in methods (store values don't need to be re-selected each time).
  questions: QuestionInAttemptModel[] = [];
  currentIndex: number = 0;
  answers: Record<number, number | null> = {};
  attemptId: number = 0;
  examId: number = 0;

  private timerHandle: ReturnType<typeof setInterval> | null = null;
  private subs: Subscription[] = [];

  // ── Flag / "Report a problem" modal state ──────────────────────────────
  showFlagModal = false;
  flagAboutQuestion = true;   // checkbox: is this about the current question?
  flagMessage = '';
  isSubmittingFlag = false;
  flagError = '';
  flagSuccess = false;

  // ── Proctoring: tab-switch warnings ────────────────────────────────────
  // How many times the candidate has left the exam tab/window this attempt.
  tabSwitchCount = signal(0);
  // Whether the "come back to your exam" warning overlay is showing.
  showTabWarning = signal(false);
  // The threshold the backend also uses — kept in sync for the message copy.
  readonly tabSwitchLimit = 3;
  // Debounce so a single tab switch that fires multiple browser events
  // (visibilitychange + blur) is only counted once.
  private lastTabSwitchAt = 0;

  constructor(
    private route: ActivatedRoute,
    private store: Store,
    private attemptApi: AttemptApiService,
    private issueApi: IssueReportApiService
  ) {
    // Assign Observables here — store is available after super() in the constructor.
    this.examTitle$    = this.store.select(selectExamTitle);
    this.questions$    = this.store.select(selectQuestions);
    this.currentIndex$ = this.store.select(selectCurrentIndex);
    this.answers$      = this.store.select(selectAnswers);
    this.status$       = this.store.select(selectAttemptStatus);
    this.errorMessage$ = this.store.select(selectErrorMessage);
  }

  ngOnInit(): void {
    // Read examId from the route parameter :examId.
    this.examId = Number(this.route.snapshot.params['examId']);
    this.store.dispatch(AttemptActions.startAttempt({ examId: this.examId }));

    // Subscribe to store slices and keep local snapshots for methods.
    this.subs.push(
      this.store.select(selectAttemptId).subscribe(id => this.attemptId = id),

      this.store.select(selectQuestions).subscribe(qs => this.questions = qs),

      this.store.select(selectCurrentIndex).subscribe(i => this.currentIndex = i),

      this.store.select(selectAnswers).subscribe(a => {
        this.answers = a;
        // CONCEPT DEMO: signal.set() — reactively update the answered count.
        this.answeredCount.set(Object.values(a).filter(v => v != null).length);
      }),

      // Seed the timer signal from the server-returned remainingSeconds, then start ticking locally.
      this.store.select(selectTimeRemaining).subscribe(t => {
        if (!this.timerHandle && t > 0) {
          this.timeDisplay.set(t);
          this.startTimer();
        }
      }),

      this.store.select(selectAttemptStatus).subscribe(s => {
        this.status = s;
        if (s === 'submitted') this.clearTimer();
      }),

      // Connection resilience: reflect sync state in the offline banner.
      this.store.select(selectIsOffline).subscribe(off => {
        if (off) this.isOffline.set(true);
        else if (navigator.onLine) this.isOffline.set(false);
      }),

      this.store.select(selectPendingSyncCount).subscribe(n => this.pendingSyncCount.set(n))
    );
  }

  // ── Connection listeners ────────────────────────────────────────────
  // When the browser regains connectivity, immediately re-send every answer
  // that failed to save while offline. The exam continues uninterrupted.
  @HostListener('window:online')
  handleOnline(): void {
    this.isOffline.set(false);
    this.store.dispatch(AttemptActions.syncPendingAnswers());
  }

  @HostListener('window:offline')
  handleOffline(): void {
    this.isOffline.set(true);
  }

  // Warn before leaving mid-exam (refresh/close). The attempt is resumable —
  // answers are saved — but the timer keeps running server-side.
  @HostListener('window:beforeunload', ['$event'])
  handleBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.status === 'active' || this.status === 'submitting') {
      event.preventDefault();
    }
  }

  // ── Proctoring: detect leaving the exam tab/window ─────────────────────
  // visibilitychange is the single most reliable signal (fires on tab switch,
  // window minimise, and OS app switch). We act only when the page becomes
  // hidden during an active attempt. A short debounce collapses duplicate
  // events from one physical switch into a single recorded violation.
  @HostListener('document:visibilitychange')
  handleVisibilityChange(): void {
    if (document.hidden) this.registerTabSwitch();
  }

  private registerTabSwitch(): void {
    // Only while the exam is actually running.
    if (this.status !== 'active' || !this.attemptId) return;

    const now = Date.now();
    if (now - this.lastTabSwitchAt < 1000) return; // ignore duplicate events
    this.lastTabSwitchAt = now;

    // Optimistically bump the local counter so the warning shows instantly,
    // then reconcile with the server's authoritative count.
    this.tabSwitchCount.update(c => c + 1);
    this.showTabWarning.set(true);

    this.attemptApi.recordTabSwitch(this.attemptId).subscribe({
      next: res => this.tabSwitchCount.set(res.tabSwitchCount),
      // A failed record must never disrupt the exam — the warning still shows.
      error: () => {}
    });
  }

  // Candidate acknowledges the tab-switch warning and returns to the exam.
  dismissTabWarning(): void {
    this.showTabWarning.set(false);
  }

  // ── Flag / "Report a problem" ──────────────────────────────────────────
  openFlagModal(): void {
    // Default the "about this question" checkbox on only if there's a current
    // question to attach it to.
    this.flagAboutQuestion = !!this.questions[this.currentIndex];
    this.flagMessage = '';
    this.flagError = '';
    this.flagSuccess = false;
    this.showFlagModal = true;
  }

  closeFlagModal(): void {
    this.showFlagModal = false;
  }

  submitFlag(): void {
    const message = this.flagMessage.trim();
    if (message.length < 3) {
      this.flagError = 'Please describe the problem in a few words.';
      return;
    }

    const currentQ = this.questions[this.currentIndex];
    const questionId = this.flagAboutQuestion && currentQ ? currentQ.questionId : null;

    const payload = new CreateIssueReportModel(
      this.examId, this.attemptId || null, questionId, message);

    this.isSubmittingFlag = true;
    this.flagError = '';
    this.issueApi.raiseIssue(payload).subscribe({
      next: () => {
        this.isSubmittingFlag = false;
        this.flagSuccess = true;
        // Auto-close shortly after showing the success confirmation.
        setTimeout(() => this.closeFlagModal(), 1800);
      },
      error: err => {
        this.isSubmittingFlag = false;
        this.flagError = err?.error?.error
          || 'Could not send your report. Please try again.';
      }
    });
  }

  // Manual "Sync now" from the offline banner.
  handleSyncNowClick(): void {
    this.store.dispatch(AttemptActions.syncPendingAnswers());
  }

  // Retry loading the exam after a failed start (e.g. connection dropped).
  handleRetryStartClick(): void {
    this.store.dispatch(AttemptActions.startAttempt({ examId: this.examId }));
  }

  private startTimer(): void {
    this.timerHandle = setInterval(() => {
      // CONCEPT DEMO: signal.update() — decrements displayed time each second.
      this.timeDisplay.update(t => Math.max(0, t - 1));
      this.store.dispatch(AttemptActions.tickTimer());

      if (this.timeDisplay() <= 0) {
        this.clearTimer();
        this.handleAutoSubmit();
      }
    }, 1000);
  }

  private clearTimer(): void {
    if (this.timerHandle) {
      clearInterval(this.timerHandle);
      this.timerHandle = null;
    }
  }

  // Formats seconds as MM:SS for display.
  formatTime(seconds: number): string {
    const m = Math.floor(seconds / 60).toString().padStart(2, '0');
    const s = (seconds % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  }

  // Called by question navigator clicks.
  handleNavigateToQuestion(index: number): void {
    this.store.dispatch(AttemptActions.setCurrentQuestion({ index }));
  }

  // Called when QuestionCard emits answerSelected (@Output from child component).
  handleAnswerSelected(event: { questionId: number; optionId: number }): void {
    this.store.dispatch(AttemptActions.saveAnswer({
      questionId: event.questionId,
      optionId: event.optionId
    }));
  }

  handleSubmitClick(): void {
    if (!confirm('Submit your exam? You cannot change answers after submission.')) return;
    this.clearTimer();
    this.store.dispatch(AttemptActions.submitAttempt({ attemptId: this.attemptId }));
  }

  private handleAutoSubmit(): void {
    alert('Time is up! Your exam has been submitted automatically.');
    this.store.dispatch(AttemptActions.submitAttempt({ attemptId: this.attemptId }));
  }

  // Returns the CSS class for each question navigator square based on its state.
  getNavSquareClass(questionId: number, index: number): string {
    if (index === this.currentIndex) return 'btn btn-primary btn-sm';
    if (this.answers[questionId] != null) return 'btn btn-success btn-sm';
    return 'btn btn-outline-secondary btn-sm';
  }

  // CONCEPT DEMO: ngOnDestroy — clears interval and unsubscribes to prevent memory leaks.
  ngOnDestroy(): void {
    this.clearTimer();
    this.subs.forEach(s => s.unsubscribe());
    this.store.dispatch(AttemptActions.resetAttempt());
  }
}
