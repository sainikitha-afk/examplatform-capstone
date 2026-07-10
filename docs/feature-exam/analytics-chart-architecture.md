# AI-Powered Performance Analytics Architecture

This document outlines the detailed architecture and workflow for generating automated topic-wise performance charts using Gemini 1.5 Flash API via a Python FastAPI server.

## 1. High-Level Workflow (ASCII Architecture)

```text
 [Candidate] --(Submits Exam Attempt)--> [ .NET AttemptsController ]
                                                  |
                                                  | 1. Score calculated & returned immediately
                                                  v
                                  +------------------------------------+
                                  |   .NET AnalyticsProcessor.cs       |
                                  |   (Runs in Background Queue)       |
                                  +------------------------------------+
                                                  |
                                                  | 2. Check if Categories Exist?
                                                  v
                                      +-------------------------+
                                      | Are Categories Null?    |
                                      +-------------------------+
                                        /                     \
                                      YES                      NO
                                      /                         \
                3. Trigger Gemini 1.5 API                   (Skip AI entirely)
                (Lazy Evaluated 1st run)                         |
                           |                                     |
             +-------------+-------------+                       |
             |                           |                       |
        [SUCCESS]                     [FAIL]                     |
             |                           |                       |
      Save categories              Throws Exception!             |
      to PostgreSQL            (Stops Execution Flow)            |
   (exam_questions table)                |                       |
   (analytic_category col)               |                       |
             |                           |                       |
             v                           v                       v
 4. Run asynchronous        5. TopicScoresUrl stays null.   4. Run asynchronous
    Python script to           Schedules exactly one           Python script to
    calculate topic            Task.Delay(10 minutes)          calculate topic
    score percentages.         to retry AnalyticsProcessor     score percentages.
             |                           |                       |
             v                           v                       v
 5. Save output artifact:         (Candidate UI          5. Save output artifact:
    attempt_{id}_scores.json       shows graceful           attempt_{id}_scores.json
             |                     "Processing...")              |
             v                                                   v
 6. Update PostgreSQL                                    6. Update PostgreSQL
    (attempts table)                                        (attempts table)
    (topic_scores_url col)                                  (topic_scores_url col)
             |                                                   |
             v                                                   v
 (Candidate UI dynamically                          (Candidate UI dynamically
  renders ng2-charts Radar)                          renders ng2-charts Radar)
```

## 2. Database Schema Changes

1. **`exam_questions` Table:**
   - Add column: `question_category` (String, Nullable). 
   - This holds the AI-generated category (e.g., "Array Traversal") specific to this exam's context.

2. **`attempts` Table:**
   - Add column: `topic_scores_url` (String, Nullable).

## 3. The Backend Components

### A. Background Categorization Job
- When an exam is published, a fire-and-forget background job is enqueued with the `ExamId`. The API immediately returns `200 OK` to the examiner so they don't have to wait for the AI processing.
- The job fetches all questions for that exam and constructs a single prompt.
- **AI Call:** It uses native C# `HttpClient` (or `Microsoft.Extensions.AI`) to call the **Gemini 1.5 Flash API**.
- It forces the LLM to output strict JSON mapping each `QuestionId` to exactly one of 5 generated categories.
- **Retry Mechanism:** If the Gemini API fails or times out, the job catches the exception and schedules a retry in **10 minutes**.

### B. Analytics Score Processor (Background Process)
- A background analytics processor orchestrates the evaluation of candidate attempts upon submission.
- It delegates the core score and topic aggregation to an **asynchronous Python-based script** (e.g., using `ProcessStartInfo` in C#).
- The Python script calculates the percentage score for each `question_category` and outputs the result as a structured JSON artifact.
- The JSON file is saved locally to `ExamPlatform.BLL/Assets/candidates/{candidate_id}/attempt_{attempt_id}_scores.json`.
- The processor then updates the `attempts` table with the `topic_scores_url`.
- *Note: Removed legacy `.NET ChartService.cs` as we no longer generate static images on the backend.*

## 4. Frontend Integration

1. **Taken Exams / Exam Attempts Table:**
   - A new column "Chart" is added to the examiner monitoring views and candidate views.
   - If `topic_scores_url == null`, the button dynamically displays an animated **"Processing..."** loading indicator using CSS, gracefully indicating that the background python script is still working.
   - If `topic_scores_url != null`, the button displays **"View"**.
2. **The Result Modal:**
   - Clicking "View" opens a `performance-chart-modal`.
   - The modal fetches the JSON data using the `topic_scores_url`.
   - Instead of displaying a static backend image, the frontend dynamically plots the data via an interactive **Radar Chart using `ng2-charts` (Chart.js)**.
   - Enhanced hover interactions smoothly display combined axes data in customized tooltips.

## 5. Failure Tolerance
- Because chart generation depends on the AI having categorized the questions *first*, if the AI background job fails or is delayed, the student can still submit the exam.
- The `attempts` row is saved with `topic_scores_url = null`.
- The user interface naturally falls back to the "Processing..." state until the analytics can be successfully aggregated and the JSON URL populated.

## 6. Raw Planned version:

So here's our plan!

Examiner adds question
  |
Publishes the exam
  |
1. The database will get added with questions.   
2. The background job triggers the python fastapi server for categorizing
  |
(Backgriund activity)
If background job is done it'll be sending back the categories which will get stored in the exam_questions table in a column named question_category
 |
Everytime the student completes the question, the service will calculate the percentage on each categories based on the completion of the questions and it'll generate a Rador Chart image and stores it inside EventPlatform.BLL/asset/candidates/{candidate_id} and the url of that file path will get stored inside attempts table beside the scores column with a column of a suitable name.
 |
The email will be mentioning it to view the chart.
 |
In the candidates dashboard under taken exam there will be a another column that conatins a button as. "View Chart" which will open a modal popup, shows the chart, performance table and its respective description.


What if the background job fails?
If it fails, still fine the background job retry it every 10 minutes for the questions that failed and retry the entire process of calculating perfoamce and chart generation, until then the view chart button shows status as "Processing" with a refresh button.

Note:
The chart processing must be a separate endpoint, a separate service file.

