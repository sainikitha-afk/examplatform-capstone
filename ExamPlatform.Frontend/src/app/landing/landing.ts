// landing.ts — Public landing page shown at the root "/" URL.
// No auth required. Showcases the platform with hero, features,
// stats, testimonials, FAQ accordion, and a call-to-action footer.
// CONCEPT DEMO: signals for counter animation, (click) event binding,
//   *ngFor, *ngIf, [ngClass], and routerLink.

import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface Feature {
  icon: string;
  title: string;
  description: string;
}

interface Testimonial {
  name: string;
  role: string;
  avatar: string;
  text: string;
}

interface FaqItem {
  question: string;
  answer: string;
  open: boolean;
}

@Component({
  selector: 'app-landing',
  imports: [CommonModule, RouterModule],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements OnInit, OnDestroy {

  // ── Animated counters ────────────────────────────────────────────
  // Signals hold the current displayed value; they increment until
  // they reach the target so the user sees a counting animation.
  totalExams     = signal(0);
  totalCandidates = signal(0);
  passRate        = signal(0);
  totalCerts      = signal(0);

  private counterInterval: ReturnType<typeof setInterval> | null = null;

  // ── Features grid ────────────────────────────────────────────────
  features: Feature[] = [
    {
      icon: 'bi-shield-lock',
      title: 'Secure & Reliable',
      description: 'JWT-authenticated sessions, role-based access, and server-authoritative timer prevent cheating.'
    },
    {
      icon: 'bi-lightning-charge',
      title: 'Real-time Auto-save',
      description: 'Answers are saved automatically as you type so you never lose progress due to connectivity issues.'
    },
    {
      icon: 'bi-award',
      title: 'Digital Certificates',
      description: 'Earn verifiable certificates upon passing. Download or share them from your personal certificate vault.'
    },
    {
      icon: 'bi-bar-chart-line',
      title: 'Deep Analytics',
      description: 'Track your pass rate, average score, and best-per-exam performance with a personal analytics dashboard.'
    },
    {
      icon: 'bi-people',
      title: 'Multi-role Platform',
      description: 'Candidates take exams; Examiners build question banks and publish results; Admins oversee the entire platform.'
    },
    {
      icon: 'bi-file-earmark-spreadsheet',
      title: 'Bulk Question Import',
      description: 'Examiners can upload hundreds of MCQ questions at once via Excel, saving hours of manual entry.'
    },
  ];

  // ── Testimonials carousel ────────────────────────────────────────
  testimonials: Testimonial[] = [
    {
      name: 'Priya Mehta',
      role: 'Software Engineering Candidate',
      avatar: 'PM',
      text: 'ExamHub made my certification process seamless. The countdown timer and question navigator kept me organised throughout the exam.'
    },
    {
      name: 'Dr. Rajan Iyer',
      role: 'University Examiner',
      avatar: 'RI',
      text: 'Bulk question import saved me hours. I had 200 MCQs ready in Excel — uploaded in seconds. Publishing results to all candidates in one click is fantastic.'
    },
    {
      name: 'Sneha Kapoor',
      role: 'HR Admin',
      avatar: 'SK',
      text: 'The admin dashboard gives a live overview of the entire platform. Managing examiners and reviewing platform stats has never been this intuitive.'
    },
  ];

  activeTestimonial = signal(0);
  private testimonialInterval: ReturnType<typeof setInterval> | null = null;

  // ── FAQ ──────────────────────────────────────────────────────────
  faqs: FaqItem[] = [
    {
      question: 'How do I register as a candidate?',
      answer: 'Click "Get Started" and fill in the registration form. Your account is activated immediately — no email verification needed.',
      open: true
    },
    {
      question: 'Can I pause an exam mid-way?',
      answer: 'Yes. Your progress is auto-saved continuously. If you close the browser, simply revisit the exam page to resume from exactly where you left off, with the remaining time preserved.',
      open: false
    },
    {
      question: 'How are certificates generated?',
      answer: 'Once an examiner publishes results, the system automatically evaluates all submissions. Candidates who meet the passing mark receive a unique certificate code instantly.',
      open: false
    },
    {
      question: 'How do I become an examiner?',
      answer: 'Submit an examiner request from the Register page. An admin will review your credentials and, upon approval, will send your account credentials to your email.',
      open: false
    },
    {
      question: 'What question formats are supported?',
      answer: 'Currently, Multiple-Choice Questions (MCQ) with one correct answer among four options. Each question carries customisable marks and a difficulty rating.',
      open: false
    },
  ];

  ngOnInit(): void {
    // Start counter animation 400 ms after the component loads.
    setTimeout(() => this.startCounters(), 400);

    // Auto-advance testimonials every 5 s.
    this.testimonialInterval = setInterval(() => {
      this.activeTestimonial.update(i => (i + 1) % this.testimonials.length);
    }, 5000);
  }

  ngOnDestroy(): void {
    if (this.counterInterval) clearInterval(this.counterInterval);
    if (this.testimonialInterval) clearInterval(this.testimonialInterval);
  }

  // Animate all counters simultaneously by incrementing them in one interval.
  private startCounters(): void {
    const targets = { exams: 120, candidates: 3400, pass: 87, certs: 2100 };
    const steps   = 60;
    let step = 0;
    this.counterInterval = setInterval(() => {
      step++;
      const pct = step / steps;
      this.totalExams.set(Math.round(targets.exams * pct));
      this.totalCandidates.set(Math.round(targets.candidates * pct));
      this.passRate.set(Math.round(targets.pass * pct));
      this.totalCerts.set(Math.round(targets.certs * pct));
      if (step >= steps) clearInterval(this.counterInterval!);
    }, 20);
  }

  // Toggle a FAQ accordion item open/closed.
  toggleFaq(index: number): void {
    this.faqs[index].open = !this.faqs[index].open;
  }

  // Manually switch testimonial.
  setTestimonial(index: number): void {
    this.activeTestimonial.set(index);
  }
}
