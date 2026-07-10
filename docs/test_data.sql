
----- Select all tables ------

SELECT schemaname, tablename 
FROM pg_catalog.pg_tables 
WHERE schemaname NOT IN ('pg_catalog', 'information_schema');


------- Clear all records --------

DO $$ 
DECLARE 
    r RECORD;
BEGIN
    -- Loop through all tables in the public schema except the EF Migrations table
    FOR r IN (
        SELECT tablename 
        FROM pg_tables 
        WHERE schemaname = 'public' 
          AND tablename NOT IN ('__EFMigrationsHistory', '__efmigrationshistory')
    ) LOOP
        -- Safe dynamic SQL execution using format()
        EXECUTE format('TRUNCATE TABLE %I CASCADE;', r.tablename);
    END LOOP;
END $$;




Select * from "attempts";
Select * from "questions";
Select * from "question_options";
--/assets/candidates/4/attempt_2_scores.json
Update "attempts"
Set "topic_scores_url" = null
Where "attempt_id" = 1;

SELECT 
    "q"."question_text", 
    "qo"."option_text"
FROM "questions" AS "q"
JOIN "question_options" AS "qo" 
    ON "q"."question_id" = "qo"."question_id"
Where "qo"."is_correct" = true;



-- 1. Insert Examiner and Candidate Users
INSERT INTO "users" ("user_id", "username", "email", "password_hash", "full_name", "role", "date_of_joining", "is_active") 
VALUES (2, 'examiner2', 'examiner@test.com', 'dummyhash', 'Test Examiner', 2, NOW(), true)
ON CONFLICT ("user_id") DO NOTHING;

INSERT INTO "users" ("user_id", "username", "email", "password_hash", "full_name", "role", "date_of_joining", "is_active") 
VALUES (1, 'candidate1', 'candidate@test.com', 'dummyhash', 'Test Candidate', 1, NOW(), true)
ON CONFLICT ("user_id") DO NOTHING;

-- 2. Insert Category
INSERT INTO "exam_categories" ("cat_id", "cat_name", "description")
VALUES (99, 'Software Engineering', 'Test SE questions')
ON CONFLICT ("cat_id") DO NOTHING;

-- 3. Insert Exam (ExaminerId = 2)
INSERT INTO "exams" ("exam_id", "examiner_id", "cat_id", "title", "instructions", "duration_minutes", "total_marks", "passing_marks", "start_time", "end_time", "exam_status", "created_at")
VALUES (777, 2, 99, 'Analytics Test Exam', 'An exam designed to test the Gemini AI and QuickChart flow.', 60, 30.0, 15.0, NOW(), NOW() + INTERVAL '30 days', 1, NOW())
ON CONFLICT ("exam_id") DO NOTHING;

-- 4. Insert 15 Questions (Spanning Database, OOP, Web, Data Structures, Architecture) (ExaminerId = 2)
INSERT INTO "questions" ("question_id", "examiner_id", "cat_id", "question_text", "marks", "difficulty", "created_at", "is_active") VALUES 
(101, 2, 99, 'What does SQL stand for?', 2.0, 'Easy', NOW(), true),
(102, 2, 99, 'Which keyword is used to retrieve data from a database?', 2.0, 'Easy', NOW(), true),
(103, 2, 99, 'Which type of JOIN returns all rows from the left table?', 2.0, 'Medium', NOW(), true),
(104, 2, 99, 'In C#, what is the base class of all types?', 2.0, 'Medium', NOW(), true),
(105, 2, 99, 'What principle does the "S" in SOLID stand for?', 2.0, 'Hard', NOW(), true),
(106, 2, 99, 'What is encapsulation in Object-Oriented Programming?', 2.0, 'Medium', NOW(), true),
(107, 2, 99, 'What is a closure in JavaScript?', 2.0, 'Hard', NOW(), true),
(108, 2, 99, 'Which HTTP method is idempotent and used to replace a resource?', 2.0, 'Medium', NOW(), true),
(109, 2, 99, 'What does CSS stand for?', 2.0, 'Easy', NOW(), true),
(110, 2, 99, 'What is the time complexity of searching in a balanced Binary Search Tree?', 2.0, 'Hard', NOW(), true),
(111, 2, 99, 'Which data structure uses LIFO (Last-In-First-Out)?', 2.0, 'Easy', NOW(), true),
(112, 2, 99, 'What is a Hash Table primarily used for?', 2.0, 'Medium', NOW(), true),
(113, 2, 99, 'What does a Load Balancer do?', 2.0, 'Medium', NOW(), true),
(114, 2, 99, 'What is the purpose of caching in system design?', 2.0, 'Easy', NOW(), true),
(115, 2, 99, 'What is microservices architecture?', 2.0, 'Medium', NOW(), true)
ON CONFLICT ("question_id") DO NOTHING;

-- 5. Insert Options (1 correct, 1 wrong per question for brevity)
INSERT INTO "question_options" ("option_id", "question_id", "option_text", "is_correct") VALUES
(1011, 101, 'Structured Query Language', true),
(1012, 101, 'Simple Question Language', false),
(1021, 102, 'SELECT', true),
(1022, 102, 'GET', false),
(1031, 103, 'LEFT JOIN', true),
(1032, 103, 'INNER JOIN', false),
(1041, 104, 'System.Object', true),
(1042, 104, 'System.Base', false),
(1051, 105, 'Single Responsibility Principle', true),
(1052, 105, 'Static Typing', false),
(1061, 106, 'Hiding internal state and requiring all interaction to be performed through an objects methods', true),
(1062, 106, 'Writing code inside capsules', false),
(1071, 107, 'A function retaining access to its lexical scope', true),
(1072, 107, 'A closed tag in HTML', false),
(1081, 108, 'PUT', true),
(1082, 108, 'POST', false),
(1091, 109, 'Cascading Style Sheets', true),
(1092, 109, 'Creative Style System', false),
(1101, 110, 'O(log n)', true),
(1102, 110, 'O(1)', false),
(1111, 111, 'Stack', true),
(1112, 111, 'Queue', false),
(1121, 112, 'Fast data retrieval', true),
(1122, 112, 'Storing hierarchical data', false),
(1131, 113, 'Distributes network or application traffic across a number of servers', true),
(1132, 113, 'Balances the weight of the server rack', false),
(1141, 114, 'To reduce latency and improve performance', true),
(1142, 114, 'To store user passwords securely', false),
(1151, 115, 'Structuring an application as a collection of loosely coupled services', true),
(1152, 115, 'A small desktop application', false)
ON CONFLICT ("option_id") DO NOTHING;

-- 6. Map Questions to Exam (ExamQuestions)
INSERT INTO "exam_questions" ("exam_question_id", "exam_id", "question_id", "question_order", "analytic_category") VALUES
(201, 777, 101, 1, NULL),
(202, 777, 102, 2, NULL),
(203, 777, 103, 3, NULL),
(204, 777, 104, 4, NULL),
(205, 777, 105, 5, NULL),
(206, 777, 106, 6, NULL),
(207, 777, 107, 7, NULL),
(208, 777, 108, 8, NULL),
(209, 777, 109, 9, NULL),
(210, 777, 110, 10, NULL),
(211, 777, 111, 11, NULL),
(212, 777, 112, 12, NULL),
(213, 777, 113, 13, NULL),
(214, 777, 114, 14, NULL),
(215, 777, 115, 15, NULL)
ON CONFLICT ("exam_question_id") DO NOTHING;

-- 7. Insert Attempt (UserId = 1)
INSERT INTO "attempts" ("attempt_id", "user_id", "exam_id", "started_at", "attempt_status", "score", "is_passed", "time_taken_seconds", "submitted_at", "chart_url", "chart_status")
VALUES (666, 1, 777, NOW(), 0, NULL, NULL, NULL, NULL, NULL, '')
ON CONFLICT ("attempt_id") DO NOTHING;

-- 8. Insert AttemptAnswers (Simulating a mix of correct and incorrect answers)
INSERT INTO "attempt_answers" ("answer_id", "attempt_id", "question_id", "selected_option_id", "is_correct", "marks_awarded", "answered_at") VALUES
(301, 666, 101, 1011, true, 2.0, NOW()),
(302, 666, 102, 1021, true, 2.0, NOW()),
(303, 666, 103, 1032, false, 0.0, NOW()), -- Wrong
(304, 666, 104, 1042, false, 0.0, NOW()), -- Wrong
(305, 666, 105, 1051, true, 2.0, NOW()),
(306, 666, 106, 1061, true, 2.0, NOW()),
(307, 666, 107, 1071, true, 2.0, NOW()),
(308, 666, 108, 1082, false, 0.0, NOW()), -- Wrong
(309, 666, 109, 1091, true, 2.0, NOW()),
(310, 666, 110, 1102, false, 0.0, NOW()), -- Wrong
(311, 666, 111, 1111, true, 2.0, NOW()),
(312, 666, 112, 1121, true, 2.0, NOW()),
(313, 666, 113, 1131, true, 2.0, NOW()),
(314, 666, 114, 1142, false, 0.0, NOW()), -- Wrong
(315, 666, 115, 1151, true, 2.0, NOW())
ON CONFLICT ("answer_id") DO NOTHING;
