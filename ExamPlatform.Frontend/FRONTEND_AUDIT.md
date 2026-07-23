## Audit Checklist

### Setup (do this once)
1. Both backend (port 5014) and frontend (port 4200) running
2. Browser DevTools open (F12), Console + Network tabs visible
3. Hard refresh between role switches (Cmd+Shift+R) to clear sessionStorage and old state
4. You have 3 test accounts ready — one Admin, one Examiner, one Candidate. If you don't have all three, register a candidate now and use SQL to create the other two.

### Candidate role audit

| # | Screen | URL | Works? | Notes / Console errors |
|---|---|---|---|---|
| 1 | Login as candidate | `/login` | ? | |

| 2 | Candidate dashboard loads | `/candidate` | ? | (the polling bug — note how many times analytics is called) |
| 3 | Navigation top bar | (any page) | ? | username visible? logout works? |
| 4 | Browse exams | `/browse-exams` | ? | category filter works? list populates? |
| 5 | Click an exam → details | `/exam-details/:id` | ? | |
| 6 | Click "Start Exam" | `/attempt/:examId` | ? | timer starts? questions render? |
| 7 | Answer a question | (attempt screen) | ? | auto-save fires in Network tab? |
| 8 | Navigate between questions | (attempt screen) | ? | navigator squares change color? |
| 9 | Submit exam | (attempt screen) | ? | redirects to result? |
| 10 | View result | `/result/:attemptId` | ? | shows score, per-question breakdown? |
| 11 | View my analytics | `/candidate-analytics` | ? | stats correct? |
| 12 | View my certificates | `/certificates` | ? | list populates? download works? |
| 13 | View notifications | `/notifications` | ? | mark-as-read works? |

### Examiner role audit

| # | Screen | URL | Works? | Notes |
|---|---|---|---|---|
| 14 | Login as examiner | `/login` | ? | |
| 15 | Examiner dashboard | `/examiner` | ? | |
| 16 | Manage questions list | `/manage-questions` | ? | |
| 17 | Create a question | (form on same screen?) | ? | options input works? mark correct? |
| 18 | Edit a question | | ? | |
| 19 | Delete a question | | ? | |
| 20 | Excel import | (button on manage-questions) | ? | file picker opens? upload works? |
| 21 | Manage exams list | `/manage-exams` | ? | |
| 22 | Create an exam | | ? | |
| 23 | **Attach questions to exam** | | ❌ | (we know this is broken) |
| 24 | Edit an exam | | ? | |
| 25 | Delete an exam | | ? | |
| 26 | Publish an exam | | ? | |
| 27 | Publish results | `/publish-results/:examId`? | ? | |

### Admin role audit

| # | Screen | URL | Works? | Notes |
|---|---|---|---|---|
| 28 | Login as admin | `/login` | ? | |
| 29 | Admin dashboard | `/admin` | ? | platform stats correct? |
| 30 | Manage categories | `/manage-categories` | ? | CRUD works? |
| 31 | Manage users | `/manage-users` | ? | list paginates? deactivate works? |
| 32 | Manage examiners | `/manage-examiners` | ? | create examiner works? |
| 33 | Revoke a certificate | (somewhere?) | ? | |

### Cross-cutting

| # | Concern | Works? | Notes |
|---|---|---|---|
| 34 | Direct URL access while logged out → redirects to /login | ? | try `/admin` in incognito |
| 35 | Direct URL to wrong role → blocked by role guard | ? | login as candidate, hit `/admin` |
| 36 | Refresh page mid-session → JWT survives, doesn't logout | ? | |
| 37 | Logout → sessionStorage cleared, redirect to /login | ? | |
| 38 | Token expiry → behavior? | ? | (skip unless you know how to test) |
| 39 | Browser back button → behavior? | ? | |
| 40 | Network tab: any 500 errors anywhere? | ? | |
| 41 | Network tab: same endpoint hit repeatedly? | ? | (the polling bug — list affected screens) |
| 42 | Console: any red errors? | ? | screenshot the worst |

---

# 🔒 Your move

1. **Set up your 3 test accounts** if you don't have them (tell me if you need help creating them via SQL or via the Register flow)
2. **Open both apps + DevTools**
3. **Work through the checklist** — fill in the "Works?" column with ✅/⚠️/❌, jot quick notes
4. **Paste the filled checklist back to me**

I'll then build the prioritized fix list, and we'll go through them one at a time in lock-in mode — diagnose, fix, understand, viva-prep.

This audit will probably take **45-90 minutes**. It's not glamorous. But it's the difference between "I think it works" and "I know exactly what works, what doesn't, and why." Mentor-grade prep.

**Ready?** Or do you want to set up test accounts first? 🚀