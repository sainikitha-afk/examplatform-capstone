# ExamHub — Online Examination Platform

A full-stack online examination platform where **Examiners** build question banks and publish timed exams, **Candidates** take those exams with auto-save and a server-authoritative timer, and **Admins** govern the whole platform — users, categories, examiner onboarding, and certificates.

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), Entity Framework Core, PostgreSQL |
| Frontend | Angular 21 (standalone components), Bootstrap 5.3 |
| Auth | JWT bearer tokens, BCrypt password hashing, role-based authorization |
| Email | MailKit (SMTP) with a no-op fallback for local dev |
| Excel | ClosedXML (bulk question import/export template) |
| Tests | NUnit + Moq + FluentAssertions (backend, 229 tests), Vitest + Angular TestBed (frontend, 66 tests) |

---

## Table of Contents

1. [Repository layout](#1-repository-layout)
2. [Architecture](#2-architecture)
3. [Features by role](#3-features-by-role)
4. [Business rules worth knowing](#4-business-rules-worth-knowing)
5. [Getting started from scratch](#5-getting-started-from-scratch)
6. [Database & migrations](#6-database--migrations)
7. [Creating the first Admin](#7-creating-the-first-admin)
8. [Configuration reference](#8-configuration-reference)
9. [API overview](#9-api-overview)
10. [Security design](#10-security-design)
11. [Concepts used (learning map)](#11-concepts-used-learning-map)
12. [Testing](#12-testing)
13. [Excel question import format](#13-excel-question-import-format)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. Repository layout

```
capstone/
├── ExamPlatform.Backend/
│   ├── ExamPlatform.API/          ← Web API: controllers, DTOs, middleware, Program.cs
│   ├── ExamPlatform.BLL/          ← Business Logic Layer: services, exceptions, email
│   ├── ExamPlatform.DAL/          ← Data Access Layer: DbContext, repositories, migrations
│   ├── ExamPlatform.Domain/       ← Entities and enums (no dependencies on anything)
│   ├── ExamPlatform.BLL.Tests/    ← NUnit test suite for every BLL service
│   └── exam_platform_backup.sql   ← Optional database backup/restore file
├── ExamPlatform.Frontend/         ← Angular 21 app (standalone components, Bootstrap 5)
│   └── src/app/
│       ├── models/                ← One class per API DTO (constructor-parameter style)
│       ├── services/              ← One service per API controller (+ .spec.ts tests)
│       ├── guards/                ← authGuard (login) + roleGuard (role match)
│       ├── interceptors/          ← JWT attach + global 401/429 handling
│       └── <one folder per screen> (login, register, manage-exams, attempt-exam, …)
├── TESTING_CHECKLIST.md           ← Step-by-step manual E2E test plan (all roles)
├── QuestionImportTemplate.xlsx    ← Sample Excel file for bulk question import
└── README.md                      ← You are here
```

### Backend layering (dependencies flow downward only)

```
API (HTTP, DTOs, [Authorize])
 └── BLL (business rules, validation, transactions, notifications, email)
      └── DAL (EF Core, repositories, migrations)
           └── Domain (entities + enums — pure C#, zero dependencies)
```

- **Controllers never touch the DbContext.** They parse the JWT, map DTOs, and call a service.
- **Services never build HTTP responses.** They throw typed exceptions (`ExamNotFoundException`, `ValidationException`, …) that `GlobalExceptionMiddleware` maps to proper HTTP status codes (404, 400, 409, …) with a consistent `{ status, error }` JSON body.
- **Repositories are the only EF Core surface.** Each aggregate (User, Exam, Question, Attempt, Certificate, Notification, ExaminerRequest, ExamCategory) has an interface in `DAL/Interfaces` — services depend on the interface, which is what makes them unit-testable with Moq.

---

## 2. Architecture

### Request lifecycle (example: candidate submits an exam)

```
Angular AttemptExam component
  → attemptapi.service.ts        POST /api/attempts/4/submit
    → auth.interceptor.ts        attaches "Authorization: Bearer <jwt>"
      → ASP.NET Core pipeline    GlobalExceptionMiddleware → Auth → [Authorize(Roles="Candidate")]
        → AttemptsController     extracts userId from JWT claims
          → AttemptService       ownership check → timer check → evaluate answers → score
            → AttemptRepository  one SaveChangesAsync = one implicit DB transaction
```

### Key backend design decisions

- **Server-authoritative timer.** The browser countdown is cosmetic. The server computes `remaining = duration − (UtcNow − startedAt)` on *every* attempt operation and auto-submits when it hits zero — refreshing, closing the tab, or hacking the client clock cannot buy extra time.
- **Explicit transactions only where they must span aggregates.** `PublishResultsAsync` (exam status + certificates + notifications) and `BulkCreateAsync` (questions + options) open a real transaction; everything else relies on EF Core's implicit transaction per `SaveChangesAsync`.
- **snake_case database.** A single convention block in `ExamPlatformContext` converts all PascalCase entity/property names to snake_case tables/columns (`Users.FullName` → `users.full_name`), the PostgreSQL norm.
- **All timestamps are UTC.** PostgreSQL `timestamptz` columns reject `Kind=Unspecified` values, so the frontend converts `datetime-local` input to UTC ISO before sending, and `ExamService.EnsureUtc` re-normalizes defensively on the way in.

### Frontend design decisions

- **Standalone components, no NgModules.** Every screen is one folder with `name.ts` / `name.html` / `name.css`.
- **Constructor injection everywhere** (never `inject()` in classes — functional guards/interceptors are the only exception because they have no constructor).
- **Models are classes** with public constructor parameters + defaults (no interfaces) — one model file per backend DTO.
- **One API service per controller** returning `Observable<T>`; components subscribe with `{ next, error }` — no async/await, no NgRx.
- **State is plain class properties.** The only shared reactive state is `currentUser$` (a `BehaviorSubject`) that the navigation bar subscribes to.

---

## 3. Features by role

### 🌐 Public (not logged in)
| Feature | Where | How it works |
|---|---|---|
| Landing page | `/` | Marketing-style hero, feature cards, role explainer |
| Candidate registration | `/register` | `POST /api/auth/register` — always creates a **Candidate**; returns a JWT immediately (no second login call) |
| Login | `/login` | `POST /api/auth/login` accepts email **or** username; response `{ token, userId, username, fullName, email, role }` routes the user to their role's dashboard |
| Apply as Examiner | `/apply-as-examiner` | Public form → `POST /api/examiner-requests`; an Admin later approves/rejects it |

### 🧑‍🎓 Candidate
| Feature | Where | How it works |
|---|---|---|
| Dashboard | `/candidate` | Stat cards (attempts, pass rate, certificates) + recent activity from `GET /api/analytics/me` |
| Browse exams | `/browse-exams` | Only **Published** exams, filterable by category (client-side filter) |
| Exam details | `/exam-details/:examId` | Duration, question count, passing marks, window — with a Start button |
| **Take an exam** | `/attempt/:examId` (lazy-loaded) | `POST /api/attempts/start` creates or **resumes** an attempt. Countdown timer seeded from server `remainingSeconds`. Every answer click fires `PUT /api/attempts/{id}/answers` (auto-save). Question navigator shows answered/current state. Manual submit or **auto-submit at 0:00** |
| Resume after crash | automatic | Refresh/reopen → the in-progress attempt is returned with all saved answers; the examiner gets an "AttemptResumed" alert |
| Results | `/result/:attemptId` | Visible only after the examiner publishes results; per-question breakdown with correct answers |
| My analytics | `/my-analytics` | Score trends, per-exam best scores |
| Certificates | `/my-certificates` | Auto-issued on pass when results are published (`CERT-YYYY-NNNNNN` code); downloadable; revocation reflected |
| Notifications | `/notifications` | New-exam announcements, results published, certificate issued/revoked; unread badge in navbar; mark read / mark all read |

### 🧑‍🏫 Examiner
| Feature | Where | How it works |
|---|---|---|
| Dashboard | `/examiner` | Question-bank stats (by difficulty/category), exam counts |
| Question bank | `/manage-questions` | MCQ CRUD — exactly 4 options, exactly 1 correct, marks > 0, duplicate detection. Server-side filters (category, difficulty, search) + pagination. **Soft delete** (blocked when the question is in a Published exam) |
| Excel import | `/manage-questions` | Upload `.xlsx` → `POST /api/questions/import` — per-row validation with continue-on-error; a downloadable template is provided |
| Exam builder | `/manage-exams` | Create Draft → attach/detach own questions → **publish**. Publish pre-conditions: ≥ 1 question, TotalMarks == sum of question marks, PassingMarks ≤ TotalMarks, StartTime < EndTime. Publishing **notifies every active candidate** |
| Attempt monitoring | `/exam-attempts/:examId` | Live view of every attempt: status, progress (answered count), scores, disconnect signals |
| Publish results | `/publish-results` | Transactional: exam → ResultsPublished, best attempt per candidate selected, certificates issued to passers, notifications + 3 email types sent (results / congratulations / certificate) |
| Unpublish | `/manage-exams` | Published → Draft rollback, allowed only if **zero** attempts exist |

### 🛡️ Admin
| Feature | Where | How it works |
|---|---|---|
| Dashboard | `/admin` | Platform-wide stats from `GET /api/analytics/platform` |
| Categories | `/admin/categories` | CRUD + bulk create; duplicate-name detection (case-insensitive) |
| Manage users | `/admin/users` | Paginated list; activate/deactivate any Candidate/Examiner (**Admins cannot be deactivated** — no button, plus a backend guard) |
| Create examiners | `/admin/examiners` | Direct examiner account creation |
| Examiner requests | `/admin/examiner-requests` | Approve (auto-generates a unique username + 12-char temp password, emails credentials) or reject (with an admin note, emailed) |
| Certificates | admin views | Revoke any certificate → candidate is notified in-app + by email |

---

## 4. Business rules worth knowing

These are deliberate product decisions — don't "fix" them as bugs:

1. **One attempt per candidate per exam.** Once a candidate submits (or is auto-submitted), that exam is closed to them forever. Enforced by `HasCompletedAttemptAsync` in `AttemptService.StartOrResumeAsync` plus a DB unique index on in-progress attempts.
2. **End time is optional when creating an exam.** If the examiner leaves it blank, the window defaults to **StartTime + 30 days** (applied in `ExamsController`).
3. **Late starters get the full duration.** A candidate who starts at 5:29 for a window ending at 5:30 is allowed in and gets the exam's entire duration — the server timer is based on *their start time*, never clamped to the window end. The window only gates **starting**, not finishing.
4. **Results are gated.** Candidates cannot see scores until the examiner explicitly publishes results.
5. **Question banks are private per examiner.** An examiner can only attach *their own* questions to an exam (Admins see everything).
6. **404 over 403 for ownership failures.** "Not found" and "not yours" return the same 404 so users can't probe for other people's resource IDs.
7. **Registration only creates Candidates.** Examiners come from Admin creation or request approval; Admins are bootstrapped manually (see §7).

---

## 5. Getting started from scratch

### Prerequisites

| Tool | Version | Check with |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` |
| Node.js | 20+ | `node --version` |
| PostgreSQL | 14+ | `psql --version` |
| Angular CLI | 21 | `npx ng version` (installed via npm below) |
| EF Core tools | any recent | `dotnet tool install --global dotnet-ef` |

### Step 1 — Database

Create the database (the app expects it to exist):

```bash
psql -U postgres -c "CREATE DATABASE exam_platform_db;"
```

Then point the backend at it. Open `ExamPlatform.Backend/ExamPlatform.API/appsettings.json` and edit:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=exam_platform_db;Username=postgres;Password=<your-password>"
}
```

### Step 2 — Apply migrations

```bash
cd ExamPlatform.Backend
dotnet ef database update --project ExamPlatform.DAL --startup-project ExamPlatform.API
```

This creates every table, index, and constraint (see §6). Alternatively, restore the included backup: `psql -U postgres -d exam_platform_db -f exam_platform_backup.sql`.

### Step 3 — Run the backend

```bash
cd ExamPlatform.Backend
dotnet run --project ExamPlatform.API
```

- API: **http://localhost:5014** (HTTP on purpose for local dev — no cert hassles)
- Swagger UI: **http://localhost:5014/swagger** (dev environment only)

### Step 4 — Run the frontend

```bash
cd ExamPlatform.Frontend
npm install
npx ng serve
```

- App: **http://localhost:4200**
- CORS is pre-configured on the backend for this origin (`AngularDev` policy).
- The API base URL is hardcoded as `http://localhost:5014/api` inside each file in `src/app/services/` — change it there if your backend runs elsewhere.

### Step 5 — Bootstrap the first Admin (see §7), then log in.

A sensible first session: log in as Admin → create 2–3 categories → create an examiner → log in as the examiner → create ~10 questions → create + publish an exam → register a candidate → take the exam. `TESTING_CHECKLIST.md` walks the entire platform in this order.

---

## 6. Database & migrations

### Schema at a glance (all snake_case)

| Table | Purpose | Notable constraints |
|---|---|---|
| `users` | All accounts (role column: `Admin`/`Examiner`/`Candidate` as **strings**) | unique email, unique username |
| `exam_categories` | Subject areas | |
| `questions` | MCQ bank, owned by an examiner | soft-delete flag `is_active` |
| `question_options` | 4 per question, exactly 1 `is_correct` | |
| `exams` | Draft → Published → ResultsPublished lifecycle | status stored as string |
| `exam_questions` | join table with `question_order` | unique (exam, question) |
| `attempts` | one row per exam sitting | **unique index: one InProgress attempt per (user, exam)** |
| `attempt_answers` | upserted on every auto-save; evaluated at submit | unique (attempt, question) |
| `certificates` | issued on pass at results-publish | unique (user, exam); code `CERT-YYYY-NNNNNN` |
| `notifications` | in-app inbox, typed by `NotificationType` | |
| `examiner_requests` | public applications, Pending/Approved/Rejected | |

All enums are stored as **strings** (`HasConversion<string>()`) so the database is human-readable — `SELECT role FROM users` returns `Admin`, not `0`.

### Existing migrations (chronological)

| Migration | What it did |
|---|---|
| `InitialCreate` | The full schema |
| `RemovePasswordSalt` | BCrypt embeds its own salt — the separate column was redundant |
| `AddAttemptInProgressUniqueIndex` | Enforces one in-progress attempt per (user, exam) at the DB level |
| `AddExaminerRequest` | The public examiner-application table |

### Migration commands you'll actually use

Run all of these from `ExamPlatform.Backend/`:

```bash
# Apply all pending migrations (the usual one)
dotnet ef database update --project ExamPlatform.DAL --startup-project ExamPlatform.API

# After changing an entity/configuration: create a new migration
dotnet ef migrations add <DescriptiveName> --project ExamPlatform.DAL --startup-project ExamPlatform.API

# Undo the last migration (only if not yet applied/shared)
dotnet ef migrations remove --project ExamPlatform.DAL --startup-project ExamPlatform.API

# See what's applied vs pending
dotnet ef migrations list --project ExamPlatform.DAL --startup-project ExamPlatform.API

# Nuke and rebuild the schema (dev only — destroys data!)
dotnet ef database drop --project ExamPlatform.DAL --startup-project ExamPlatform.API
dotnet ef database update --project ExamPlatform.DAL --startup-project ExamPlatform.API
```

The `--project` flag is needed because the `DbContext` lives in **DAL** while the runnable app (and connection string) live in **API**.

---

## 7. Creating the first Admin

There is **no database seeder** and registration always creates Candidates — this is intentional (no magic default credentials in the codebase). Bootstrap the first admin like this:

1. Register a normal account through the UI at `/register` (this stores a proper BCrypt password hash).
2. Promote it in the database:

```sql
UPDATE users SET role = 'Admin' WHERE username = '<your-username>';
```

3. Log out and log back in — the new JWT now carries the Admin role and routes to `/admin`.

Every subsequent examiner/admin can be created through the UI by this admin.

---

## 8. Configuration reference

All backend settings live in `ExamPlatform.Backend/ExamPlatform.API/appsettings.json`:

| Section | Key | Meaning |
|---|---|---|
| `ConnectionStrings` | `DefaultConnection` | PostgreSQL connection string |
| `Jwt` | `Issuer` / `Audience` | Must match what the token validator expects (defaults are fine) |
| `Jwt` | `Key` | HMAC signing secret — **replace for anything non-local** |
| `Jwt` | `ExpiryMinutes` | Token lifetime (480 = 8h; expiry triggers the frontend's "session expired" redirect) |
| `Smtp` | `Host`, `Port`, `Username`, `Password`, `EnableSsl` | Real email sending via MailKit. **If `Host` is empty, the app automatically uses `NoOpNotificationEmailService`**, which logs emails to the console instead — perfect for local dev, nothing to configure |
| `Logging` | | EF Core SQL logging is on in dev — you can watch every query |

> ⚠️ **Never commit real secrets.** The JWT key and any real SMTP app-password belong in user-secrets or environment variables for anything beyond a local demo: `dotnet user-secrets set "Smtp:Password" "..."`. If a real password has ever been committed here, rotate it.

Frontend configuration is minimal: the API base URL string inside each `src/app/services/*.service.ts`, and the theme preference persisted in `localStorage` (`examhub-theme`).

---

## 9. API overview

~40 endpoints across 9 controllers. Roles in brackets; no bracket = any authenticated user.

| Controller | Endpoints |
|---|---|
| **Auth** | `POST /api/auth/register` (public) · `POST /api/auth/login` (public) |
| **Users** | `GET /api/users/me` · `GET /api/users` [Admin] · `PATCH /api/users/{id}/active` [Admin] · `POST /api/users/examiners` [Admin] |
| **Categories** | `GET /api/categories` · `GET .../{id}` · `POST` / `PUT .../{id}` / `DELETE .../{id}` [Admin] |
| **Questions** [Examiner/Admin] | `GET /api/questions` (filters: categoryId, difficulty, search, page) · `GET .../{id}` · `POST` · `PUT .../{id}` · `DELETE .../{id}` (soft) · `POST .../import` (multipart xlsx) · `GET .../import-template` |
| **Exams** | `GET /api/exams` (published, paged) · `GET .../mine` [Examiner] · `GET .../{id}` (answer keys stripped for candidates) · `POST` / `PUT` / `DELETE` [Examiner] · `POST .../{id}/questions` + `DELETE .../{id}/questions/{qid}` (attach/detach) · `POST .../{id}/publish` · `POST .../{id}/publish-results` · `GET .../{id}/attempts` [Examiner/Admin] |
| **Attempts** [Candidate] | `POST /api/attempts/start` `{examId}` · `GET .../{id}` (resume) · `PUT .../{id}/answers` (auto-save upsert) · `POST .../{id}/submit` · `GET .../mine` · `GET .../{id}/result` |
| **Certificates** | `GET /api/certificates/mine` · `GET .../{id}/download` · `POST .../{id}/revoke` [Admin] |
| **Analytics** | `GET /api/analytics/me` [Candidate] · `GET /api/analytics/platform` [Admin] |
| **Notifications** | `GET /api/notifications` · `GET .../unread-count` · `POST .../{id}/read` · `POST .../read-all` |
| **ExaminerRequests** | `POST /api/examiner-requests` (public) · `GET` / `GET .../pending` / `POST .../{id}/approve` / `POST .../{id}/reject` [Admin] |

Error contract: every failure returns `{ "status": <code>, "error": "<human message>" }` from `GlobalExceptionMiddleware` — the frontend surfaces `err.error.error` directly.

---

## 10. Security design

**Backend (the real enforcement):**
- JWT bearer auth; passwords hashed with **BCrypt** (embedded salt).
- `[Authorize(Roles = "...")]` on every non-public endpoint — role tampering in the browser gets 403s from the API regardless of what the UI shows.
- User IDs come from **JWT claims, never request bodies** — you cannot act as someone else by editing a payload.
- Ownership checks in services (examiner owns the exam/question, candidate owns the attempt/certificate), failing as 404 to avoid information leaks.
- Answer keys (`isCorrect`) are stripped from every candidate-facing DTO.
- Deactivated users fail login with a clear message.
- Anti-cheat guards in `SaveAnswerAsync`: the question must belong to the exam, and the selected option must belong to the question (prevents rogue-ID injection that would corrupt scoring).

**Frontend (UX layer on top):**
- `authGuard` redirects unauthenticated visits on every private route; `roleGuard` reroutes role mismatches to the caller's own dashboard.
- `auth.interceptor.ts` attaches the token to all requests, and globally handles **401** (clear session → "Your session has expired" → `/login`) and **429** (friendly rate-limit message).
- Session lives in `sessionStorage` (cleared when the tab closes; logout wipes it explicitly, so the Back button shows no data).
- Passwords are masked; all forms validate before submitting; buttons disable + show spinners while a request is in flight (double-click/duplicate-submit protection).

---

## 11. Concepts used (learning map)

This project was built training-style — each feature deliberately demonstrates a concept, and files carry beginner-friendly comments explaining *why they exist* and *how the framework uses them*.

### Angular concepts → where to see them

| Concept | Demonstrated in |
|---|---|
| Standalone components (no NgModules) | every component; `app.config.ts` wires providers |
| Constructor dependency injection | every component/service |
| Two-way binding `[(ngModel)]` | all forms (login, register, manage-questions, …) |
| Event binding `(click)` / property binding `[ngValue]` | everywhere; category `<select>` in manage-exams |
| Structural directives `*ngFor` / `*ngIf` | every list + view-switching (`view === 'create'`) in manage-exams |
| `[ngClass]` conditional styling | status badges, role pills, question navigator |
| Interpolation + pipes (`date`, `number`) | dashboards, exam lists |
| Routing with route params (`:examId`) | `exam-details`, `attempt-exam`, `exam-result` |
| Lazy loading (`loadComponent`) | `attempt-exam` and `exam-attempts` in `app.routes.ts` |
| Functional route guards (`CanActivateFn`) | `guards/auth.guard.ts`, `guards/role.guard.ts` |
| Functional HTTP interceptor (`HttpInterceptorFn`) | `interceptors/auth.interceptor.ts` |
| `Observable` + `.subscribe({ next, error })` | every service call |
| `BehaviorSubject` shared state | `rxjs/auth.subject.ts` → navbar user display |
| `signal()` basics | timer + theme + menu state in `navigation.ts` / `attempt-exam.ts` |
| `setInterval` + `ngOnDestroy` cleanup | the exam countdown timer |
| CSS custom properties for theming | `styles.css` — `data-theme` + Bootstrap `data-bs-theme` dark mode |
| Unit testing with TestBed + HttpTestingController | every `*.spec.ts` in `services/`, `guards/`, `interceptors/` |

### Backend concepts → where to see them

| Concept | Demonstrated in |
|---|---|
| N-layer architecture (API/BLL/DAL/Domain) | solution structure |
| Repository pattern + generic base repo | `DAL/Repositories/AbstractRepository.cs` + per-aggregate repos |
| Dependency injection & lifetimes | `DependencyInjection.cs` in BLL and DAL |
| EF Core code-first + Fluent API configurations | `DAL/Configurations/*.cs` |
| Migrations | `DAL/Migrations/` (§6) |
| Enum-to-string conversions | every `HasConversion<string>()` |
| Global exception middleware → HTTP mapping | `API/Middleware/GlobalExceptionMiddleware.cs` |
| Custom typed exceptions | `BLL/Exceptions/` |
| JWT creation + claims + role authorization | `AuthService`, `Program.cs`, controllers |
| BCrypt hashing | `AuthService` |
| Server-authoritative timing | `AttemptService.ComputeRemainingSeconds` |
| Explicit DB transactions with rollback | `ExamService.PublishResultsAsync`, `QuestionService.BulkCreateAsync` |
| Upsert pattern | `AttemptAnswerRepository.UpsertAsync` (auto-save) |
| Soft delete | `QuestionService.SoftDeleteAsync` |
| Pagination (page/pageSize clamped server-side) | every `*PagedAsync` method + `PagedResult<T>` |
| Options pattern (`IOptions<EmailSettings>`) | `SmtpNotificationEmailService` |
| Strategy via DI (real SMTP vs no-op email) | email service registration in `Program.cs` |
| File upload (multipart) + ClosedXML parsing | `QuestionsController` + `ImportFromExcelAsync` |
| Unit testing: Moq + FluentAssertions + InMemory EF | `ExamPlatform.BLL.Tests` |

---

## 12. Testing

### Backend — 229 NUnit tests, ~99% line coverage of BLL services

```bash
cd ExamPlatform.Backend
dotnet test ExamPlatform.BLL.Tests                                   # run
dotnet test ExamPlatform.BLL.Tests --collect:"XPlat Code Coverage"   # + coverage.cobertura.xml
```

Every service has a test suite covering happy paths, all validation branches, ownership guards, transaction rollback, and failure isolation (e.g. an email failure never rolls back a result publish). 11 of 13 services sit at 100% line coverage; the only uncovered lines are physically untestable in unit tests (a zero-worksheet xlsx and the live SMTP handshake).

### Frontend — 66 Vitest tests

```bash
cd ExamPlatform.Frontend
npx ng test
```

Covers all 10 API services (method/URL/body per endpoint, error mapping, the datetime→UTC conversion), both guards (token + role redirects), the interceptor (Bearer header, 401 session expiry, 429 handling), and component smoke tests.

### Manual E2E

`TESTING_CHECKLIST.md` at the repo root is a complete scripted walkthrough — every screen, every role, edge cases (timer expiry, mid-attempt refresh, deactivation mid-session, referential-integrity deletes, concurrency), including exactly when to create each test account.

---

## 13. Excel question import format

Download the template from the Manage Questions screen — it includes a **Categories sheet listing every valid category name**, and its sample row uses a real category from your database. Row 1 is the header; data starts at row 2:

| Col | Field | Rule |
|---|---|---|
| A | Category | the category **name** as shown in the app (case-insensitive), e.g. `Web Development` — a numeric id is also accepted for backwards compatibility |
| B | QuestionText | required |
| C | Marks | positive number |
| D | Difficulty | `Easy` / `Medium` / `Hard` |
| E–H | OptionA–OptionD | all four required |
| I | CorrectOption | `A` / `B` / `C` / `D` |

Import is **continue-on-error**: valid rows are created, invalid rows are reported back as `"Row N: reason"` — one bad row never blocks the rest. Duplicate questions (same examiner + category + text, case-insensitive) are rejected.

---

## 14. Troubleshooting

| Symptom | Cause & fix |
|---|---|
| `Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone'` | A timestamp reached Npgsql without UTC kind. New code paths must send UTC ISO strings from the frontend (see `examapi.service.ts` → `toUtcIso`) or pass through `ExamService.EnsureUtc` on the backend |
| `password authentication failed for user "postgres"` | Connection string password in `appsettings.json` doesn't match your local PostgreSQL |
| `relation "users" does not exist` | Migrations not applied — run the `dotnet ef database update` command from §6 |
| Frontend shows "Cannot reach the server" | Backend isn't running on port 5014, or you changed the port without updating the service base URLs |
| CORS errors in the browser console | Frontend must run on `http://localhost:4200` (the `AngularDev` CORS policy allows exactly that origin) |
| 401 on every request right after login | JWT `Issuer`/`Audience`/`Key` mismatch — don't edit one side only |
| Emails not arriving | If `Smtp:Host` is empty the no-op logger is used (check the API console — emails are printed there). With a real host, Gmail needs an **app password**, not the account password |
| "You have already completed this exam" | Working as intended — one attempt per candidate (§4). Use a different candidate account to retest |
| Exam publish rejected | Check the publish pre-conditions in §3 (Examiner table) — the error message states which rule failed |
| Dark mode shows dark text | Both `data-theme` and `data-bs-theme` must be set on `<html>` — the navbar toggle does this; hard-refresh if styles look stale |

---

*Built as a Genspark training capstone — ASP.NET Core 10 · Angular 21 · PostgreSQL.*
