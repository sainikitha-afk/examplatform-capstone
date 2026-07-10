# Exam Platform feature-exam-updates

## Implemented Features

### 1. Question Randomization / Shuffling per Student Attempt
- **Description:** Whenever a candidate starts or resumes an exam attempt, the questions and their multiple-choice options are returned in a random, shuffled order.
- **Implementation Details:** 
  - Handled at the API layer in `AttemptsController.cs` (`ToResumeDto` method).
  - Uses the `AttemptId` as a deterministic seed for the random number generator (`new Random(AttemptId)`).
  - Ensures every unique attempt gets a completely different shuffled order.
  - Ensures that if a candidate refreshes the page or resumes the same attempt midway, the exact same shuffled order is preserved (preventing confusion).
  - No database schema changes were required.

### 2. Auto-generated Performance Analytics Chart
- **Description:** A radar chart dynamically generated based on candidate performance on various topics after exam submission.
- **Backend Implementation:**
  - Removed old tightly-coupled `ChartService.cs` and `ChartStatus`/`ChartUrl` columns in PostgreSQL (via Entity Framework Core migration).
  - Adopted an asynchronous offline python-based script processor that writes to a `TopicScoresUrl` JSON artifact.
  - The API relies *entirely* on the presence of a non-null `TopicScoresUrl` to determine if processing is complete.
  - Cleaned up multiple DTOs (`ExamAttemptMonitorDto`, `AttemptSummaryDto`, `AttemptResultDto`) mapping flows by stripping redundant fields.

- **Frontend UI/UX Enhancements:**
  - **Dynamic Loading State:** If `TopicScoresUrl` is `null`, the UI displays a disabled `Processing...` button featuring a seamless custom CSS dot animation (`.loading-dots` with a static 1.25em box to prevent layout jitter).
  - **Applied Views:** This elegant loading UI is implemented symmetrically across `exam-attempts.html` (for examiners), `candidate-dashboard.html`, and `candidate-analytics.html`.
  - **Radar Chart Visualization:** Uses `ng2-charts` (Chart.js) in the `performance-chart-modal`. Enhanced hover interactions so tooltips combine axes properly (e.g., origin point shows both "Data Structures" and "System Architecture" with a title of "Subject Score").
  - **Table Layout Restyling:** Enhanced data-table aesthetics, adding standard borders, equalized responsive widths (Exam: 30%, Best Score: 30%, Status: 20%, Chart: 20%), and polished cursor behaviors.
