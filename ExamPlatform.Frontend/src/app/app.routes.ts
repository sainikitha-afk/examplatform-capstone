// app.routes.ts — Defines every URL path in the app and which component renders it.
// CONCEPTS DEMO:
//   - canActivate guards (authGuard, roleGuard)
//   - route data: { role } used by roleGuard
//   - loadComponent — lazy loading for attempt-exam (biggest screen)
//   - route parameters (:examId, :attemptId)

import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';

// Eagerly-loaded components (imported statically — small screens).
import { Landing } from './landing/landing';
import { Login } from './login/login';
import { Register } from './register/register';
import { CandidateDashboard } from './candidate-dashboard/candidate-dashboard';
import { ExaminerDashboard } from './examiner-dashboard/examiner-dashboard';
import { AdminDashboard } from './admin-dashboard/admin-dashboard';
import { BrowseExams } from './browse-exams/browse-exams';
import { ExamDetails } from './exam-details/exam-details';
import { ExamResult } from './exam-result/exam-result';
import { CandidateAnalytics } from './candidate-analytics/candidate-analytics';
import { Certificates } from './certificates/certificates';
import { Notifications } from './notifications/notifications';
import { ManageQuestions } from './manage-questions/manage-questions';
import { ManageExams } from './manage-exams/manage-exams';
import { PublishResults } from './publish-results/publish-results';
import { IssueReports } from './issue-reports/issue-reports';
import { Categories } from './categories/categories';
import { ManageUsers } from './manage-users/manage-users';
import { ManageExaminers } from './manage-examiners/manage-examiners';
import { ExaminerRequest } from './examiner-request/examiner-request';
import { AdminExaminerRequests } from './admin-examiner-requests/admin-examiner-requests';

export const routes: Routes = [

  // ── Public routes (no guard) ──────────────────────────────────────────
  { path: '',       component: Landing,   pathMatch: 'full' },
  { path: 'login',    component: Login },
  { path: 'register', component: Register },
  { path: 'apply-as-examiner', component: ExaminerRequest },

  // ── Candidate routes ──────────────────────────────────────────────────
  {
    path: 'candidate',
    component: CandidateDashboard,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Candidate' }
  },
  {
    path: 'browse-exams',
    component: BrowseExams,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Candidate' }
  },
  {
    // Route parameter :examId — read inside exam-details.ts via ActivatedRoute.
    path: 'exam-details/:examId',
    component: ExamDetails,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Candidate' }
  },
  {
    // CONCEPT DEMO: lazy loading — loadComponent defers the bundle until the route is hit.
    path: 'attempt/:examId',
    loadComponent: () => import('./attempt-exam/attempt-exam').then(m => m.AttemptExam),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Candidate' }
  },
  {
    // Route parameter :attemptId — read in exam-result.ts.
    path: 'result/:attemptId',
    component: ExamResult,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Candidate' }
  },
  {
    path: 'my-analytics',
    component: CandidateAnalytics,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Candidate' }
  },
  {
    path: 'my-certificates',
    component: Certificates,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Candidate' }
  },

  // ── Shared (all authenticated users) ────────────────────────────────
  {
    path: 'notifications',
    component: Notifications,
    canActivate: [authGuard]
  },

  // ── Examiner routes ──────────────────────────────────────────────────
  {
    path: 'examiner',
    component: ExaminerDashboard,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Examiner' }
  },
  {
    path: 'manage-questions',
    component: ManageQuestions,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Examiner' }
  },
  {
    path: 'manage-exams',
    component: ManageExams,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Examiner' }
  },
  {
    path: 'publish-results',
    component: PublishResults,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Examiner' }
  },
  {
    path: 'issue-reports',
    component: IssueReports,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Examiner' }
  },
  {
    // Examiner monitoring: every attempt on one exam (:examId route parameter).
    path: 'exam-attempts/:examId',
    loadComponent: () => import('./exam-attempts/exam-attempts').then(m => m.ExamAttempts),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Examiner' }
  },

  // ── Admin routes ─────────────────────────────────────────────────────
  {
    path: 'admin',
    component: AdminDashboard,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'admin/categories',
    component: Categories,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'admin/users',
    component: ManageUsers,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'admin/examiners',
    component: ManageExaminers,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'admin/examiner-requests',
    component: AdminExaminerRequests,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },

  // ── Wildcard ──────────────────────────────────────────────────────────
  { path: '**', redirectTo: '/' },
];
