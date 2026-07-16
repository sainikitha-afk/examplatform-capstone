// ai-coach.ts — the ExamHub AI Learning Coach screen.
//
// ROLE-AWARE BY DESIGN. The route only requires authentication, because the coach has two
// modes and they are not both candidate-only:
//   • Candidate → personalized coaching, grounded in their own published results.
//   • Examiner / Admin → general platform guidance only. They are told plainly that
//     personalized coaching is candidate-only rather than being silently treated as a
//     candidate or shown a locked door. The backend enforces this too (403 on /ask).
//
// STATE: component-local + RxJS, matching the rest of this app. NgRx is used here only for
// the attempt slice, and a stateless coach does not justify a new store. The transcript
// lives in memory ONLY — it is never written to sessionStorage/localStorage, because it
// discusses the candidate's performance.

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CoachApiService } from '../services/coachapi.service';
import {
  CoachAnswerModel,
  CoachMessage,
  CoachSnapshotModel,
  CoachTurn,
} from '../models/coach.model';

@Component({
  selector: 'app-ai-coach',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './ai-coach.html',
  styleUrl: './ai-coach.css',
})
export class AiCoach implements OnInit {
  // Mirrors the backend's own limit so the counter and the server agree.
  readonly maxMessageLength = 500;

  // Only the last two exchanges are sent back for continuity — enough for "make that seven
  // days instead", small enough to keep the prompt (and its cost) bounded.
  private readonly maxHistoryTurns = 2;

  role = '';
  isCandidate = false;

  snapshot: CoachSnapshotModel | null = null;
  isLoadingSnapshot = false;

  messages: CoachMessage[] = [];
  draft = '';

  isSending = false;
  errorMessage = '';

  // Kept so the "Try again" button can resend exactly what failed.
  private lastFailedMessage = '';

  constructor(private coachService: CoachApiService) {}

  ngOnInit(): void {
    // Role comes from the session the same way the guards and navigation read it.
    // It is a display concern here — the backend re-derives the real role from the JWT.
    this.role = sessionStorage.getItem('role') ?? '';
    this.isCandidate = this.role === 'Candidate';

    if (this.isCandidate) {
      this.loadSnapshot();
    }
  }

  // The deterministic summary. No AI call server-side, so this is cheap — it powers the
  // welcome panel and tells us whether to show the no-data state.
  private loadSnapshot(): void {
    this.isLoadingSnapshot = true;
    this.coachService.loadSnapshot().subscribe({
      next: (data) => {
        this.snapshot = data;
        this.isLoadingSnapshot = false;
      },
      error: (err: Error) => {
        // A snapshot failure must not block the screen — the candidate can still ask
        // questions, and /ask will surface any real problem.
        this.errorMessage = err.message;
        this.isLoadingSnapshot = false;
      },
    });
  }

  // ── Suggested prompts ────────────────────────────────────────────────
  // Candidates with results get performance prompts; everyone else gets platform prompts.
  get suggestedPrompts(): string[] {
    if (!this.isCandidate) {
      return [
        'How do I publish exam results?',
        'What does the tab-switch count mean?',
        'How do examiner requests work?',
      ];
    }

    if (!this.snapshot?.hasData) {
      return [
        'How do I browse available exams?',
        "Why can't I see my result yet?",
        'How do certificates work?',
      ];
    }

    return [
      'What are my weakest topics?',
      'What should I study next?',
      'Create a 3-day study plan',
      'Explain my performance in simple language',
    ];
  }

  usePrompt(prompt: string): void {
    if (this.isSending) return;
    this.draft = prompt;
    this.send();
  }

  // ── Validation ───────────────────────────────────────────────────────

  get trimmedDraft(): string {
    return this.draft.trim();
  }

  get isTooLong(): boolean {
    return this.trimmedDraft.length > this.maxMessageLength;
  }

  // Blank and whitespace-only are both rejected; a request already in flight blocks a second
  // one (no double-submit, no duplicate spend).
  get canSend(): boolean {
    return !this.isSending && this.trimmedDraft.length > 0 && !this.isTooLong;
  }

  // ── Sending ──────────────────────────────────────────────────────────

  send(): void {
    if (!this.canSend) return;

    const question = this.trimmedDraft;
    this.draft = '';
    this.errorMessage = '';
    this.lastFailedMessage = '';
    this.isSending = true;

    this.messages.push({ role: 'user', text: question });

    // Candidates get grounded coaching; Examiners/Admins get platform guidance.
    const request$ = this.isCandidate
      ? this.coachService.ask(question, this.buildHistory())
      : this.coachService.help(question, this.buildHistory());

    request$.subscribe({
      next: (result: CoachAnswerModel) => {
        this.messages.push({ role: 'coach', text: result.answer, result });
        this.isSending = false;
      },
      error: (err: Error) => {
        // The failed question stays visible in the transcript, and "Try again" resends it.
        this.errorMessage = err.message;
        this.lastFailedMessage = question;
        this.isSending = false;
      },
    });
  }

  retry(): void {
    if (this.isSending || !this.lastFailedMessage) return;

    // Drop the unanswered question — send() re-adds it — so the transcript doesn't duplicate.
    const last = this.messages[this.messages.length - 1];
    if (last?.role === 'user') this.messages.pop();

    this.draft = this.lastFailedMessage;
    this.send();
  }

  // Enter sends; Shift+Enter inserts a newline.
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  // The last couple of answered exchanges, summarised. We send the coach's SUMMARY rather
  // than its full answer — it is what the model needs for continuity, and it keeps the
  // outgoing payload small.
  private buildHistory(): CoachTurn[] {
    const turns: CoachTurn[] = [];

    for (let i = 0; i < this.messages.length - 1; i++) {
      const question = this.messages[i];
      const answer = this.messages[i + 1];

      if (question.role === 'user' && answer.role === 'coach') {
        turns.push({
          question: question.text,
          answer: answer.result?.summary || answer.text,
        });
      }
    }

    return turns.slice(-this.maxHistoryTurns);
  }

  // Splits the answer into paragraphs for readable rendering. The template interpolates each
  // one, so any stray markup from the model renders as literal text, never as HTML.
  paragraphs(text: string): string[] {
    return text.split('\n').filter((p) => p.trim().length > 0);
  }

  priorityClass(priority: string): string {
    switch (priority) {
      case 'High':
        return 'priority-high';
      case 'Medium':
        return 'priority-medium';
      default:
        return 'priority-low';
    }
  }
}
