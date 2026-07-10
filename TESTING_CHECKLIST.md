# ExamPlatform — End-to-End Frontend Testing Checklist

Work top to bottom. Each phase builds on data created in the previous one.
**Account creation steps are marked with 👤 CREATE — do them exactly when instructed**, because later steps depend on those accounts existing.

## Accounts you will end up with

| Account | Role | Created in | Purpose |
|---|---|---|---|
| `admin` | Admin | Pre-existing (Phase 0) | All admin flows |
| `examiner_alpha` | Examiner | Phase 2 (admin creates) | Main examiner flows |
| `examiner_beta` | Examiner | Phase 2 (via approved request) | Request-approval flow, data isolation |
| `candidate_one` | Candidate | Phase 1 (self-register) | Main candidate happy path (passes exam) |
| `candidate_two` | Candidate | Phase 4 (self-register) | Fail path + concurrency tests |
| `candidate_three` | Candidate | Phase 5 (self-register) | Deactivation test (throwaway) |

---

## Phase 0 — Environment setup

- [*] Backend running at `http://localhost:5014` (check Swagger loads at `/swagger`)
- [*] Frontend running via `ng serve`, loads at `http://localhost:4200` with no console errors
- [*] Database is in a known state (ideally fresh/clean so counts are predictable)
- [*] **Admin account exists.** Register does NOT allow creating admins (it hardcodes Candidate). If no admin exists in the DB, insert one directly (SQL) or via whatever bootstrap you used before. Verify you can log in as admin before continuing.
- [*] Open DevTools → Network tab and keep it open for the whole session (you'll verify API calls and status codes throughout)

---

## Phase 1 — Public pages, registration, login, guards

### Landing page
- [*] `/` loads the landing page while logged out
- [*] All landing links/buttons navigate correctly (Login, Register, Apply as Examiner)
- [*] Unknown URL (e.g. `/garbage`) redirects to `/` (wildcard route)

### Registration (candidate)
- [*] 👤 CREATE `candidate_one` — register with valid data → success, redirected to login (or logged in, per your design)
- [*] Register with an already-taken username → friendly error, no crash
- [*] Register with an already-taken email → friendly error
- [*] Submit with empty required fields → validation blocks submit
- [*] Invalid email format → validation error
- [*] Password too short / not meeting rules → error message matches backend rule
- [*] Verify the new user appears in Admin → Manage Users later (Phase 2)

### Apply as Examiner (public form)
- [*] Submit a valid examiner request for `examiner_beta` (name, email, reason etc.) → success message
- [*] Submit the same request again (same email) → check behavior (duplicate handled gracefully?)
- [*] Submit with empty fields → validation blocks

### Login
- [*] Login as `admin` → lands on `/admin` dashboard
- [*] Login as `candidate_one` → lands on `/candidate` dashboard
- [*] Wrong password → error alert, stays on login, no token in sessionStorage
- [*] Non-existent username → error alert
- [*] Empty form submit → validation blocks
- [*] After login: `token`, `role`, `username` present in sessionStorage (DevTools → Application)

### Guards & session edge cases (do these logged in as `candidate_one`)
- [*] Visit `/admin` directly by URL → blocked (redirect, not a broken page)
- [*] Visit `/examiner` directly → blocked
- [*] Visit `/manage-questions` directly → blocked
- [*] Log out → sessionStorage cleared, redirected to login/landing
- [*] After logout, press browser Back → guarded pages don't render with data
- [*] While logged out, visit `/candidate` directly → redirected to login
- [*] Open the app in a **new tab** while logged in → sessionStorage is per-tab, so new tab is logged out. Confirm this is the behavior you expect (known limitation, not a bug)
- [*] Manually delete the token in sessionStorage, then click around → app handles 401s without white-screening
- [*] Manually change `role` in sessionStorage to `Admin` as a candidate, visit `/admin` → frontend may let you in, but every API call must return 403 (backend is the real enforcement). Page should show errors, not fake data.

---

## Phase 2 — Admin module (log in as `admin`)

### Admin dashboard
- [ ] Dashboard loads: stat cards show real numbers (users, exams, etc.)
- [ ] No endless network requests in the Network tab (regression check for the old HTTP polling loop bug)
- [ ] Quick links navigate to the right screens

### Categories (`/admin/categories`)
- [ ] Create category "Programming" → appears in list without manual refresh
- [ ] Create 2–3 more categories (you'll need them for exams/questions later)
- [ ] Create with empty name → blocked by validation
- [ ] Create duplicate name → check behavior (error or allowed — confirm it matches intent)
- [ ] Edit a category name → list updates
- [ ] Delete an unused category → gone from list
- [ ] (Defer) Delete a category that has questions/exams — retest in Phase 5 after data exists

### Manage Examiners (`/admin/examiners`)
- [ ] 👤 CREATE `examiner_alpha` — fill the create-examiner form → success
- [ ] Verify `examiner_alpha` can log in (new tab / logout-login) and lands on `/examiner`
- [ ] Create examiner with duplicate username/email → friendly error
- [ ] Empty form → validation blocks

### Examiner Requests (`/admin/examiner-requests`)
- [ ] The request submitted in Phase 1 for `examiner_beta` is listed
- [ ] 👤 CREATE `examiner_beta` — **Approve** the request → examiner account created (note how credentials are communicated)
- [ ] Verify `examiner_beta` can log in as Examiner
- [ ] Submit another throwaway request (public form), then **Reject** it → status updates, no account created
- [ ] Approve/reject an already-actioned request (double-click the button) → no duplicate account / no crash

### Manage Users (`/admin/users`)
- [ ] All users listed with correct roles (`candidate_one`, both examiners, admin)
- [ ] Deactivate — defer the full test to Phase 5 (needs `candidate_three`); for now just confirm the button exists and the list renders

### Platform Analytics
- [ ] Admin analytics loads (`GET /api/analytics/platform` returns 200)
- [ ] Numbers are plausible for current data (mostly zeros right now — no crash on empty data)

---

## Phase 3 — Examiner module (log in as `examiner_alpha`)

### Examiner dashboard
- [ ] Loads with correct stats (0 exams, 0 questions initially — empty state, no crash)
- [ ] No polling loop in the Network tab

### Manage Questions (`/manage-questions`)
- [ ] Create an MCQ: text, category, options, correct answer, marks → appears in list
- [ ] Create at least **10 questions** in one category (needed for a real exam) and 2–3 in a second category
- [ ] Create with no correct answer selected → blocked or clear error
- [ ] Create with fewer than 2 options → blocked
- [ ] Create with empty question text → validation blocks
- [ ] Edit a question (text + change correct answer) → saved, visible after reload
- [ ] Delete an unattached question → removed
- [ ] Filter by category → only matching questions shown; switch back to All
- [ ] **Excel import**: import a valid file → questions appear with correct options/answers
- [ ] Excel import: wrong file type (e.g. .png) → friendly error
- [ ] Excel import: valid .xlsx but wrong/missing columns → friendly error, no partial garbage rows
- [ ] Excel import: empty file → handled

### Manage Exams (`/manage-exams`)
- [ ] Create exam "Main Exam": title, description, category, duration (e.g. 30 min), passing score → appears in list as Draft/unpublished
- [ ] Create with invalid values (0 or negative duration, empty title, passing score > total) → blocked
- [ ] Edit the exam → changes persist
- [ ] **Attach questions**: attach the 10 questions → attached list updates
- [ ] Attach the same question twice → blocked or deduplicated (no dupes in exam)
- [ ] **Detach** a question → removed from exam
- [ ] Try to **publish an exam with 0 questions** (make a second empty exam) → should be blocked or clearly warned
- [ ] **Publish** "Main Exam" → status changes; verify `GET /api/exams` returns 200 (regression check for the old 500 bug)
- [ ] Edit/delete a **published** exam → check behavior is intentional (blocked, or allowed with warning)
- [ ] Delete a draft exam → removed
- [ ] 👤 Also create + publish a **"Speed Exam"**: duration **1–2 minutes**, 2–3 questions — you'll use it for the auto-submit timeout test in Phase 4
- [ ] Create + publish a **"Fail Exam"**: few questions, high passing score — used for the fail path
- [ ] `Publish results` before anyone has attempted → no crash, sensible message

### Data isolation
- [ ] Log in as `examiner_beta` → sees **no** questions/exams from `examiner_alpha` (or sees them, if shared by design — confirm which is intended)

### Exam attempts monitoring (`/exam-attempts/:examId`)
- [ ] Loads with an empty state for "Main Exam" (no attempts yet) — retest after Phase 4

---

## Phase 4 — Candidate module (log in as `candidate_one`)

### Candidate dashboard
- [ ] Loads, stat cards correct (0 attempts, 0 certificates), no polling loop

### Browse exams (`/browse-exams`)
- [ ] Only **published** exams appear (Main, Speed, Fail) — draft exams are invisible
- [ ] Filter by category works; "no exams" empty state renders for an empty category
- [ ] Click an exam → exam details page shows title, duration, question count, passing score

### Attempt flow — happy path (Main Exam)
- [ ] Start attempt → `POST /api/attempts/start/{examId}` 200, exam screen loads
- [ ] Timer counts down from the full duration
- [ ] Answer question 1 → Network tab shows `PATCH /api/attempts/{id}/answer` (auto-save fires per answer)
- [ ] Change an answer → another PATCH fires, new value saved
- [ ] Question navigator jumps between questions; answered ones are visually marked
- [ ] Navigate away and back to a question → selected answer still shown
- [ ] **Refresh mid-attempt** → attempt resumes with saved answers, timer continues from **server** remaining time (not reset to full)
- [ ] **Start the same exam again in a second tab** (while the first attempt is in progress) → blocked (one in-progress attempt per exam — DB unique index; verify friendly error, not a 500)
- [ ] Answer enough to pass, leave 1–2 questions **unanswered**, Submit → confirm dialog (if any) → `POST /submit` 200 → navigated to result/confirmation
- [ ] Try to reopen the submitted attempt URL → can't continue answering

### Auto-submit on timeout (Speed Exam)
- [ ] Start "Speed Exam", answer 1 question, **let the timer hit 0** → auto-submits, no crash, navigates to result
- [ ] Timer at 0 does not fire duplicate submits (check Network: exactly one POST submit)

### Fail path (Fail Exam) — 👤 CREATE `candidate_two` first (register), then as `candidate_two`:
- [ ] Attempt "Fail Exam", answer wrong/few → submit → result shows **Fail**, score below passing
- [ ] No certificate generated for a failed attempt (check `/my-certificates` — empty)

### Results & publish-results gating
- [ ] As candidate: check whether score/result is visible **before** the examiner publishes results — confirm gating matches your design
- [ ] As `examiner_alpha`: `/exam-attempts/{mainExamId}` now lists candidate_one's attempt; then **Publish Results** for Main Exam
- [ ] As `candidate_one`: result page (`/result/:attemptId`) shows score, pass/fail, per-question breakdown (if designed)
- [ ] `GET /api/attempts/my` list (dashboard/history) shows the attempt with correct status

### Certificates (`/my-certificates`)
- [ ] Passed attempt produced a certificate, listed with exam name/date
- [ ] **Download** → a valid PDF downloads and opens
- [ ] As `admin`: revoke that certificate → as candidate, list reflects revoked state; download blocked or marked
- [ ] Candidate with no certificates (`candidate_two`) sees a clean empty state

### Candidate analytics (`/my-analytics`)
- [ ] Loads with correct totals (attempts, pass rate, avg score) for `candidate_one`
- [ ] Loads without crashing for a brand-new candidate with zero attempts

### Notifications (`/notifications`)
- [ ] Notifications exist for the events that should create them (result published, certificate issued — verify per your backend)
- [ ] Mark as read → visual state changes, persists after reload
- [ ] Unread badge/count in navigation updates (if implemented)
- [ ] Empty state for a fresh user

### Retake behavior
- [ ] After submitting, start "Main Exam" again → allowed or blocked per your max-attempts rule; verify the rule actually enforces

---

## Phase 5 — Cross-cutting & destructive edge cases

### Deactivation (👤 CREATE `candidate_three` — register, log in once, start an attempt, keep the tab open)
- [ ] As admin: deactivate `candidate_three`
- [ ] `candidate_three` cannot log in anymore → clear error message
- [ ] In the still-open tab: next API call fails gracefully (401/403 → kicked to login, not white screen)

### Referential integrity (as admin/examiner)
- [ ] Delete a category that has questions and a published exam → blocked with a clear error, or cascades per design — verify DB isn't left inconsistent
- [ ] Delete a question that's attached to a published exam that has attempts → blocked or handled; existing results unchanged

### Concurrency
- [ ] `candidate_one` and `candidate_two` attempt the same exam at the same time (two browsers) → both work independently, answers don't cross
- [ ] Examiner publishes results while a candidate is mid-attempt → no data corruption; define/verify expected behavior

### Failure handling
- [ ] Stop the backend, click around each dashboard → user-friendly errors (alerts), no infinite spinners, no white screens; restart backend → app recovers
- [ ] Slow network (DevTools throttling) during attempt → answer PATCHes queue/complete, double-clicking Submit doesn't create two submits
- [ ] Double-click every create/save button once (category, question, exam) → no duplicate records

### UI polish sweep (every screen, both themes if applicable)
- [ ] No raw JSON/`[object Object]` in alerts or on screen
- [ ] All empty states render (fresh user on every list screen)
- [ ] All tables/forms usable at mobile width (Bootstrap responsive check)
- [ ] Browser console clean of errors on every route
- [ ] Every action gives feedback (success or error) — nothing silently does nothing

### Regression checks for known past bugs
- [ ] No HTTP polling loop on candidate/examiner/admin dashboards (Network tab quiet after load)
- [ ] `GET /api/exams` returns 200 for all three roles
- [ ] Attach-questions UI is complete and functional (was WIP)

---

## Sign-off

- [ ] All phases complete, failures logged with: screen, account used, steps, expected vs actual, network status code
