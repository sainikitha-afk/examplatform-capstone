# AI-Powered Performance Analytics Architecture

This document outlines the detailed architecture and workflow for generating automated topic-wise performance charts using Gemini 1.5 Flash API via a Python FastAPI server.

## 1. High-Level Workflow (ASCII Architecture)

```text
 [Examiner] --(Publishes Exam)--> [ .NET API Backend ]
                                         |
                                         | 1. Store Questions & return 200 OK immediately
                                         |    (Examiner DOES NOT wait for AI)
                                         v
                            +--------------------------+
                            |     PostgreSQL DB        |
                            +--------------------------+
                                         |
                                         | 2. Fire-and-Forget Background Job
                                         |    (e.g., Hangfire or IHostedService)
                                         v
                         +-----------------------------------+
                         | .NET Background Categorizer Job   |
                         | (Retries every 10 mins on fail)   |
                         +-----------------------------------+
                                         | 3. HTTP Request (C# HttpClient)
                                         v
                           +---------------------------+
                           |  Google Gemini 1.5 Flash  |
                           +---------------------------+
                                         | 4. Return JSON Categories
                                         v
                                [ .NET Background Job ]
                                         | 5. Update `exam_questions` table
                                         |    (Set `question_category`)
                                         v
                            +--------------------------+
                            |     PostgreSQL DB        |
                            +--------------------------+

========================================================================================

 [Student] --(Submits Exam)-->  [ .NET AttemptsController ]
                                         |
                                         | 1. Score calculated
                                         v
                          +-------------------------------------+
                          |   .NET Analytics Processing Service |
                          |                                     |
                          +-------------------------------------+
                                         | 2. Generate Radar Chart Image
                                         | 3. Save to Local FS: 
                                         |    `assets/candidates/{candidate_id}/attempt_{attempt_id}_scores.json`
                                         v
                            +-----------------------------------------+
                            |     PostgreSQL DB                       |
                            | (Update `attempts` table                |
                            |  with `ChartStatus and topScoresUrl` )  |
                            +-----------------------------------------+
                                         |
                                         | 4. Send Email Notification (Mentions to view Chart)
                                         v
                             [ SMTP / Email Provider ]

========================================================================================

 [Student Views Dashboard]
            |
            v
 [Check Attempt `chart_status`]
            |
    +-------+-------+-------------------+
    |                                   |
[ Processing / Pending ]          [ Completed ]
    |                                   |
    v                                   v
 [Show Button]                     [Show Button]
 "Processing ⏳"                    "View Chart 📊"
    |                                   |
    v                                   v
(Button Disabled/                 (Clicks Button)
 Shows Come Back                        |
 later)                                 |
    |                                   v
    v                              [Modals popup]
(Waits for 10-min retry                 |
 sweep to finish)                       v
                                +----------------+
                                | - Radar Chart  |
                                | - Perf Table   |
                                +----------------+
```

## 2. Database Schema Changes

1. **`exam_questions` Table:**
   - Add column: `question_category` (String, Nullable). 
   - This holds the AI-generated category (e.g., "Array Traversal") specific to this exam's context.

2. **`attempts` Table:**
   - Add column: `chart_url` (String, Nullable).
   - Add column: `chart_status` (String/Enum). States: `Pending`, `Processing`, `Completed`, `Failed`.

## 3. The Backend Components

### A. Background Categorization Job
- When an exam is published, a fire-and-forget background job is enqueued with the `ExamId`. The API immediately returns `200 OK` to the examiner so they don't have to wait for the AI processing.
- The job fetches all questions for that exam and constructs a single prompt.
- **AI Call:** It uses native C# `HttpClient` (or `Microsoft.Extensions.AI`) to call the **Gemini 1.5 Flash API**.
- It forces the LLM to output strict JSON mapping each `QuestionId` to exactly one of 5 generated categories.
- **Retry Mechanism:** If the Gemini API fails or times out, the job catches the exception and schedules a retry in **10 minutes**.

### B. Chart Processing Service (Separate Endpoint)
- A dedicated `.cs` service file that is invoked upon attempt completion.
- It calculates the percentage score for each `question_category`.
- It generates a graphical image (Radar Chart) using a library like **SkiaSharp** or **OxyPlot**.
- It saves the image to `ExamPlatform.BLL/assets/candidates/{candidate_id}/{filename}.png`.
- Updates the `attempts` table with the file path.

## 4. Frontend Integration

1. **Taken Exams Table:**
   - A new column is added to the candidates' "Taken Exams" view.
   - If `chart_status == 'Processing'`, the button displays **"Processing ⏳"** and a manual **Refresh** button is shown.
   - If `chart_status == 'Completed'`, the button displays **"View Chart 📊"**.
2. **The Result Modal:**
   - Clicking "View Chart" opens a modal.
   - The modal fetches the image using the `chart_url`.
   - Displays a breakdown table of topics and scores alongside the radar chart.

## 5. Failure Tolerance
- Because chart generation depends on the AI having categorized the questions *first*, if the AI background job fails or is delayed, the student can still submit the exam.
- The `attempts` row is saved with `chart_status = 'Pending'`.
- A separate sweeper job (or a check during the 10-minute retry) will look for completed attempts that don't have charts yet, verify if categories are now available, and trigger the Chart Processing Service retroactively.

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

