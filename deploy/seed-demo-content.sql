-- ============================================================================
--  ExamHub — Demo Content Seed  (seed-demo-content.sql)
--
--  WHAT THIS IS
--    A deterministic, human-readable data seed captured from the working demo
--    database. It is NOT a binary backup — it is plain INSERT statements you
--    commit to git and run with psql. Every row uses ON CONFLICT DO NOTHING, so
--    the script is idempotent: safe to run once or many times.
--
--  PRE-REQUISITE
--    Run EF Core migrations FIRST so the schema exists. On Azure the API applies
--    migrations automatically on startup (Database:MigrateOnStartup=true), so by
--    the time you run this the tables already exist. Locally you can also run:
--      dotnet ef database update --project ExamPlatform.DAL --startup-project ExamPlatform.API
--
--  RUN
--    Local:  PGPASSWORD=postgre psql -h localhost -U postgres -d exam_platform_db \\
--              -v ON_ERROR_STOP=1 -1 -f deploy/seed-demo-content.sql
--    Azure:  PGPASSWORD='<db-pass>' psql \\
--              "host=<server>.postgres.database.azure.com port=5432 dbname=exam_platform_db \\
--               user=examadmin sslmode=require" \\
--              -v ON_ERROR_STOP=1 -1 -f deploy/seed-demo-content.sql
--
--  LOGIN AFTER SEEDING (passwords are already BCrypt-hashed in the data —
--  no hasher needed; they are the real working credentials):
--      Admin      admin      / admin1234
--      Examiner   profdemo   / demo1234       (premium/professional)
--      Candidate  diya       / diya1234
--
--  NOTES
--    * __EFMigrationsHistory is intentionally excluded — migrations own it.
--    * Identity sequences are reset (setval, at the end) so the app's next
--      insert continues after the seeded ids instead of colliding.
--    * Table order is FK-safe (parents before children).
-- ============================================================================

BEGIN;

INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (3, 'Web Development', 'HTML, CSS, JavaScript, Angular and frontend concepts') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (4, 'Software Engineering', 'Design patterns, SDLC, architecture and testing') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (5, 'Cloud & DevOps', 'AWS, Azure, CI/CD, containers and deployment fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (6, 'Web APIs', 'Web api fundaentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (1, 'C# & .NET', 'C# programming using Microsoft .NET') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (7, 'Logical Reasoning', 'Math and more!') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (2, 'Database Systems (DBMS)', 'SQL, PostgreSQL, normalization and database concepts') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (8, 'Mixed', 'Exams combining questions from multiple categories.') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (10, 'Mathematics', 'Math and computations, statistics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (11, 'Python Programming', 'Python syntax, data types, functions, OOP and standard-library essentials') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (12, 'Data Structures & Algorithms', 'Complexity analysis, lists, trees, graphs, sorting and searching') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (13, 'Cybersecurity Fundamentals', 'Web security, authentication, common attacks and defensive practices') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_categories (cat_id, cat_name, description) VALUES (14, 'AI & Machine Learning Basics', 'Core ML concepts: supervised learning, evaluation, overfitting and LLMs') ON CONFLICT DO NOTHING;



INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (21, 'qa_xss_test', 'qa.xss.test@example.test', '$2a$11$c8PdHdPOHrzCp2Jzlr1IieUkUdxhjrRxWlm4v2mYTlyNZxszwi9M6', '<script>alert(1)</script>', 'Candidate', '2026-07-17 08:46:03.486031+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (22, 'qa_candidate2_1784258204', 'qa.candidate2.1784258204@example.test', '$2a$11$aFhKfqkkQdU6ayu2ZK62.e0TW9H1.aoNag5AZhJgQPE0Ndn4zPD6m', 'QA Candidate Two', 'Candidate', '2026-07-17 08:46:43.279175+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (23, 'qa_unicode_1784258309', 'qa.unicode.1784258309@example.test', '$2a$11$zNk3JG5vJw8fAFPjNtFtvOFoaJV52ROkeTc0AF.lBNBtJOmOPPt42', '日本語テスト 🎉🚀', 'Candidate', '2026-07-17 08:48:27.420913+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (24, 'qa_examiner_req', 'qa.examiner.req@example.test', '$2a$11$Nvnvrbd3mJk8Q5.R1C2sI.kHr1uuJ1PfzvWmON1x6mU/6Xy5otU3K', 'QA Examiner', 'Examiner', '2026-07-17 09:25:30.930283+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (25, 'jane', 'jane@gmail.com', '$2a$11$o4TwssJLYxkJwLjr4miFA.2RcRu92PZib0HiZaUV4xlAMy6rlqP0q', 'Jane Smith', 'Examiner', '2026-07-17 10:33:07.757746+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (8, 'profrahul', 'profrahul@gmail.com', '$2a$11$cJiVz2tVh19ED/L9wkuKJ.4xt17bFORwU.xIGnXgg6iEsVB9djRPu', 'Rahul Singh', 'Candidate', '2026-06-30 15:07:14.995359+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (2, 'proframu', 'proframu@gmail.com', '$2a$11$FuLQ63ickbeDMfr3qIRa9O48bqiqPGRhSzjbD1Bgwr39Ab59cIYYu', 'Prof Ramu', 'Examiner', '2026-06-12 15:11:47.850092+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (3, 'profraju', 'profraju@gmail.com', '$2a$11$S5Shvqd65uUbbZQ2sstzreD8gCynm5mZWzdxklFYUZlEm2bgaaDFy', 'Prof Raju', 'Examiner', '2026-06-12 15:12:08.718961+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (4, 'johncandidate', 'john@gmail.com', '$2a$11$WGCgiKBeY5IlmLLP3otuVufTvhpgBrx81WJOb7gTo54Cq8BUo.i1W', 'John Candidate', 'Candidate', '2026-06-15 10:54:28.688299+05:30', false) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (6, 'diya', 'diya@gmail.com', '$2a$11$tjrZ9eS8GI8.XjxGylhMtOPlDut0o6Zo8YkYq7c6vV92rkSc2nX3G', 'Diya', 'Candidate', '2026-06-15 13:58:08.161901+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (5, 'riya', 'sainikinsr@gmail.com', '$2a$11$fmP/zVPt0Sg91E/AEsL8WOEc.21wZTvo4F4ZtxJVyu1qBA5YvemWS', 'Riya', 'Candidate', '2026-06-15 13:57:18.150591+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (9, 'profashok', 'profashok@gmail.com', '$2a$11$xfjMl1g145VnzRNgpn5qM.x/6XW45KO2rXERtZ6maHjz3o/WSGwN6', 'Dr. Ashok Gupta', 'Examiner', '2026-06-30 17:21:29.753349+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (1, 'admin', 'admin@gmail.com', '$2a$11$jm5H7dcZeFMsa/OGPuuloeCCb./1P3KZ2YDz2MYHgXsgzNxXqsG9y', 'Admin', 'Admin', '2026-06-05 12:48:40.705458+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (12, 'candidate_phase1', 'candidate_phase1@gmail.com', '$2a$11$Oczfd3t61oGvJQeSKI9m8uxIIyZs5wVS82lxoyaeV5vuEEX3IQhua', 'candidate_phase1', 'Candidate', '2026-07-09 12:53:04.010128+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (7, 'profdemo', 'profdemo@gmail.com', '$2a$11$XHGYPDAAoqn4T4L1YIYk6ej0qhT.ok3KwOC/FQ8XGJXf76iyu3U2u', 'Prof Demo', 'Examiner', '2026-06-19 14:38:53.427358+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (28, 'raghavnks', 'raghavnks@gmail.com', '$2a$11$ut/XFRT9a7haM8NrFwbA4O6kB9DxylIVxKYtve2Q6n5j748cDg/Nu', 'Prof Raghav', 'Examiner', '2026-07-19 20:19:02.081309+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (18, 'nikisai_003', 'nikisai.003@gmail.com', '$2a$11$N.X0Nj67rj33o37HOfvsSecgRteHVDX./dBoDDJeFL3Vc3LPoJHeK', 'Dr. Nikitha', 'Examiner', '2026-07-16 10:38:19.210318+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (19, 'verify_subpay_0716', 'verify_subpay_0716@example-test.invalid', '$2a$11$5.5OQ7uwxDtZQOZKwrMeLe8KYQ38Wgq.dzLotyQRubOgPWNoeJxAW', 'Subscription Verify Candidate', 'Candidate', '2026-07-16 12:20:28.824339+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) VALUES (20, 'qa_candidate_1784258133', 'qa.candidate.1784258133@example.test', '$2a$11$NWRlruShcMKmZuHP.Bub8uNUixFi7.n/aMJfVNRsM0FCivZeK.Qbm', 'QA Candidate 1784258133', 'Candidate', '2026-07-17 08:45:34.092602+05:30', true) ON CONFLICT DO NOTHING;



INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (31, 25, 11, 'Python Aptitude Mix', 'Mixed-difficulty Python drill. Three attempts — best score counts.', 30, 45.00, 18.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 3) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (17, 2, 8, 'Professional Computer Science Certification (Foundation Level)', '• This is a premium certification examination.
• Read every question carefully before answering.
• Each question carries equal marks.
• No negative marking.
• Switching browser tabs may be recorded for proctoring.
• Ensure a stable internet connection before starting.
• Results will be published after evaluation.
• Candidates scoring 40% or above will receive a digital certificate.', 90, 100.00, 40.00, '2026-07-17 11:35:00+05:30', '2026-08-16 11:35:00+05:30', 'ResultsPublished', '2026-07-17 11:35:41.726184+05:30', false, true, 8000.00, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (28, 18, 2, 'SQL Practice Test', 'Practice SQL queries, joins and normalization. Two attempts allowed.', 30, 95.00, 38.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 2) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (18, 7, 2, 'General exam - Web API', '', 30, 30.00, 15.00, '2026-07-17 12:47:00+05:30', '2026-08-16 12:47:00+05:30', 'Published', '2026-07-17 12:48:00.102252+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (19, 3, 10, 'Advanced Mathematics Aptitude Assessment', '• This examination evaluates analytical and mathematical problem-solving skills.
• Each question carries 5 marks.
• There is no negative marking.
• Calculators are not permitted.
• Rough work may be done separately.
• Submit the examination before the timer expires.
• Candidates scoring 40% or above will pass.', 45, 100.00, 40.00, '2026-07-17 13:44:00+05:30', '2026-08-16 13:44:00+05:30', 'Published', '2026-07-17 13:45:28.63589+05:30', false, true, 10000.00, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (20, 2, 6, 'Web API Mastery Certification', 'Covers REST fundamentals, status codes, security and versioning. Passing earns a verified certificate.', 40, 47.00, 19.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', true, true, 799.00, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (21, 2, 7, 'Logical Reasoning Sprint', 'Timed aptitude practice. You have 3 attempts — your best score counts.', 25, 47.00, 19.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 3) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (7, 3, 4, 'Software Engineering Assessment', NULL, 60, 70.00, 42.00, '2026-06-15 05:30:00+05:30', '2026-06-17 05:29:59+05:30', 'ResultsPublished', '2026-06-15 10:38:34.193184+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (22, 3, 11, 'Python Programming Essentials', 'Core Python for beginners: types, functions, comprehensions and exceptions. Two attempts allowed.', 35, 45.00, 18.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 2) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (23, 3, 12, 'Data Structures Deep Dive', 'Certification-level DSA assessment. Single attempt — read each question carefully.', 45, 45.00, 18.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, true, 999.00, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (24, 9, 13, 'Cybersecurity Basics Challenge', 'Practical web-security awareness test: attacks, defenses and best practices.', 30, 45.00, 18.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 2) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (1, 2, 1, 'C# Certification Assessment', NULL, 60, 100.00, 60.00, '2026-06-17 05:30:00+05:30', '2026-06-19 05:29:59+05:30', 'ResultsPublished', '2026-06-12 17:21:20.598292+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (25, 9, 14, 'AI & ML Foundations', 'Introductory machine-learning concepts — no coding required.', 30, 45.00, 18.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 2) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (26, 7, 6, 'Full Stack Developer Certification', 'Premium certification spanning API design and security. Institute-verified certificate on passing.', 60, 47.00, 19.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', true, true, 1499.00, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (9, 2, 1, 'C# Quick Check — Concurrent Test', '5 quick C# questions. 10 minutes.', 10, 50.00, 25.00, '2026-06-19 05:30:00+05:30', '2028-01-01 05:29:59+05:30', 'ResultsPublished', '2026-06-22 10:51:52.722006+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (27, 7, 3, 'Frontend Fundamentals Refresher', 'Quick HTML/CSS/JS/Angular refresher. Three attempts — best score counts.', 30, 90.00, 36.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 3) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (11, 3, 3, 'General', 'Genreal', 10, 30.00, 15.00, '2026-07-10 14:33:00+05:30', '2026-08-08 14:33:00+05:30', 'ResultsPublished', '2026-07-10 14:33:19.474017+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (8, 7, 5, 'Full Stack & DevOps Capstone Demo Exam', 'Answer all 6 questions covering Cloud/DevOps and Web Development. You have 30 minutes.', 30, 60.00, 36.00, '2026-06-19 05:30:00+05:30', '2027-01-01 05:29:59+05:30', 'ResultsPublished', '2026-06-19 15:02:51.488742+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (12, 2, 8, 'General', 'Attempt all questions.', 35, 35.00, 12.00, '2026-07-11 20:55:00+05:30', '2026-08-08 20:55:00+05:30', 'ResultsPublished', '2026-07-11 20:56:03.668933+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (30, 25, 4, 'Software Engineering Principles', 'SDLC, design patterns and testing fundamentals.', 35, 70.00, 28.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 2) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (29, 18, 5, 'Cloud Concepts Quiz', 'Draft — cloud fundamentals quiz being prepared.', 25, 100.00, 40.00, '2026-07-18 19:19:08.756027+05:30', '2026-09-17 19:19:08.756027+05:30', 'Published', '2026-07-19 19:19:08.756027+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (14, 2, 1, 'STAB TEST Exam2 (auto) 1783838167', 'Temp test.', 30, 40.00, 20.00, '2026-07-12 12:05:07.516854+05:30', '2026-07-12 14:06:07.516854+05:30', 'ResultsPublished', '2026-07-12 12:06:07.65779+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (15, 2, 1, 'STAB Draft 1783866943', NULL, 30, 20.00, 10.00, '2026-07-12 20:10:42.721316+05:30', '2026-07-12 22:05:42.721316+05:30', 'Draft', '2026-07-12 20:05:43.227256+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (16, 3, 2, 'Database Fundamentals', 'Attempt all questions', 20, 30.00, 15.00, '2026-07-13 18:20:00+05:30', '2026-08-12 18:20:00+05:30', 'ResultsPublished', '2026-07-13 18:20:24.848135+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (13, 2, 1, 'STAB TEST Exam (auto) 1783837456', 'Temporary stabilization test exam.', 30, 40.00, 20.00, '2026-07-12 11:53:16.251549+05:30', '2026-07-12 13:54:16.251549+05:30', 'ResultsPublished', '2026-07-12 11:54:16.332585+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;
INSERT INTO public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at, is_featured, is_premium, price, max_attempts) VALUES (32, 28, 14, 'AI fundamentals', 'Attempt all', 20, 30.00, 15.00, '2026-07-19 20:33:00+05:30', '2026-08-18 20:33:00+05:30', 'ResultsPublished', '2026-07-19 20:33:12.341931+05:30', false, false, NULL, 1) ON CONFLICT DO NOTHING;



INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (2, 5, 7, '2026-06-16 17:21:55.394552+05:30', '2026-06-16 19:42:50.494456+05:30', 'AutoSubmitted', 0.00, false, 8455, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (3, 5, 7, '2026-06-16 20:07:17.441394+05:30', '2026-06-16 20:14:49.630904+05:30', 'Submitted', 70.00, true, 452, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (4, 6, 1, '2026-06-17 19:19:25.65679+05:30', '2026-06-17 19:34:42.991807+05:30', 'Submitted', 10.00, false, 917, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (6, 6, 9, '2026-06-22 11:41:46.03686+05:30', '2026-06-22 11:51:49.388576+05:30', 'AutoSubmitted', 10.00, false, 603, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (5, 5, 9, '2026-06-22 11:39:55.265562+05:30', '2026-06-22 11:52:28.34149+05:30', 'AutoSubmitted', 40.00, true, 753, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (7, 5, 8, '2026-07-03 17:19:30.781303+05:30', '2026-07-03 17:21:22.20053+05:30', 'Submitted', 60.00, true, 111, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (8, 5, 8, '2026-07-08 10:53:35.811362+05:30', '2026-07-08 10:54:41.85119+05:30', 'Submitted', 40.00, true, 66, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (9, 5, 11, '2026-07-10 14:36:48.495394+05:30', '2026-07-10 14:36:59.069508+05:30', 'Submitted', 30.00, true, 10, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (10, 12, 8, '2026-07-10 15:25:53.433986+05:30', '2026-07-10 15:28:45.13798+05:30', 'Submitted', 60.00, true, 171, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (11, 5, 12, '2026-07-11 21:00:21.311763+05:30', '2026-07-11 21:01:15.706734+05:30', 'Submitted', 5.00, false, 54, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (12, 6, 12, '2026-07-12 10:44:46.023871+05:30', '2026-07-12 10:45:08.369752+05:30', 'Submitted', 0.00, false, 22, 0, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (13, 6, 13, '2026-07-12 11:55:22.557371+05:30', '2026-07-12 11:55:44.917106+05:30', 'Submitted', 30.00, true, 22, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (14, 6, 14, '2026-07-12 12:06:27.657605+05:30', '2026-07-12 12:06:28.478297+05:30', 'Submitted', 40.00, true, 0, 0, '/assets/candidates/6/attempt_14_scores.json') ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (15, 6, 16, '2026-07-13 18:36:19.13636+05:30', '2026-07-13 18:38:02.124462+05:30', 'Submitted', 30.00, true, 102, 0, '/assets/candidates/6/attempt_15_scores.json') ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (16, 5, 17, '2026-07-17 11:54:08.849513+05:30', '2026-07-17 12:09:59.051444+05:30', 'Submitted', 90.00, true, 950, 1, '/assets/candidates/5/attempt_16_scores.json') ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (17, 5, 32, '2026-07-19 20:36:39.019991+05:30', '2026-07-19 20:38:30.812651+05:30', 'Submitted', 10.00, false, 111, 2, '/assets/candidates/5/attempt_17_scores.json') ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (18, 6, 32, '2026-07-19 20:39:59.291346+05:30', '2026-07-19 20:41:23.209741+05:30', 'Submitted', 30.00, true, 83, 0, '/assets/candidates/6/attempt_18_scores.json') ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (19, 12, 32, '2026-07-19 20:41:51.363584+05:30', '2026-07-19 20:42:58.29176+05:30', 'Submitted', 30.00, true, 66, 3, '/assets/candidates/12/attempt_19_scores.json') ON CONFLICT DO NOTHING;
INSERT INTO public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds, tab_switch_count, topic_scores_url) VALUES (20, 6, 18, '2026-07-20 09:10:03.48636+05:30', NULL, 'InProgress', NULL, NULL, NULL, 0, NULL) ON CONFLICT DO NOTHING;



INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (48, 7, 3, 'Which HTTP method is idempotent and reads data?', 10.00, 'Easy', '2026-06-19 14:56:29.115296+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (49, 7, 3, 'Which lifecycle hook runs first in Angular?', 10.00, 'Medium', '2026-06-19 14:56:29.154358+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (50, 7, 3, 'What is a Promise in JavaScript?', 10.00, 'Medium', '2026-06-19 14:56:29.203403+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (51, 7, 3, 'Which Angular feature enables two-way binding?', 10.00, 'Medium', '2026-06-19 14:56:29.221599+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (1, 2, 1, 'Which keyword is used to define a class in C#?', 10.00, 'Easy', '2026-06-12 16:19:37.715165+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (2, 2, 1, 'Which symbol is used for inheritance in C#?', 10.00, 'Easy', '2026-06-12 16:19:37.71533+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (3, 2, 1, 'What keyword is used to define an interface?', 10.00, 'Easy', '2026-06-12 16:19:37.715344+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (4, 2, 1, 'Which keyword is used to declare an abstract class?', 10.00, 'Easy', '2026-06-12 16:19:37.715345+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (5, 2, 1, 'Which collection stores a sequence of elements in C#?', 10.00, 'Easy', '2026-06-12 16:19:37.715346+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (6, 2, 1, 'Which LINQ method is used to filter records?', 10.00, 'Medium', '2026-06-12 16:19:37.715348+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (7, 2, 1, 'Which keyword is used to define an asynchronous method?', 10.00, 'Medium', '2026-06-12 16:19:37.715348+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (8, 2, 1, 'Which type represents a delegate in modern C#?', 10.00, 'Medium', '2026-06-12 16:19:37.715349+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (9, 2, 1, 'Which ORM framework are we using in this project?', 10.00, 'Medium', '2026-06-12 16:19:37.71535+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (10, 2, 1, 'What is Dependency Injection primarily used for?', 10.00, 'Hard', '2026-06-12 16:19:37.71535+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (11, 2, 2, 'What is a Primary Key?', 10.00, 'Easy', '2026-06-12 16:31:37.127335+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (12, 2, 2, 'What is a Foreign Key?', 10.00, 'Easy', '2026-06-12 16:31:37.127392+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (13, 2, 2, 'What is normalization used for?', 10.00, 'Medium', '2026-06-12 16:31:37.127393+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (14, 2, 2, 'Which JOIN returns matching rows from both tables?', 10.00, 'Medium', '2026-06-12 16:31:37.127394+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (15, 2, 2, 'Why are indexes used?', 10.00, 'Medium', '2026-06-12 16:31:37.127395+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (16, 3, 4, 'What does SDLC stand for?', 10.00, 'Easy', '2026-06-12 16:39:03.952014+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (17, 3, 4, 'Which design pattern creates only one instance of a class?', 10.00, 'Medium', '2026-06-12 16:39:03.952037+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (18, 3, 4, 'What is the purpose of unit testing?', 10.00, 'Medium', '2026-06-12 16:39:03.952038+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (19, 3, 4, 'Which Agile ceremony reviews completed work?', 10.00, 'Medium', '2026-06-12 16:39:03.952039+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (20, 3, 4, 'Which architecture principle promotes loose coupling?', 10.00, 'Hard', '2026-06-12 16:39:03.952039+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (21, 3, 3, 'Which language is used to structure web pages?', 10.00, 'Easy', '2026-06-12 16:39:10.264836+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (22, 3, 3, 'Which language is used for styling web pages?', 10.00, 'Easy', '2026-06-12 16:39:10.264848+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (23, 3, 3, 'Which JavaScript framework are we learning?', 10.00, 'Easy', '2026-06-12 16:39:10.264849+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (24, 3, 3, 'What directive is used for looping in Angular?', 10.00, 'Medium', '2026-06-12 16:39:10.26485+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (25, 3, 3, 'Which Angular feature manages application routes?', 10.00, 'Medium', '2026-06-12 16:39:10.264856+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (26, 2, 1, 'Which keyword is used to define a class in C#?', 10.00, 'Easy', '2026-06-13 08:04:51.923279+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (27, 2, 1, 'Which symbol is used for inheritance in C#?', 10.00, 'Easy', '2026-06-13 08:04:51.923303+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (28, 2, 1, 'What keyword is used to define an interface?', 10.00, 'Easy', '2026-06-13 08:04:51.923304+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (29, 2, 1, 'Which keyword is used to declare an abstract class?', 10.00, 'Easy', '2026-06-13 08:04:51.923306+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (30, 2, 1, 'Which collection stores a sequence of elements in C#?', 10.00, 'Easy', '2026-06-13 08:04:51.923313+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (31, 2, 1, 'Which LINQ method is used to filter records?', 10.00, 'Medium', '2026-06-13 08:04:51.923314+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (32, 2, 1, 'Which keyword is used to define an asynchronous method?', 10.00, 'Medium', '2026-06-13 08:04:51.923314+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (33, 2, 1, 'Which type represents a delegate in modern C#?', 10.00, 'Medium', '2026-06-13 08:04:51.923315+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (34, 2, 1, 'Which ORM framework are we using in this project?', 10.00, 'Medium', '2026-06-13 08:04:51.923316+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (35, 2, 1, 'What is Dependency Injection primarily used for?', 10.00, 'Hard', '2026-06-13 08:04:51.923317+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (36, 3, 4, 'What does SOLID stand for?', 10.00, 'Easy', '2026-06-15 08:27:06.727658+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (37, 3, 4, 'What is Unit Testing?', 10.00, 'Easy', '2026-06-15 08:27:06.731075+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (38, 7, 5, 'What does CI/CD stand for?', 10.00, 'Easy', '2026-06-19 14:47:47.353979+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (39, 7, 5, 'Which AWS service is used for object storage?', 10.00, 'Easy', '2026-06-19 14:47:47.356507+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (40, 7, 5, 'Which command builds a Docker image from a Dockerfile?', 10.00, 'Easy', '2026-06-19 14:47:47.357384+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (41, 7, 5, 'What is Kubernetes primarily used for?', 10.00, 'Medium', '2026-06-19 14:47:47.358538+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (42, 7, 5, 'Which file defines a Docker image''s build steps?', 10.00, 'Easy', '2026-06-19 14:47:47.359451+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (43, 7, 5, 'What is Infrastructure as Code (IaC)?', 10.00, 'Medium', '2026-06-19 14:47:47.360173+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (44, 7, 5, 'Which Azure service hosts serverless functions?', 10.00, 'Medium', '2026-06-19 14:47:47.36089+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (45, 7, 5, 'What is the purpose of a load balancer?', 10.00, 'Medium', '2026-06-19 14:47:47.366564+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (46, 7, 5, 'Which tool is commonly used for CI/CD pipelines in DevOps?', 10.00, 'Medium', '2026-06-19 14:47:47.367385+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (47, 7, 5, 'What is the main benefit of blue-green deployment?', 10.00, 'Hard', '2026-06-19 14:47:47.36796+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (52, 3, 1, 'What is Dependency Injection primarily used for?', 10.00, 'Medium', '2026-07-03 11:29:19.834758+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (53, 2, 2, 'Test question', 5.00, 'Hard', '2026-07-11 20:57:00.759685+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (61, 2, 8, 'Which keyword is used to define a class in C#?', 5.00, 'Easy', '2026-07-17 11:43:28.60236+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (54, 2, 1, 'STAB temp updated?', 6.00, 'Medium', '2026-07-12 12:08:43.714624+05:30', false) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (55, 3, 2, 'Which ACID property ensures that all operations within a transaction are completed successfully or none are?', 10.00, 'Easy', '2026-07-13 18:33:38.219332+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (56, 3, 2, 'Which of the following normal forms is strictly concerned with eliminating partial functional dependencies?', 10.00, 'Medium', '2026-07-13 18:33:38.291049+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (57, 3, 2, 'What type of join returns all rows from the left table, and the matched rows from the right table, filling with NULL if no match exists?', 10.00, 'Easy', '2026-07-13 18:33:38.30764+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (58, 3, 2, 'Which command is classified as a Data Definition Language (DDL) command that permanently removes all records and frees the space?', 10.00, 'Medium', '2026-07-13 18:33:38.31346+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (59, 2, 8, 'Which data structure follows the Last In First Out (LIFO) principle?', 5.00, 'Easy', '2026-07-17 11:43:28.33139+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (60, 2, 8, 'Which SQL statement is used to retrieve data from a database?', 5.00, 'Easy', '2026-07-17 11:43:28.592209+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (62, 2, 8, 'Which HTTP method is commonly used to create a new resource?', 5.00, 'Easy', '2026-07-17 11:43:28.622983+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (63, 2, 8, 'Which normal form removes partial dependency in database normalization?', 5.00, 'Easy', '2026-07-17 11:43:28.634743+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (64, 2, 8, 'Which symbol is used to inherit a class in C#?', 5.00, 'Easy', '2026-07-17 11:43:28.64462+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (65, 2, 8, 'Which collection stores key-value pairs in C#?', 5.00, 'Medium', '2026-07-17 11:43:28.648734+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (66, 2, 8, 'Which LINQ method filters a collection based on a condition?', 5.00, 'Medium', '2026-07-17 11:43:28.653455+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (67, 2, 8, 'Which design principle promotes programming to abstractions instead of implementations?', 5.00, 'Medium', '2026-07-17 11:43:28.678036+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (68, 2, 8, 'Which authentication mechanism is used in this project?', 5.00, 'Medium', '2026-07-17 11:43:28.688236+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (69, 2, 8, 'Which Entity Framework feature maps C# objects to database tables?', 5.00, 'Medium', '2026-07-17 11:43:28.710463+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (70, 2, 8, 'Which HTTP status code indicates Forbidden access?', 5.00, 'Medium', '2026-07-17 11:43:28.731036+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (71, 2, 8, 'What is the time complexity of binary search on a sorted array?', 5.00, 'Medium', '2026-07-17 11:43:28.745258+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (72, 2, 8, 'Which SQL JOIN returns matching records from both tables only?', 5.00, 'Medium', '2026-07-17 11:43:28.755934+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (73, 2, 8, 'Which C# keyword is used to define an asynchronous method?', 5.00, 'Medium', '2026-07-17 11:43:28.777278+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (74, 2, 8, 'Which scheduling algorithm gives the shortest job highest priority?', 5.00, 'Hard', '2026-07-17 11:43:28.797186+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (75, 2, 8, 'Which ACID property ensures that a transaction is completed entirely or not at all?', 5.00, 'Hard', '2026-07-17 11:43:28.817778+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (76, 2, 8, 'Which SOLID principle allows adding new functionality without modifying existing code?', 5.00, 'Hard', '2026-07-17 11:43:28.822039+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (77, 2, 8, 'Which index structure is most commonly used internally by relational databases?', 5.00, 'Hard', '2026-07-17 11:43:28.841328+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (78, 2, 8, 'In JWT, which part contains the claims about the authenticated user?', 5.00, 'Hard', '2026-07-17 11:43:28.847199+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (79, 7, 8, 'Which data structure follows the Last In First Out (LIFO) principle?', 5.00, 'Easy', '2026-07-17 12:53:29.217045+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (80, 7, 8, 'Which SQL statement is used to retrieve data from a database?', 5.00, 'Easy', '2026-07-17 12:53:29.237225+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (81, 7, 8, 'Which keyword is used to define a class in C#?', 5.00, 'Easy', '2026-07-17 12:53:29.244582+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (82, 7, 8, 'Which HTTP method is commonly used to create a new resource?', 5.00, 'Easy', '2026-07-17 12:53:29.260527+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (83, 7, 8, 'Which normal form removes partial dependency in database normalization?', 5.00, 'Easy', '2026-07-17 12:53:29.269419+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (84, 7, 8, 'Which symbol is used to inherit a class in C#?', 5.00, 'Easy', '2026-07-17 12:53:29.299072+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (85, 7, 8, 'Which collection stores key-value pairs in C#?', 5.00, 'Medium', '2026-07-17 12:53:29.313698+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (86, 7, 8, 'Which LINQ method filters a collection based on a condition?', 5.00, 'Medium', '2026-07-17 12:53:29.372575+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (87, 7, 8, 'Which design principle promotes programming to abstractions instead of implementations?', 5.00, 'Medium', '2026-07-17 12:53:29.437664+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (88, 7, 8, 'Which authentication mechanism is used in this project?', 5.00, 'Medium', '2026-07-17 12:53:29.467986+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (89, 7, 8, 'Which Entity Framework feature maps C# objects to database tables?', 5.00, 'Medium', '2026-07-17 12:53:29.489157+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (90, 7, 8, 'Which HTTP status code indicates Forbidden access?', 5.00, 'Medium', '2026-07-17 12:53:29.507342+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (91, 7, 8, 'What is the time complexity of binary search on a sorted array?', 5.00, 'Medium', '2026-07-17 12:53:29.522109+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (92, 7, 8, 'Which SQL JOIN returns matching records from both tables only?', 5.00, 'Medium', '2026-07-17 12:53:29.531432+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (93, 7, 8, 'Which C# keyword is used to define an asynchronous method?', 5.00, 'Medium', '2026-07-17 12:53:29.535092+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (94, 7, 8, 'Which scheduling algorithm gives the shortest job highest priority?', 5.00, 'Hard', '2026-07-17 12:53:29.539492+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (95, 7, 8, 'Which ACID property ensures that a transaction is completed entirely or not at all?', 5.00, 'Hard', '2026-07-17 12:53:29.542778+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (96, 7, 8, 'Which SOLID principle allows adding new functionality without modifying existing code?', 5.00, 'Hard', '2026-07-17 12:53:29.546285+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (97, 7, 8, 'Which index structure is most commonly used internally by relational databases?', 5.00, 'Hard', '2026-07-17 12:53:29.55083+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (98, 7, 8, 'In JWT, which part contains the claims about the authenticated user?', 5.00, 'Hard', '2026-07-17 12:53:29.561153+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (99, 3, 10, 'What is the derivative of x²?', 5.00, 'Easy', '2026-07-17 14:33:57.225533+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (100, 3, 10, 'What is the value of π approximately?', 5.00, 'Easy', '2026-07-17 14:33:57.349331+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (101, 3, 10, 'What is the square root of 144?', 5.00, 'Easy', '2026-07-17 14:33:57.383895+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (102, 3, 10, 'Which number is a prime number?', 5.00, 'Easy', '2026-07-17 14:33:57.400803+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (103, 3, 10, 'What is the value of 15 × 8?', 5.00, 'Easy', '2026-07-17 14:33:57.408617+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (104, 3, 10, 'What is the integral of 1 dx?', 5.00, 'Medium', '2026-07-17 14:33:57.423279+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (105, 3, 10, 'Which theorem relates the sides of a right triangle?', 5.00, 'Medium', '2026-07-17 14:33:57.44058+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (106, 3, 10, 'What is the determinant of a 2×2 matrix [[a,b],[c,d]]?', 5.00, 'Medium', '2026-07-17 14:33:57.446434+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (107, 3, 10, 'If sin θ = 1 then θ equals?', 5.00, 'Medium', '2026-07-17 14:33:57.452009+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (108, 3, 10, 'How many degrees are there in a straight angle?', 5.00, 'Medium', '2026-07-17 14:33:57.459384+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (109, 3, 10, 'What is log₁₀(1000)?', 5.00, 'Medium', '2026-07-17 14:33:57.472361+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (110, 3, 10, 'Which sequence has a constant difference?', 5.00, 'Medium', '2026-07-17 14:33:57.479554+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (111, 3, 10, 'The derivative of sin(x) is?', 5.00, 'Hard', '2026-07-17 14:33:57.490765+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (112, 3, 10, 'The value of e is approximately?', 5.00, 'Hard', '2026-07-17 14:33:57.500228+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (113, 3, 10, 'What is the probability of getting a head on a fair coin toss?', 5.00, 'Hard', '2026-07-17 14:33:57.511677+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (114, 3, 10, 'Which distribution is commonly known as the bell curve?', 5.00, 'Hard', '2026-07-17 14:33:57.52211+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (115, 3, 10, 'What is the solution of x²−9=0?', 5.00, 'Hard', '2026-07-17 14:33:57.532651+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (116, 3, 10, 'The sum of angles in a triangle equals?', 5.00, 'Hard', '2026-07-17 14:33:57.537223+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (117, 3, 10, 'What is the value of 7!?', 5.00, 'Hard', '2026-07-17 14:33:57.543048+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (118, 3, 10, 'Which branch of mathematics deals with limits and derivatives?', 5.00, 'Hard', '2026-07-17 14:33:57.551822+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (119, 2, 6, 'Which HTTP method is idempotent and used to fully replace a resource?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (120, 2, 6, 'What does HTTP status 201 indicate?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (121, 2, 6, 'In REST, what does HATEOAS provide?', 6.00, 'Hard', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (122, 2, 6, 'Which header carries a JWT bearer token?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (123, 2, 6, 'What status code should a missing resource return?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (124, 2, 6, 'Which HTTP status indicates the client sent invalid data?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (125, 2, 6, 'What is the main purpose of API versioning?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (126, 2, 6, 'Which is a common strategy to protect an API from abuse?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (127, 2, 6, 'What does CORS control?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (128, 2, 6, 'A 409 Conflict is most appropriate when…', 6.00, 'Hard', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (129, 2, 7, 'Find the next number: 2, 6, 12, 20, 30, ?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (130, 2, 7, 'If all Bloops are Razzies and all Razzies are Lubs, then all Bloops are…', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (131, 2, 7, 'A clock shows 3:15. What is the angle between the hands?', 6.00, 'Hard', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (132, 2, 7, 'Complete the series: A, C, F, J, O, ?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (133, 2, 7, 'If MONDAY is coded as NPOEBZ, how is FRIDAY coded?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (134, 2, 7, 'A is B''s sister. C is B''s mother. D is C''s father. How is A related to D?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (135, 2, 7, 'Which number is the odd one out: 3, 5, 11, 14, 17, 21?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (136, 2, 7, 'Pointing to a photo, Ram says ''She is the daughter of my grandfather''s only son.'' Who is she?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (137, 2, 7, 'Water is to pipe as electricity is to…', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (138, 2, 7, 'A train 150 m long passes a pole in 15 seconds. Its speed is…', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (139, 3, 11, 'What is the output of: print(type([]))?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (140, 3, 11, 'Which keyword defines an anonymous function in Python?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (141, 3, 11, 'What does a list comprehension [x*x for x in range(3)] produce?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (142, 3, 11, 'Which collection type is immutable?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (143, 3, 11, 'What does the ''self'' parameter refer to?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (144, 3, 11, 'Which statement handles exceptions?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (145, 3, 11, 'What is the result of 7 // 2 in Python 3?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (146, 3, 11, 'Which decorator makes a method callable on the class rather than an instance?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (147, 3, 11, 'What does the GIL (Global Interpreter Lock) limit?', 6.00, 'Hard', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (148, 3, 11, 'Which built-in creates key/value pairs from two lists?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (149, 3, 12, 'What is the average time complexity of binary search?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (150, 3, 12, 'Which data structure uses FIFO ordering?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (151, 3, 12, 'Which structure gives O(1) average lookup by key?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (152, 3, 12, 'What is the worst-case complexity of quicksort?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (153, 3, 12, 'In a max-heap, the largest element is at…', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (154, 3, 12, 'Which traversal of a BST yields sorted order?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (155, 3, 12, 'Which algorithm finds shortest paths from one source with non-negative weights?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (156, 3, 12, 'A stack overflow during recursion is caused by…', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (157, 3, 12, 'Which structure best implements undo functionality?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (158, 3, 12, 'Amortized time for appending to a dynamic array is…', 6.00, 'Hard', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (159, 9, 13, 'What does SQL injection exploit?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (160, 9, 13, 'What is the purpose of hashing passwords?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (161, 9, 13, 'Which attack tricks a user into clicking a disguised element?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (162, 9, 13, 'What does HTTPS add over HTTP?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (163, 9, 13, 'What is the principle of least privilege?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (164, 9, 13, 'XSS attacks inject…', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (165, 9, 13, 'What does 2FA add to authentication?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (166, 9, 13, 'A CSRF attack abuses…', 6.00, 'Hard', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (167, 9, 13, 'Salting a password hash defends against…', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (168, 9, 13, 'Which is an example of social engineering?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (169, 9, 14, 'Supervised learning requires…', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (170, 9, 14, 'Which task is a classification problem?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (171, 9, 14, 'Overfitting means the model…', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (172, 9, 14, 'Which technique helps reduce overfitting?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (173, 9, 14, 'What does a confusion matrix summarize?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (174, 9, 14, 'Gradient descent is used to…', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (175, 9, 14, 'Which is an unsupervised method?', 5.00, 'Medium', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (176, 9, 14, 'A train/test split exists to…', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (177, 9, 14, 'Precision measures…', 6.00, 'Hard', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (178, 9, 14, 'What is a large language model trained to do at its core?', 4.00, 'Easy', '2026-07-19 19:19:08.756027+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (179, 28, 14, 'Who is considered the father of AI?', 5.00, 'Easy', '2026-07-19 20:28:50.990935+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (180, 28, 14, 'What is the main purpose of a validation dataset during model training?', 10.00, 'Medium', '2026-07-19 20:30:33.165257+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (181, 28, 14, 'Which activation function suffers from the vanishing gradient problem for very high or low inputs?', 10.00, 'Easy', '2026-07-19 20:30:33.181761+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (182, 28, 14, 'What technique prevents overfitting by randomly dropping neurons during neural network training?', 10.00, 'Easy', '2026-07-19 20:30:33.188355+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (183, 28, 14, 'In unsupervised learning, which algorithm groups data points based on feature similarity?', 10.00, 'Easy', '2026-07-19 20:30:33.195835+05:30', true) ON CONFLICT DO NOTHING;
INSERT INTO public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) VALUES (184, 28, 14, 'What does the ''Kernel Trick'' allow a Support Vector Machine (SVM) to do?', 10.00, 'Hard', '2026-07-19 20:30:33.202792+05:30', true) ON CONFLICT DO NOTHING;



INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (1, 1, 'class', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (2, 1, 'object', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (3, 1, 'new', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (4, 1, 'struct', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (5, 2, ':', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (6, 2, '::', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (7, 2, '=>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (8, 2, '->', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (9, 3, 'interface', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (10, 3, 'contract', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (11, 3, 'implements', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (12, 3, 'abstract', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (13, 4, 'abstract', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (14, 4, 'virtual', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (15, 4, 'sealed', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (16, 4, 'override', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (17, 5, 'List<T>', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (18, 5, 'Dictionary<TKey,TValue>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (19, 5, 'HashSet<T>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (20, 5, 'Queue<T>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (21, 6, 'Where()', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (22, 6, 'Select()', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (23, 6, 'OrderBy()', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (24, 6, 'GroupBy()', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (25, 7, 'async', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (26, 7, 'await', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (27, 7, 'Task', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (28, 7, 'thread', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (29, 8, 'Func<>', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (30, 8, 'List<>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (31, 8, 'Task<>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (32, 8, 'IEnumerable<>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (33, 9, 'Entity Framework Core', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (34, 9, 'Dapper', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (35, 9, 'NHibernate', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (36, 9, 'ADO.NET Only', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (37, 10, 'Reducing coupling between components', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (38, 10, 'Creating database tables', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (39, 10, 'Improving SQL performance', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (40, 10, 'Replacing interfaces', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (41, 11, 'Uniquely identifies a row', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (42, 11, 'Connects two tables', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (43, 11, 'Stores duplicate values', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (44, 11, 'Sorts data automatically', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (45, 12, 'Links tables together', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (46, 12, 'Identifies rows uniquely', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (47, 12, 'Creates indexes', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (48, 12, 'Deletes records', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (49, 13, 'Reduce data redundancy', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (50, 13, 'Increase duplicates', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (51, 13, 'Speed up networking', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (52, 13, 'Encrypt tables', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (53, 14, 'INNER JOIN', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (54, 14, 'LEFT JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (55, 14, 'RIGHT JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (56, 14, 'FULL JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (57, 15, 'Improve query performance', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (58, 15, 'Increase storage', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (59, 15, 'Delete records', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (60, 15, 'Create backups', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (61, 16, 'Software Development Life Cycle', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (62, 16, 'System Design Logic Cycle', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (63, 16, 'Software Delivery Layer Control', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (64, 16, 'System Development Link Chain', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (65, 17, 'Singleton', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (66, 17, 'Factory', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (67, 17, 'Adapter', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (68, 17, 'Strategy', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (69, 18, 'Test individual components', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (70, 18, 'Deploy applications', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (71, 18, 'Manage databases', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (72, 18, 'Create APIs', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (73, 19, 'Sprint Review', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (74, 19, 'Sprint Planning', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (75, 19, 'Daily Standup', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (76, 19, 'Retrospective', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (77, 20, 'Dependency Injection', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (78, 20, 'Global State', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (79, 20, 'Tight Coupling', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (80, 20, 'Static Dependencies', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (81, 21, 'HTML', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (82, 21, 'CSS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (83, 21, 'Java', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (84, 21, 'SQL', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (85, 22, 'CSS', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (86, 22, 'HTML', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (87, 22, 'C#', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (88, 22, 'Python', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (89, 23, 'Angular', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (90, 23, 'Django', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (91, 23, 'Spring', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (92, 23, 'Laravel', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (93, 24, '*ngFor', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (94, 24, '*ngIf', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (95, 24, '*ngLoop', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (96, 24, '*for', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (97, 25, 'RouterModule', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (98, 25, 'FormsModule', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (99, 25, 'HttpClient', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (100, 25, 'NgZone', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (101, 26, 'class', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (102, 26, 'object', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (103, 26, 'new', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (104, 26, 'struct', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (105, 27, ':', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (106, 27, '::', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (107, 27, '=>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (108, 27, '->', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (109, 28, 'interface', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (110, 28, 'contract', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (111, 28, 'implements', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (112, 28, 'abstract', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (113, 29, 'abstract', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (114, 29, 'virtual', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (115, 29, 'sealed', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (116, 29, 'override', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (117, 30, 'List<T>', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (118, 30, 'Dictionary<TKey,TValue>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (119, 30, 'HashSet<T>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (120, 30, 'Queue<T>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (121, 31, 'Where()', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (122, 31, 'Select()', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (123, 31, 'OrderBy()', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (124, 31, 'GroupBy()', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (125, 32, 'async', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (126, 32, 'await', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (127, 32, 'Task', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (128, 32, 'thread', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (129, 33, 'Func<>', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (130, 33, 'List<>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (131, 33, 'Task<>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (132, 33, 'IEnumerable<>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (133, 34, 'Entity Framework Core', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (134, 34, 'Dapper', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (135, 34, 'NHibernate', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (136, 34, 'ADO.NET Only', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (137, 35, 'Reducing coupling between components', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (138, 35, 'Creating database tables', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (139, 35, 'Improving SQL performance', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (140, 35, 'Replacing interfaces', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (141, 36, 'Design Principles', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (142, 36, 'Database Rules', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (143, 36, 'Testing Framework', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (144, 36, 'Deployment Strategy', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (145, 37, 'Testing individual components', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (146, 37, 'Deploying applications', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (147, 37, 'Database optimization', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (148, 37, 'Version control', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (149, 38, 'Continuous Integration / Continuous Delivery', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (150, 38, 'Computed Integration / Computed Delivery', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (151, 38, 'Cloud Integration / Cloud Distribution', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (152, 38, 'Control Integration / Control Deployment', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (153, 39, 'S3', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (154, 39, 'EC2', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (155, 39, 'RDS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (156, 39, 'Lambda', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (157, 40, 'docker build', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (158, 40, 'docker run', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (159, 40, 'docker pull', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (160, 40, 'docker compose', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (161, 41, 'Container orchestration', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (162, 41, 'Database management', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (163, 41, 'Source control', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (164, 41, 'Code compilation', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (165, 42, 'Dockerfile', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (166, 42, 'docker-compose.yml', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (167, 42, 'package.json', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (168, 42, 'Makefile', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (169, 43, 'Managing infra through machine-readable definition files', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (170, 43, 'Writing application code on infrastructure servers', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (171, 43, 'A type of cloud database', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (172, 43, 'An operating system feature', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (173, 44, 'Azure Functions', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (174, 44, 'Azure SQL', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (175, 44, 'Azure Blob Storage', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (176, 44, 'Azure DevOps', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (177, 45, 'Distribute traffic across multiple servers', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (178, 45, 'Store backup data', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (179, 45, 'Encrypt network traffic', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (180, 45, 'Compile code remotely', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (181, 46, 'Jenkins', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (182, 46, 'Postman', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (183, 46, 'Photoshop', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (184, 46, 'Tableau', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (185, 47, 'Zero-downtime releases with easy rollback', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (186, 47, 'Faster CPU performance', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (187, 47, 'Reduced storage cost', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (188, 47, 'Smaller container sizes', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (189, 48, 'GET', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (190, 48, 'POST', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (191, 48, 'DELETE', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (192, 48, 'PATCH', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (193, 49, 'Object representing eventual async completion', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (194, 49, 'A synchronous function', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (195, 49, 'A loop construct', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (196, 49, 'A CSS property', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (197, 50, '[(ngModel)]', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (198, 50, '*ngFor', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (199, 50, '(click)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (200, 50, '[hidden]', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (201, 51, 'Cross-Origin Resource Sharing', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (202, 51, 'Common Origin Request System', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (203, 51, 'Cross-Origin Restricted Service', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (204, 51, 'Content Origin Resource Standard', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (205, 52, 'Decoupling object creation from usage', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (206, 52, 'Compressing database queries', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (207, 52, 'Validating HTTP requests', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (208, 52, 'Encrypting configuration files', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (209, 53, 'a', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (210, 53, 'b', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (211, 53, 'c', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (212, 53, 'd', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (217, 54, 'A', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (218, 54, 'B', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (219, 54, 'C', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (220, 54, 'D', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (221, 55, 'Atomicity', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (222, 55, 'Consistency', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (223, 55, 'Isolation', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (224, 55, 'Durability', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (225, 56, 'First Normal Form (1NF)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (226, 56, 'Second Normal Form (2NF)', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (227, 56, 'Third Normal Form (3NF)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (228, 56, 'Boyce-Codd Normal Form (BCNF)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (229, 57, 'INNER JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (230, 57, 'RIGHT JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (231, 57, 'LEFT JOIN', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (232, 57, 'FULL OUTER JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (233, 58, 'DELETE', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (234, 58, 'TRUNCATE', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (235, 58, 'DROP', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (236, 58, 'ALTER', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (237, 59, 'Queue', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (238, 59, 'Stack', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (239, 59, 'Linked List', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (240, 59, 'Tree', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (241, 60, 'INSERT', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (242, 60, 'UPDATE', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (243, 60, 'SELECT', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (244, 60, 'DELETE', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (245, 61, 'class', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (246, 61, 'struct', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (247, 61, 'object', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (248, 61, 'new', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (249, 62, 'GET', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (250, 62, 'POST', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (251, 62, 'PUT', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (252, 62, 'DELETE', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (253, 63, '1NF', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (254, 63, '2NF', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (255, 63, '3NF', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (256, 63, 'BCNF', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (257, 64, 'extends', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (258, 64, 'inherits', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (259, 64, ':', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (260, 64, 'implements', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (261, 65, 'List', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (262, 65, 'Array', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (263, 65, 'Dictionary', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (264, 65, 'Queue', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (265, 66, 'Select', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (266, 66, 'Where', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (267, 66, 'OrderBy', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (268, 66, 'GroupBy', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (269, 67, 'SOLID', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (270, 67, 'DRY', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (271, 67, 'KISS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (272, 67, 'YAGNI', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (273, 68, 'OAuth', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (274, 68, 'JWT', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (275, 68, 'Basic Authentication', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (276, 68, 'Session Cookies', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (277, 69, 'Reflection', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (278, 69, 'ORM', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (279, 69, 'Serialization', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (280, 69, 'Caching', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (281, 70, '401', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (282, 70, '403', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (283, 70, '404', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (284, 70, '500', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (285, 71, 'O(n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (286, 71, 'O(log n)', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (287, 71, 'O(n log n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (288, 71, 'O(1)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (289, 72, 'LEFT JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (290, 72, 'RIGHT JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (291, 72, 'FULL JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (292, 72, 'INNER JOIN', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (293, 73, 'await', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (294, 73, 'async', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (295, 73, 'task', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (296, 73, 'promise', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (297, 74, 'Round Robin', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (298, 74, 'FCFS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (299, 74, 'SJF', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (300, 74, 'Priority Scheduling', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (301, 75, 'Consistency', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (302, 75, 'Isolation', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (303, 75, 'Atomicity', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (304, 75, 'Durability', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (305, 76, 'Single Responsibility', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (306, 76, 'Open Closed', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (307, 76, 'Liskov Substitution', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (308, 76, 'Dependency Inversion', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (309, 77, 'B-Tree', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (310, 77, 'Heap', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (311, 77, 'Linked List', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (312, 77, 'Trie', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (313, 78, 'Header', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (314, 78, 'Signature', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (315, 78, 'Payload', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (316, 78, 'Secret Key', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (317, 79, 'Queue', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (318, 79, 'Stack', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (319, 79, 'Linked List', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (320, 79, 'Tree', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (321, 80, 'INSERT', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (322, 80, 'UPDATE', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (323, 80, 'SELECT', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (324, 80, 'DELETE', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (325, 81, 'class', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (326, 81, 'struct', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (327, 81, 'object', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (328, 81, 'new', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (329, 82, 'GET', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (330, 82, 'POST', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (331, 82, 'PUT', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (332, 82, 'DELETE', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (333, 83, '1NF', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (334, 83, '2NF', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (335, 83, '3NF', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (336, 83, 'BCNF', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (337, 84, 'extends', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (338, 84, 'inherits', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (339, 84, ':', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (340, 84, 'implements', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (341, 85, 'List', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (342, 85, 'Array', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (343, 85, 'Dictionary', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (344, 85, 'Queue', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (345, 86, 'Select', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (346, 86, 'Where', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (347, 86, 'OrderBy', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (348, 86, 'GroupBy', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (349, 87, 'SOLID', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (350, 87, 'DRY', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (351, 87, 'KISS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (352, 87, 'YAGNI', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (353, 88, 'OAuth', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (354, 88, 'JWT', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (355, 88, 'Basic Authentication', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (356, 88, 'Session Cookies', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (357, 89, 'Reflection', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (358, 89, 'ORM', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (359, 89, 'Serialization', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (360, 89, 'Caching', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (361, 90, '401', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (362, 90, '403', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (363, 90, '404', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (364, 90, '500', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (365, 91, 'O(n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (366, 91, 'O(log n)', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (367, 91, 'O(n log n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (368, 91, 'O(1)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (369, 92, 'LEFT JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (370, 92, 'RIGHT JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (371, 92, 'FULL JOIN', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (372, 92, 'INNER JOIN', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (373, 93, 'await', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (374, 93, 'async', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (375, 93, 'task', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (376, 93, 'promise', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (377, 94, 'Round Robin', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (378, 94, 'FCFS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (379, 94, 'SJF', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (380, 94, 'Priority Scheduling', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (381, 95, 'Consistency', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (382, 95, 'Isolation', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (383, 95, 'Atomicity', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (384, 95, 'Durability', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (385, 96, 'Single Responsibility', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (386, 96, 'Open Closed', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (387, 96, 'Liskov Substitution', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (388, 96, 'Dependency Inversion', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (389, 97, 'B-Tree', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (390, 97, 'Heap', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (391, 97, 'Linked List', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (392, 97, 'Trie', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (393, 98, 'Header', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (394, 98, 'Signature', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (395, 98, 'Payload', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (396, 98, 'Secret Key', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (397, 99, 'x', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (398, 99, '2x', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (399, 99, 'x²', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (400, 99, '2', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (401, 100, '2.14', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (402, 100, '3.14', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (403, 100, '4.14', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (404, 100, '5.14', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (405, 101, '10', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (406, 101, '11', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (407, 101, '12', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (408, 101, '13', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (409, 102, '21', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (410, 102, '35', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (411, 102, '29', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (412, 102, '49', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (413, 103, '110', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (414, 103, '115', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (415, 103, '120', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (416, 103, '125', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (417, 104, '1', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (418, 104, 'x', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (419, 104, 'C', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (420, 104, 'x²/2', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (421, 105, 'Pascal''s Theorem', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (422, 105, 'Pythagoras Theorem', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (423, 105, 'Fermat''s Theorem', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (424, 105, 'Binomial Theorem', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (425, 106, 'ab+cd', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (426, 106, 'ad−bc', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (427, 106, 'a+b+c+d', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (428, 106, 'ac−bd', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (429, 107, '0°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (430, 107, '30°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (431, 107, '90°', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (432, 107, '180°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (433, 108, '90', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (434, 108, '120', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (435, 108, '180', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (436, 108, '360', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (437, 109, '2', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (438, 109, '3', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (439, 109, '10', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (440, 109, '100', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (441, 110, 'Geometric', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (442, 110, 'Fibonacci', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (443, 110, 'Arithmetic', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (444, 110, 'Harmonic', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (445, 111, '#NAME?', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (446, 111, 'cos(x)', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (447, 111, 'tan(x)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (448, 111, 'sec(x)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (449, 112, '2.718', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (450, 112, '3.142', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (451, 112, '1.618', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (452, 112, '1.414', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (453, 113, '04/01/2026 12:00:00 AM', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (454, 113, '03/01/2026 12:00:00 AM', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (455, 113, '02/01/2026 12:00:00 AM', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (456, 113, '1', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (457, 114, 'Poisson', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (458, 114, 'Binomial', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (459, 114, 'Normal', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (460, 114, 'Uniform', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (461, 115, '±3', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (462, 115, '±9', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (463, 115, '3 only', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (464, 115, '-3 only', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (465, 116, '90°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (466, 116, '180°', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (467, 116, '270°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (468, 116, '360°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (469, 117, '720', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (470, 117, '5040', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (471, 117, '40320', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (472, 117, '840', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (473, 118, 'Algebra', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (474, 118, 'Geometry', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (475, 118, 'Calculus', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (476, 118, 'Statistics', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (477, 119, 'POST', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (478, 119, 'PUT', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (479, 119, 'PATCH', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (480, 119, 'OPTIONS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (481, 120, 'Request accepted for async processing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (482, 120, 'Resource created successfully', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (483, 120, 'Resource moved permanently', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (484, 120, 'No content to return', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (485, 121, 'Hypermedia links to navigate the API', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (486, 121, 'Automatic caching of responses', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (487, 121, 'Binary serialization', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (488, 121, 'Load balancing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (489, 122, 'X-Api-Key', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (490, 122, 'Authorization', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (491, 122, 'Content-Type', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (492, 122, 'Accept', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (493, 123, '400', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (494, 123, '401', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (495, 123, '404', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (496, 123, '500', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (497, 124, '400 Bad Request', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (498, 124, '403 Forbidden', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (499, 124, '409 Conflict', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (500, 124, '502 Bad Gateway', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (501, 125, 'Improve response speed', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (502, 125, 'Evolve the API without breaking existing clients', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (503, 125, 'Encrypt payloads', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (504, 125, 'Reduce server memory', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (505, 126, 'Rate limiting', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (506, 126, 'Verbose logging', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (507, 126, 'Long-lived sessions', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (508, 126, 'Disabling HTTPS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (509, 127, 'Database access rules', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (510, 127, 'Which browser origins may call the API', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (511, 127, 'Serialization format', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (512, 127, 'Password hashing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (513, 128, 'the token expired', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (514, 128, 'a duplicate resource insert violates state', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (515, 128, 'the payload is too large', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (516, 128, 'the server is overloaded', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (517, 129, '36', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (518, 129, '40', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (519, 129, '42', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (520, 129, '44', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (521, 130, 'Lubs', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (522, 130, 'Not Lubs', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (523, 130, 'Razzies only', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (524, 130, 'Cannot say', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (525, 131, '0°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (526, 131, '7.5°', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (527, 131, '30°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (528, 131, '37.5°', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (529, 132, 'S', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (530, 132, 'T', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (531, 132, 'U', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (532, 132, 'V', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (533, 133, 'GSJEBZ', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (534, 133, 'GSJEBX', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (535, 133, 'GQJEBZ', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (536, 133, 'GSKEBZ', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (537, 134, 'Granddaughter', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (538, 134, 'Daughter', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (539, 134, 'Aunt', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (540, 134, 'Niece', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (541, 135, '11', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (542, 135, '14', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (543, 135, '17', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (544, 135, '21', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (545, 136, 'His sister', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (546, 136, 'His mother', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (547, 136, 'His cousin', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (548, 136, 'His aunt', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (549, 137, 'Bulb', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (550, 137, 'Wire', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (551, 137, 'Switch', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (552, 137, 'Battery', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (553, 138, '10 m/s', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (554, 138, '12 m/s', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (555, 138, '15 m/s', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (556, 138, '20 m/s', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (557, 139, '<class ''list''>', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (558, 139, '<class ''tuple''>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (559, 139, '<class ''dict''>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (560, 139, '<class ''set''>', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (561, 140, 'def', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (562, 140, 'func', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (563, 140, 'lambda', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (564, 140, 'anon', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (565, 141, '[0, 1, 4]', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (566, 141, '[1, 4, 9]', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (567, 141, '[0, 1, 2]', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (568, 141, '(0, 1, 4)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (569, 142, 'list', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (570, 142, 'dict', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (571, 142, 'set', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (572, 142, 'tuple', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (573, 143, 'The class itself', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (574, 143, 'The current instance', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (575, 143, 'The parent class', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (576, 143, 'A global object', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (577, 144, 'catch/throw', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (578, 144, 'try/except', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (579, 144, 'begin/rescue', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (580, 144, 'on/error', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (581, 145, '3.5', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (582, 145, '3', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (583, 145, '4', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (584, 145, '2', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (585, 146, '@property', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (586, 146, '@staticmethod', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (587, 146, '@classmethod', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (588, 146, '@abstractmethod', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (589, 147, 'Memory allocation', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (590, 147, 'True parallel execution of Python threads', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (591, 147, 'File I/O throughput', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (592, 147, 'Recursion depth', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (593, 148, 'zip() with dict()', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (594, 148, 'map() with list()', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (595, 148, 'join()', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (596, 148, 'enumerate() alone', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (597, 149, 'O(n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (598, 149, 'O(log n)', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (599, 149, 'O(n log n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (600, 149, 'O(1)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (601, 150, 'Stack', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (602, 150, 'Queue', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (603, 150, 'Tree', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (604, 150, 'Graph', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (605, 151, 'Linked list', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (606, 151, 'Hash table', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (607, 151, 'Binary tree', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (608, 151, 'Array', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (609, 152, 'O(n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (610, 152, 'O(n log n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (611, 152, 'O(n²)', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (612, 152, 'O(log n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (613, 153, 'a leaf', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (614, 153, 'the root', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (615, 153, 'the middle', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (616, 153, 'random position', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (617, 154, 'Pre-order', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (618, 154, 'Post-order', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (619, 154, 'In-order', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (620, 154, 'Level-order', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (621, 155, 'Kruskal''s', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (622, 155, 'Dijkstra''s', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (623, 155, 'Prim''s', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (624, 155, 'DFS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (625, 156, 'too many stack frames', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (626, 156, 'heap exhaustion', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (627, 156, 'integer overflow', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (628, 156, 'cache misses', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (629, 157, 'Queue', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (630, 157, 'Stack', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (631, 157, 'Heap', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (632, 157, 'Graph', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (633, 158, 'O(1)', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (634, 158, 'O(n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (635, 158, 'O(log n)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (636, 158, 'O(n²)', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (637, 159, 'Weak passwords', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (638, 159, 'Unsanitized user input in queries', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (639, 159, 'Open ports', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (640, 159, 'Expired certificates', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (641, 160, 'Compression', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (642, 160, 'One-way storage so plaintext is never kept', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (643, 160, 'Faster login', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (644, 160, 'Encryption for transport', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (645, 161, 'Phishing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (646, 161, 'Clickjacking', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (647, 161, 'DDoS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (648, 161, 'Brute force', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (649, 162, 'Compression', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (650, 162, 'Encryption in transit via TLS', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (651, 162, 'Faster routing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (652, 162, 'Caching', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (653, 163, 'Give users admin by default', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (654, 163, 'Grant only the access needed for the task', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (655, 163, 'Rotate passwords weekly', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (656, 163, 'Use one shared account', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (657, 164, 'SQL into the database', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (658, 164, 'scripts into pages viewed by others', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (659, 164, 'packets into routers', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (660, 164, 'keys into TLS', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (661, 165, 'A second independent verification factor', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (662, 165, 'A longer password', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (663, 165, 'Biometric only', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (664, 165, 'Session caching', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (665, 166, 'stored server sessions of a logged-in user', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (666, 166, 'DNS resolution', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (667, 166, 'weak hashing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (668, 166, 'open Wi-Fi', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (669, 167, 'brute force on live systems', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (670, 167, 'rainbow-table precomputation', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (671, 167, 'phishing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (672, 167, 'keyloggers', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (673, 168, 'Port scanning', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (674, 168, 'Pretexting phone call to reveal a password', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (675, 168, 'SYN flood', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (676, 168, 'ARP spoofing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (677, 169, 'unlabeled data', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (678, 169, 'labeled training examples', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (679, 169, 'a reward signal', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (680, 169, 'no data', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (681, 170, 'Predicting house price', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (682, 170, 'Detecting spam emails', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (683, 170, 'Forecasting temperature', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (684, 170, 'Estimating age in years', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (685, 171, 'performs poorly on training data', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (686, 171, 'memorizes training data and fails to generalize', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (687, 171, 'is too simple', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (688, 171, 'trains too slowly', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (689, 172, 'More model parameters', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (690, 172, 'Regularization', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (691, 172, 'Higher learning rate', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (692, 172, 'Removing validation set', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (693, 173, 'Feature correlations', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (694, 173, 'Classification prediction outcomes', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (695, 173, 'Training speed', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (696, 173, 'Data cleaning steps', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (697, 174, 'label data', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (698, 174, 'minimize a loss function', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (699, 174, 'split datasets', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (700, 174, 'encrypt weights', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (701, 175, 'Linear regression', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (702, 175, 'K-means clustering', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (703, 175, 'Logistic regression', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (704, 175, 'Decision-tree classification', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (705, 176, 'speed up training', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (706, 176, 'measure generalization on unseen data', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (707, 176, 'balance classes', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (708, 176, 'reduce memory', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (709, 177, 'correct positives among predicted positives', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (710, 177, 'correct positives among actual positives', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (711, 177, 'overall accuracy', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (712, 177, 'error variance', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (713, 178, 'Rank web pages', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (714, 178, 'Predict the next token in text', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (715, 178, 'Compress images', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (716, 178, 'Solve equations symbolically', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (717, 179, 'Alan Turing', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (718, 179, 'John McCarthy', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (719, 179, 'Marvin Minsky', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (720, 179, 'Geoffrey Hinton', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (721, 180, 'To train model weights', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (722, 180, 'To tune hyperparameters', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (723, 180, 'To test final deployment', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (724, 180, 'To clean missing data', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (725, 181, 'ReLU', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (726, 181, 'LeakyReLU', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (727, 181, 'Sigmoid', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (728, 181, 'ELU', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (729, 182, 'Dropout', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (730, 182, 'Batch Normalization', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (731, 182, 'Gradient Descent', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (732, 182, 'Data Augmentation', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (733, 183, 'Linear Regression', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (734, 183, 'Random Forest', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (735, 183, 'K-Means', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (736, 183, 'Support Vector Machine', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (737, 184, 'Speed up gradient descent', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (738, 184, 'Separate non-linearly separable data', true) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (739, 184, 'Compress the input features', false) ON CONFLICT DO NOTHING;
INSERT INTO public.question_options (option_id, question_id, option_text, is_correct) VALUES (740, 184, 'Automatically label raw data', false) ON CONFLICT DO NOTHING;



INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (80, 16, 72, 292, true, 5.00, '2026-07-17 12:09:22.648034+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (81, 16, 77, 309, true, 5.00, '2026-07-17 12:09:27.528039+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (82, 16, 63, 254, true, 5.00, '2026-07-17 12:09:36.334299+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (83, 16, 74, 299, true, 5.00, '2026-07-17 12:09:42.463156+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (84, 16, 68, 274, true, 5.00, '2026-07-17 12:09:51.236306+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (85, 16, 65, 263, true, 5.00, '2026-07-17 12:09:55.371404+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (47, 11, 6, 24, false, 0.00, '2026-07-11 21:00:26.602215+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (2, 3, 16, 61, true, 10.00, '2026-06-16 20:08:25.20829+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (3, 3, 17, 65, true, 10.00, '2026-06-16 20:08:54.782025+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (4, 3, 18, 69, true, 10.00, '2026-06-16 20:09:54.765512+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (5, 3, 19, 73, true, 10.00, '2026-06-16 20:10:22.465843+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (6, 3, 20, 77, true, 10.00, '2026-06-16 20:10:28.5615+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (7, 3, 36, 141, true, 10.00, '2026-06-16 20:10:40.653567+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (8, 3, 37, 145, true, 10.00, '2026-06-16 20:10:49.743538+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (48, 11, 53, 211, true, 5.00, '2026-07-11 21:00:48.533668+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (9, 4, 1, 2, false, 0.00, '2026-06-17 19:30:27.166197+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (10, 4, 2, 7, false, 0.00, '2026-06-17 19:30:51.554897+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (11, 4, 3, 9, true, 10.00, '2026-06-17 19:31:22.28692+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (12, 4, 4, 14, false, 0.00, '2026-06-17 19:31:34.348857+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (13, 4, 5, 19, false, 0.00, '2026-06-17 19:32:15.292064+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (14, 4, 6, 22, false, 0.00, '2026-06-17 19:33:12.784158+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (15, 4, 7, 28, false, 0.00, '2026-06-17 19:33:24.127864+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (16, 4, 8, 30, false, 0.00, '2026-06-17 19:33:48.312331+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (17, 4, 9, 35, false, 0.00, '2026-06-17 19:33:52.563381+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (18, 4, 10, 40, false, 0.00, '2026-06-17 19:33:57.413017+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (24, 6, 1, 2, false, 0.00, '2026-06-22 11:51:35.609267+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (25, 6, 2, 5, true, 10.00, '2026-06-22 11:51:45.431147+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (19, 5, 1, 1, true, 10.00, '2026-06-22 11:47:56.21892+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (20, 5, 2, 5, true, 10.00, '2026-06-22 11:48:02.983299+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (21, 5, 3, 9, true, 10.00, '2026-06-22 11:48:12.506266+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (22, 5, 4, 14, false, 0.00, '2026-06-22 11:48:18.734856+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (23, 5, 5, 17, true, 10.00, '2026-06-22 11:48:27.835107+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (49, 11, 5, 19, false, 0.00, '2026-07-11 21:00:52.640568+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (50, 11, 3, 10, false, 0.00, '2026-07-11 21:01:07.911702+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (26, 7, 38, 149, true, 10.00, '2026-07-03 17:19:33.927629+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (27, 7, 39, 153, true, 10.00, '2026-07-03 17:19:38.352215+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (28, 7, 40, 157, true, 10.00, '2026-07-03 17:19:44.095924+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (29, 7, 50, 197, true, 10.00, '2026-07-03 17:21:12.375604+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (30, 7, 49, 193, true, 10.00, '2026-07-03 17:21:15.336208+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (31, 7, 48, 189, true, 10.00, '2026-07-03 17:21:19.809791+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (32, 8, 38, 149, true, 10.00, '2026-07-08 10:53:38.043137+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (33, 8, 39, 153, true, 10.00, '2026-07-08 10:53:40.54862+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (34, 8, 40, 158, false, 0.00, '2026-07-08 10:53:42.261838+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (35, 8, 48, 191, false, 0.00, '2026-07-08 10:53:43.104697+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (36, 8, 49, 193, true, 10.00, '2026-07-08 10:53:44.624052+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (37, 8, 50, 197, true, 10.00, '2026-07-08 10:53:49.223421+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (38, 9, 16, 61, true, 10.00, '2026-07-10 14:36:50.819785+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (39, 9, 18, 69, true, 10.00, '2026-07-10 14:36:53.254509+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (40, 9, 20, 77, true, 10.00, '2026-07-10 14:36:54.747031+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (41, 10, 38, 149, true, 10.00, '2026-07-10 15:25:55.537756+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (42, 10, 39, 153, true, 10.00, '2026-07-10 15:26:47.470928+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (43, 10, 40, 157, true, 10.00, '2026-07-10 15:27:19.78376+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (44, 10, 48, 189, true, 10.00, '2026-07-10 15:27:21.353348+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (45, 10, 49, 193, true, 10.00, '2026-07-10 15:27:22.49057+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (46, 10, 50, 197, true, 10.00, '2026-07-10 15:27:24.025474+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (51, 12, 5, 20, false, 0.00, '2026-07-12 10:44:48.91977+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (52, 12, 3, 12, false, 0.00, '2026-07-12 10:44:51.182236+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (53, 12, 6, 22, false, 0.00, '2026-07-12 10:44:52.912929+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (54, 12, 53, 209, false, 0.00, '2026-07-12 10:45:00.895982+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (55, 13, 3, 9, true, 10.00, '2026-07-12 11:55:44.651719+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (56, 13, 5, 17, true, 10.00, '2026-07-12 11:55:44.701496+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (57, 13, 6, 21, true, 10.00, '2026-07-12 11:55:44.729308+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (58, 13, 10, 38, false, 0.00, '2026-07-12 11:55:44.766891+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (59, 14, 3, 9, true, 10.00, '2026-07-12 12:06:28.306385+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (60, 14, 5, 17, true, 10.00, '2026-07-12 12:06:28.349959+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (61, 14, 6, 21, true, 10.00, '2026-07-12 12:06:28.381725+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (62, 14, 10, 37, true, 10.00, '2026-07-12 12:06:28.426914+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (63, 15, 56, 226, true, 10.00, '2026-07-13 18:37:02.321561+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (64, 15, 58, 234, true, 10.00, '2026-07-13 18:37:13.235301+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (65, 15, 55, 221, true, 10.00, '2026-07-13 18:37:56.619373+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (66, 16, 73, 294, true, 5.00, '2026-07-17 12:06:38.50425+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (67, 16, 71, 287, false, 0.00, '2026-07-17 12:06:13.956468+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (68, 16, 66, 266, true, 5.00, '2026-07-17 12:07:10.849467+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (69, 16, 59, 238, true, 5.00, '2026-07-17 12:07:18.90835+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (70, 16, 60, 243, true, 5.00, '2026-07-17 12:07:27.280136+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (71, 16, 64, 259, true, 5.00, '2026-07-17 12:07:34.151181+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (72, 16, 76, 306, true, 5.00, '2026-07-17 12:07:43.930673+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (73, 16, 67, 270, false, 0.00, '2026-07-17 12:08:09.253371+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (74, 16, 78, 315, true, 5.00, '2026-07-17 12:08:25.942619+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (75, 16, 62, 250, true, 5.00, '2026-07-17 12:08:35.362905+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (76, 16, 61, 245, true, 5.00, '2026-07-17 12:08:47.634495+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (77, 16, 69, 278, true, 5.00, '2026-07-17 12:08:57.306753+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (78, 16, 75, 303, true, 5.00, '2026-07-17 12:09:09.249589+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (79, 16, 70, 282, true, 5.00, '2026-07-17 12:09:17.320342+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (86, 17, 183, 735, true, 10.00, '2026-07-19 20:37:09.516754+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (87, 17, 180, 721, false, 0.00, '2026-07-19 20:37:31.133759+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (88, 17, 181, 726, false, 0.00, '2026-07-19 20:37:56.451571+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (89, 18, 181, 727, true, 10.00, '2026-07-19 20:40:34.725011+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (90, 18, 183, 735, true, 10.00, '2026-07-19 20:40:51.849589+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (91, 18, 180, 722, true, 10.00, '2026-07-19 20:41:20.751081+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (92, 19, 180, 722, true, 10.00, '2026-07-19 20:42:46.133022+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (93, 19, 181, 727, true, 10.00, '2026-07-19 20:42:54.131774+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) VALUES (94, 19, 183, 735, true, 10.00, '2026-07-19 20:42:56.051579+05:30') ON CONFLICT DO NOTHING;






INSERT INTO public.subscription_plans (plan_id, name, tier, price_monthly, price_yearly, max_exams_lifetime, exam_cooldown_days, is_active) VALUES (1, 'Free Trial', 'Trial', 0.00, 0.00, 8, NULL, true) ON CONFLICT DO NOTHING;
INSERT INTO public.subscription_plans (plan_id, name, tier, price_monthly, price_yearly, max_exams_lifetime, exam_cooldown_days, is_active) VALUES (2, 'Professional', 'Professional', 1499.00, 14990.00, NULL, NULL, true) ON CONFLICT DO NOTHING;
INSERT INTO public.subscription_plans (plan_id, name, tier, price_monthly, price_yearly, max_exams_lifetime, exam_cooldown_days, is_active) VALUES (3, 'Free Forever', 'FreeForever', 0.00, 0.00, NULL, 3, true) ON CONFLICT DO NOTHING;



INSERT INTO public.examiner_subscriptions (subscription_id, user_id, plan_id, status, trial_exams_used, upgrade_decline_count, subscription_start_date, subscription_end_date, next_renewal_date, auto_renew, discount_applied_percent, last_free_exam_created_at, created_at, updated_at) VALUES (1, 18, 1, 'TrialActive', 0, 0, '2026-07-16 13:47:29.93169+05:30', NULL, NULL, false, NULL, NULL, '2026-07-16 13:47:29.93169+05:30', '2026-07-16 13:47:29.93169+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_subscriptions (subscription_id, user_id, plan_id, status, trial_exams_used, upgrade_decline_count, subscription_start_date, subscription_end_date, next_renewal_date, auto_renew, discount_applied_percent, last_free_exam_created_at, created_at, updated_at) VALUES (3, 2, 2, 'ProfessionalActive', 0, 0, '2026-07-16 14:09:36.804922+05:30', NULL, NULL, true, NULL, NULL, '2026-07-16 14:09:36.804922+05:30', '2026-07-17 11:35:41.852045+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_subscriptions (subscription_id, user_id, plan_id, status, trial_exams_used, upgrade_decline_count, subscription_start_date, subscription_end_date, next_renewal_date, auto_renew, discount_applied_percent, last_free_exam_created_at, created_at, updated_at) VALUES (2, 3, 2, 'ProfessionalActive', 0, 0, '2026-07-16 13:48:00.204144+05:30', NULL, NULL, true, NULL, NULL, '2026-07-16 13:48:00.204144+05:30', '2026-07-17 13:45:28.642539+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_subscriptions (subscription_id, user_id, plan_id, status, trial_exams_used, upgrade_decline_count, subscription_start_date, subscription_end_date, next_renewal_date, auto_renew, discount_applied_percent, last_free_exam_created_at, created_at, updated_at) VALUES (5, 28, 1, 'TrialActive', 1, 0, '2026-07-19 20:31:13.250333+05:30', NULL, NULL, false, NULL, NULL, '2026-07-19 20:31:13.250333+05:30', '2026-07-19 20:33:12.362121+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_subscriptions (subscription_id, user_id, plan_id, status, trial_exams_used, upgrade_decline_count, subscription_start_date, subscription_end_date, next_renewal_date, auto_renew, discount_applied_percent, last_free_exam_created_at, created_at, updated_at) VALUES (4, 7, 2, 'ProfessionalActive', 0, 0, '2026-07-16 14:15:55.22746+05:30', NULL, NULL, true, NULL, NULL, '2026-07-16 14:15:55.22746+05:30', '2026-07-20 09:16:46.398347+05:30') ON CONFLICT DO NOTHING;



INSERT INTO public.payments (payment_id, user_id, subscription_id, purpose, amount, currency, method, status, fake_transaction_id, fake_gateway_reference, idempotency_key, coupon_id, initiated_at, completed_at, failure_reason) VALUES (1, 5, NULL, 'ExamCertificationPurchase', 8000.00, 'INR', 'CreditCard', 'Success', 'TXN-20260717062352-q2SQtD', 'GTW-F78539FA', '3b97a897-f2ae-4584-934b-dd5e30cfcbdc', NULL, '2026-07-17 11:53:52.725954+05:30', '2026-07-17 11:53:52.725954+05:30', NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.payments (payment_id, user_id, subscription_id, purpose, amount, currency, method, status, fake_transaction_id, fake_gateway_reference, idempotency_key, coupon_id, initiated_at, completed_at, failure_reason) VALUES (2, 6, NULL, 'ExamCertificationPurchase', 10000.00, 'INR', 'CreditCard', 'Failed', 'TXN-20260717090632-YsfHxr', 'GTW-611CA064', '98ae1e6b-b421-40db-b9b8-2e5211a4aaca', NULL, '2026-07-17 14:36:32.406891+05:30', '2026-07-17 14:36:32.406891+05:30', 'Invalid card number.') ON CONFLICT DO NOTHING;



INSERT INTO public.candidate_purchases (purchase_id, user_id, exam_id, payment_id, status, purchased_at) VALUES (1, 5, 17, 1, 'Active', '2026-07-17 11:53:52.725954+05:30') ON CONFLICT DO NOTHING;



INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (2, 'CERT-2026-000002', 5, 7, 3, 70.00, '2026-06-16 21:55:17.353903+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (3, 'CERT-2026-000003', 5, 9, 5, 40.00, '2026-06-22 12:51:40.048312+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (4, 'CERT-2026-000004', 5, 11, 9, 30.00, '2026-07-10 15:01:27.778189+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (5, 'CERT-2026-000005', 5, 8, 7, 60.00, '2026-07-11 17:50:30.089367+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (6, 'CERT-2026-000006', 12, 8, 10, 60.00, '2026-07-11 17:50:30.219665+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (7, 'CERT-2026-000007', 6, 14, 14, 40.00, '2026-07-12 12:07:31.654744+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (8, 'CERT-2026-000008', 6, 16, 15, 30.00, '2026-07-14 14:43:39.239471+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (9, 'CERT-2026-000009', 6, 13, 13, 30.00, '2026-07-14 16:46:24.607488+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (10, 'CERT-2026-000010', 5, 17, 16, 90.00, '2026-07-17 12:23:15.108193+05:30', 'Issued', NULL, 'https://logovectorseek.com/harvard-business-school-logo-vector-svg/', 'Harvard Business School', true) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (11, 'CERT-2026-000011', 6, 32, 18, 30.00, '2026-07-19 20:44:45.319658+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;
INSERT INTO public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at, institute_logo_url, institute_name, is_premium) VALUES (12, 'CERT-2026-000012', 12, 32, 19, 30.00, '2026-07-19 20:44:45.34498+05:30', 'Issued', NULL, NULL, NULL, false) ON CONFLICT DO NOTHING;






INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (2, 1, 1, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (3, 1, 2, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (4, 1, 3, 3, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (5, 1, 4, 4, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (6, 1, 5, 5, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (7, 1, 6, 6, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (8, 1, 7, 7, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (9, 1, 8, 8, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (10, 1, 9, 9, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (11, 1, 10, 10, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (12, 7, 16, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (13, 7, 17, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (14, 7, 18, 3, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (15, 7, 19, 4, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (16, 7, 20, 5, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (17, 7, 36, 6, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (18, 7, 37, 7, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (19, 8, 38, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (20, 8, 39, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (21, 8, 40, 3, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (22, 8, 48, 4, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (23, 8, 49, 5, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (24, 8, 50, 6, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (25, 9, 1, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (26, 9, 2, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (27, 9, 3, 3, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (28, 9, 4, 4, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (29, 9, 5, 5, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (33, 11, 16, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (34, 11, 18, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (35, 11, 20, 3, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (36, 12, 53, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (37, 12, 6, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (38, 12, 3, 3, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (39, 12, 5, 4, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (40, 13, 3, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (41, 13, 5, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (42, 13, 6, 3, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (43, 13, 10, 4, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (44, 14, 3, 1, 'C# Syntax & Keywords') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (45, 14, 5, 2, 'Data Structures') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (46, 14, 6, 3, 'LINQ Operations') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (47, 14, 10, 4, 'Design Patterns') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (48, 15, 3, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (50, 15, 5, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (51, 16, 55, 1, 'Transaction Management') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (52, 16, 56, 2, 'Database Normalization') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (53, 16, 58, 3, 'Data Definition Language') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (54, 17, 59, 1, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (55, 17, 61, 2, 'C# Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (56, 17, 63, 3, 'Database & SQL') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (57, 17, 65, 4, 'C# Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (58, 17, 67, 5, 'Software Design Principles') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (59, 17, 69, 6, 'Database & SQL') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (60, 17, 71, 7, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (61, 17, 78, 8, 'Security & Authentication') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (62, 17, 74, 9, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (63, 17, 64, 10, 'C# Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (64, 17, 66, 11, 'C# Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (65, 17, 68, 12, 'Security & Authentication') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (66, 17, 70, 13, 'Security & Authentication') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (67, 17, 72, 14, 'Database & SQL') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (68, 17, 76, 15, 'Software Design Principles') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (69, 17, 73, 16, 'C# Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (70, 17, 75, 17, 'Database & SQL') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (71, 17, 77, 18, 'Database & SQL') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (72, 17, 60, 19, 'Database & SQL') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (73, 17, 62, 20, 'Security & Authentication') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (79, 19, 99, 1, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (80, 19, 100, 2, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (81, 19, 101, 3, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (82, 19, 103, 4, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (83, 19, 105, 5, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (84, 19, 107, 6, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (85, 19, 109, 7, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (86, 19, 111, 8, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (87, 19, 113, 9, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (88, 19, 115, 10, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (89, 19, 117, 11, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (90, 19, 118, 12, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (91, 19, 116, 13, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (92, 19, 112, 14, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (93, 19, 114, 15, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (94, 19, 110, 16, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (95, 19, 106, 17, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (96, 19, 108, 18, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (97, 19, 104, 19, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (98, 19, 102, 20, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (99, 20, 119, 1, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (100, 20, 120, 2, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (101, 20, 121, 3, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (102, 20, 122, 4, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (103, 20, 123, 5, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (104, 20, 124, 6, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (105, 20, 125, 7, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (106, 20, 126, 8, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (107, 20, 127, 9, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (108, 20, 128, 10, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (109, 21, 129, 1, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (110, 21, 130, 2, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (111, 21, 131, 3, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (112, 21, 132, 4, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (113, 21, 133, 5, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (114, 21, 134, 6, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (115, 21, 135, 7, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (116, 21, 136, 8, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (117, 21, 137, 9, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (118, 21, 138, 10, 'Logical Reasoning') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (119, 22, 139, 1, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (120, 22, 140, 2, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (121, 22, 141, 3, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (122, 22, 142, 4, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (123, 22, 143, 5, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (124, 22, 144, 6, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (125, 22, 145, 7, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (126, 22, 146, 8, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (127, 22, 147, 9, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (128, 22, 148, 10, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (129, 23, 149, 1, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (74, 18, 92, 1, 'SQL & Data Access') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (75, 18, 88, 2, 'Security & Authentication') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (76, 18, 90, 3, 'HTTP & Web Protocols') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (77, 18, 48, 4, 'HTTP & Web Protocols') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (78, 18, 86, 5, 'SQL & Data Access') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (130, 23, 150, 2, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (131, 23, 151, 3, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (132, 23, 152, 4, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (133, 23, 153, 5, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (134, 23, 154, 6, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (135, 23, 155, 7, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (136, 23, 156, 8, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (137, 23, 157, 9, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (138, 23, 158, 10, 'Data Structures & Algorithms') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (139, 24, 159, 1, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (140, 24, 160, 2, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (141, 24, 161, 3, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (142, 24, 162, 4, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (143, 24, 163, 5, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (144, 24, 164, 6, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (145, 24, 165, 7, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (146, 24, 166, 8, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (147, 24, 167, 9, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (148, 24, 168, 10, 'Cybersecurity Fundamentals') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (149, 25, 169, 1, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (150, 25, 170, 2, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (151, 25, 171, 3, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (152, 25, 172, 4, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (153, 25, 173, 5, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (154, 25, 174, 6, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (155, 25, 175, 7, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (156, 25, 176, 8, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (157, 25, 177, 9, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (158, 25, 178, 10, 'AI & Machine Learning Basics') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (159, 26, 119, 1, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (160, 26, 120, 2, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (161, 26, 121, 3, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (162, 26, 122, 4, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (163, 26, 123, 5, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (164, 26, 124, 6, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (165, 26, 125, 7, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (166, 26, 126, 8, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (167, 26, 127, 9, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (168, 26, 128, 10, 'Web APIs') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (169, 27, 21, 1, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (170, 27, 22, 2, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (171, 27, 23, 3, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (172, 27, 24, 4, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (173, 27, 25, 5, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (174, 27, 48, 6, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (175, 27, 49, 7, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (176, 27, 50, 8, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (177, 27, 51, 9, 'Web Development') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (178, 28, 11, 1, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (179, 28, 12, 2, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (180, 28, 13, 3, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (181, 28, 14, 4, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (182, 28, 15, 5, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (183, 28, 53, 6, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (184, 28, 55, 7, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (185, 28, 56, 8, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (186, 28, 57, 9, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (187, 28, 58, 10, 'Database Systems (DBMS)') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (188, 29, 38, 1, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (189, 29, 39, 2, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (190, 29, 40, 3, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (191, 29, 41, 4, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (192, 29, 42, 5, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (193, 29, 43, 6, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (194, 29, 44, 7, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (195, 29, 45, 8, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (196, 29, 46, 9, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (197, 29, 47, 10, 'Cloud & DevOps') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (198, 30, 16, 1, 'Software Engineering') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (199, 30, 17, 2, 'Software Engineering') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (200, 30, 18, 3, 'Software Engineering') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (201, 30, 19, 4, 'Software Engineering') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (202, 30, 20, 5, 'Software Engineering') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (203, 30, 36, 6, 'Software Engineering') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (204, 30, 37, 7, 'Software Engineering') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (205, 31, 139, 1, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (206, 31, 140, 2, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (207, 31, 141, 3, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (208, 31, 142, 4, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (209, 31, 143, 5, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (210, 31, 144, 6, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (211, 31, 145, 7, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (212, 31, 146, 8, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (213, 31, 147, 9, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (214, 31, 148, 10, 'Python Programming') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (215, 32, 183, 1, 'Machine Learning Concepts') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (216, 32, 181, 2, 'Neural Network Architecture') ON CONFLICT DO NOTHING;
INSERT INTO public.exam_questions (exam_question_id, exam_id, question_id, question_order, analytic_category) VALUES (218, 32, 180, 3, 'Model Training & Evaluation') ON CONFLICT DO NOTHING;



INSERT INTO public.examiner_requests (examiner_request_id, full_name, email, purpose, status, requested_at, processed_at, admin_note) VALUES (1, 'Dr. Ashok Gupta', 'profashok@gmail.com', 'I just want to bro. I have a PhD, just let me become one.', 'Approved', '2026-06-30 17:14:21.781642+05:30', '2026-06-30 17:21:29.775838+05:30', NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_requests (examiner_request_id, full_name, email, purpose, status, requested_at, processed_at, admin_note) VALUES (2, 'Dr Sai', 'sainikinsr@gmail.com', 'Test i want to be examiner', 'Rejected', '2026-06-30 17:22:37.139751+05:30', '2026-07-06 14:50:35.442331+05:30', '') ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_requests (examiner_request_id, full_name, email, purpose, status, requested_at, processed_at, admin_note) VALUES (3, 'Diya T', 'diya_stabtest@test.local', 'STAB TEST: requesting examiner access for testing.', 'Rejected', '2026-07-12 20:08:46.802928+05:30', '2026-07-14 16:56:17.669366+05:30', '') ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_requests (examiner_request_id, full_name, email, purpose, status, requested_at, processed_at, admin_note) VALUES (6, 'Dr. Nikitha', 'nikisai.003@gmail.com', 'I want to become an Examiner', 'Approved', '2026-07-16 10:36:50.252647+05:30', '2026-07-16 10:38:19.217171+05:30', NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_requests (examiner_request_id, full_name, email, purpose, status, requested_at, processed_at, admin_note) VALUES (7, 'QA Examiner', 'qa.examiner.req@example.test', 'I want to create exams for my computer science students at my institution.', 'Approved', '2026-07-17 08:48:57.323756+05:30', '2026-07-17 09:25:30.935337+05:30', NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_requests (examiner_request_id, full_name, email, purpose, status, requested_at, processed_at, admin_note) VALUES (9, 'Bhuvana', 'sainikinsr@gmail.com', 'ewiufhweiertekjhrtc3het34', 'Rejected', '2026-07-19 20:15:24.792695+05:30', '2026-07-19 20:18:48.947964+05:30', '') ON CONFLICT DO NOTHING;
INSERT INTO public.examiner_requests (examiner_request_id, full_name, email, purpose, status, requested_at, processed_at, admin_note) VALUES (10, 'Prof Raghav', 'raghavnks@gmail.com', 'I want to be an examiner.', 'Approved', '2026-07-19 20:18:23.804558+05:30', '2026-07-19 20:19:02.083895+05:30', NULL) ON CONFLICT DO NOTHING;



INSERT INTO public.institutions (institution_id, owner_user_id, name, logo_url, verification_status, requested_at, verified_at, verified_by_admin_user_id) VALUES (1, 2, 'Harvard Business School', 'https://logovectorseek.com/harvard-business-school-logo-vector-svg/', 'Verified', '2026-07-16 14:10:56.229633+05:30', '2026-07-16 14:11:28.819157+05:30', 1) ON CONFLICT DO NOTHING;
INSERT INTO public.institutions (institution_id, owner_user_id, name, logo_url, verification_status, requested_at, verified_at, verified_by_admin_user_id) VALUES (2, 7, 'IIM Mumbai', 'https://iimmumbai.ac.in/img/logo/logo.jpeg', 'Verified', '2026-07-16 14:17:20.933123+05:30', '2026-07-16 14:17:37.375531+05:30', 1) ON CONFLICT DO NOTHING;
INSERT INTO public.institutions (institution_id, owner_user_id, name, logo_url, verification_status, requested_at, verified_at, verified_by_admin_user_id) VALUES (3, 3, 'Indian Institute of Madras', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRxex8KD7vafVKOH_f8NaxE6fFW8zB4hzA-_LLKfNGO_w&s', 'Verified', '2026-07-17 13:41:52.521146+05:30', '2026-07-17 13:42:48.865797+05:30', 1) ON CONFLICT DO NOTHING;



INSERT INTO public.invoices (invoice_id, payment_id, invoice_number, fake_gst_number, billing_name, billing_email, subtotal, discount_amount, tax_amount, total, issued_at) VALUES (1, 1, 'INV-2026-000001', '29AAAAA0000EXM1Z5', 'Riya', 'sainikinsr@gmail.com', 8000.00, 0.00, 1440.00, 9440.00, '2026-07-17 11:53:52.725954+05:30') ON CONFLICT DO NOTHING;



INSERT INTO public.issue_reports (issue_report_id, candidate_id, exam_id, attempt_id, question_id, message, status, created_at, resolved_at, resolution_note) VALUES (1, 5, 12, 11, 3, 'test', 'Reviewed', '2026-07-11 21:00:41.089622+05:30', NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.issue_reports (issue_report_id, candidate_id, exam_id, attempt_id, question_id, message, status, created_at, resolved_at, resolution_note) VALUES (2, 6, 13, 13, 3, 'STAB TEST: question 3 wording unclear.', 'Resolved', '2026-07-12 20:08:16.976347+05:30', '2026-07-12 20:08:17.315393+05:30', 'Clarified.') ON CONFLICT DO NOTHING;
INSERT INTO public.issue_reports (issue_report_id, candidate_id, exam_id, attempt_id, question_id, message, status, created_at, resolved_at, resolution_note) VALUES (3, 5, 32, 17, 181, 'wrong options', 'Reviewed', '2026-07-19 20:38:25.45106+05:30', NULL, NULL) ON CONFLICT DO NOTHING;



INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (1, 5, 'ResultPublished', 'Results for ''Software Engineering Assessment'' are published. You passed with a score of 70.00.', 7, true, '2026-06-16 20:33:39.002466+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (2, 5, 'CertificateRevoked', 'Your certificate for ''Software Engineering Assessment'' has been revoked.', 7, true, '2026-06-16 21:43:53.4092+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (3, 5, 'CertificateIssued', 'Congratulations! Your certificate for ''Software Engineering Assessment'' is now available.', 7, true, '2026-06-16 21:55:17.490771+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (4, 5, 'ResultPublished', 'Results for ''Software Engineering Assessment'' are published. You passed with a score of 70.00.', 7, true, '2026-06-16 21:55:17.499432+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (5, 6, 'ResultPublished', 'Results for ''C# Certification Assessment'' are published. Your score was 10.00.', 1, true, '2026-06-17 19:45:46.446789+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (6, 5, 'CertificateIssued', 'Congratulations! Your certificate for ''C# Quick Check — Concurrent Test'' is now available.', 9, true, '2026-06-22 12:51:40.207587+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (7, 5, 'ResultPublished', 'Results for ''C# Quick Check — Concurrent Test'' are published. You passed with a score of 40.00.', 9, true, '2026-06-22 12:51:40.218325+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (8, 6, 'ResultPublished', 'Results for ''C# Quick Check — Concurrent Test'' are published. Your score was 10.00.', 9, true, '2026-06-22 12:51:40.218609+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (9, 1, 'ExaminerRequestReceived', 'New examiner request from Dr. Ashok Gupta (profashok@gmail.com): "I just want to bro. I have a PhD, just let me become one.…"', NULL, true, '2026-06-30 17:14:21.876176+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (10, 9, 'ExaminerAccountApproved', 'Welcome, Dr. Ashok Gupta! Your examiner account is ready. Please change your temporary password after your first login.', NULL, false, '2026-06-30 17:21:29.774613+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (11, 1, 'ExaminerRequestReceived', 'New examiner request from Dr Sai (sainikinsr@gmail.com): "Test i want to be examiner…"', NULL, true, '2026-06-30 17:22:37.145102+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (14, 7, 'AttemptResumed', 'Riya reconnected and resumed their in-progress attempt for ''Full Stack & DevOps Capstone Demo Exam'' (possible connection loss).', 8, true, '2026-07-03 17:20:59.653037+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (12, 7, 'AttemptResumed', 'Riya reconnected and resumed their in-progress attempt for ''Full Stack & DevOps Capstone Demo Exam'' (possible connection loss).', 8, true, '2026-07-03 17:19:48.317884+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (13, 7, 'AttemptResumed', 'Riya reconnected and resumed their in-progress attempt for ''Full Stack & DevOps Capstone Demo Exam'' (possible connection loss).', 8, true, '2026-07-03 17:20:10.040795+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (17, 8, 'ExamPublished', 'New exam available: ''General'' is now live (starts 2026-07-10 09:03:00Z, 10 min). Take it from Browse Exams if you wish!', 11, false, '2026-07-10 14:36:06.195446+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (16, 5, 'ExamPublished', 'New exam available: ''General'' is now live (starts 2026-07-10 09:03:00Z, 10 min). Take it from Browse Exams if you wish!', 11, true, '2026-07-10 14:36:06.19535+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (19, 5, 'CertificateIssued', 'Congratulations! Your certificate for ''General'' is now available.', 11, true, '2026-07-10 15:01:27.850502+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (20, 5, 'ResultPublished', 'Results for ''General'' are published. You passed with a score of 30.00.', 11, true, '2026-07-10 15:01:27.858463+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (21, 5, 'CertificateIssued', 'Congratulations! Your certificate for ''Full Stack & DevOps Capstone Demo Exam'' is now available.', 8, true, '2026-07-11 17:50:30.213455+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (22, 5, 'ResultPublished', 'Results for ''Full Stack & DevOps Capstone Demo Exam'' are published. You passed with a score of 60.00.', 8, true, '2026-07-11 17:50:30.218188+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (27, 8, 'ExamPublished', 'New exam available: ''General'' is now live (starts 2026-07-11 15:25:00Z, 35 min). Take it from Browse Exams if you wish!', 12, false, '2026-07-11 20:58:49.192089+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (26, 5, 'ExamPublished', 'New exam available: ''General'' is now live (starts 2026-07-11 15:25:00Z, 35 min). Take it from Browse Exams if you wish!', 12, true, '2026-07-11 20:58:49.191989+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (15, 6, 'ExamPublished', 'New exam available: ''General'' is now live (starts 2026-07-10 09:03:00Z, 10 min). Take it from Browse Exams if you wish!', 11, true, '2026-07-10 14:36:06.187022+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (25, 6, 'ExamPublished', 'New exam available: ''General'' is now live (starts 2026-07-11 15:25:00Z, 35 min). Take it from Browse Exams if you wish!', 12, true, '2026-07-11 20:58:49.175175+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (29, 2, 'IssueReported', 'Riya reported an issue about a specific question on ''General''. Open the Issue Reports panel to review it.', 12, true, '2026-07-11 21:00:41.124075+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (31, 6, 'ResultPublished', 'Results for ''General'' are published. Your score was 0.00.', 12, true, '2026-07-12 10:57:25.815696+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (34, 8, 'ExamPublished', 'New exam available: ''STAB TEST Exam (auto) 1783837456'' is now live (starts 2026-07-12 06:23:16Z, 30 min). Take it from Browse Exams if you wish!', 13, false, '2026-07-12 11:54:16.743513+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (39, 8, 'ExamPublished', 'New exam available: ''STAB TEST Exam2 (auto) 1783838167'' is now live (starts 2026-07-12 06:35:07Z, 30 min). Take it from Browse Exams if you wish!', 14, false, '2026-07-12 12:06:08.495074+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (32, 6, 'ExamPublished', 'New exam available: ''STAB TEST Exam (auto) 1783837456'' is now live (starts 2026-07-12 06:23:16Z, 30 min). Take it from Browse Exams if you wish!', 13, true, '2026-07-12 11:54:16.735965+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (37, 6, 'ExamPublished', 'New exam available: ''STAB TEST Exam2 (auto) 1783838167'' is now live (starts 2026-07-12 06:35:07Z, 30 min). Take it from Browse Exams if you wish!', 14, true, '2026-07-12 12:06:08.486071+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (18, 12, 'ExamPublished', 'New exam available: ''General'' is now live (starts 2026-07-10 09:03:00Z, 10 min). Take it from Browse Exams if you wish!', 11, true, '2026-07-10 14:36:06.195475+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (23, 12, 'CertificateIssued', 'Congratulations! Your certificate for ''Full Stack & DevOps Capstone Demo Exam'' is now available.', 8, true, '2026-07-11 17:50:30.241444+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (24, 12, 'ResultPublished', 'Results for ''Full Stack & DevOps Capstone Demo Exam'' are published. You passed with a score of 60.00.', 8, true, '2026-07-11 17:50:30.24161+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (28, 12, 'ExamPublished', 'New exam available: ''General'' is now live (starts 2026-07-11 15:25:00Z, 35 min). Take it from Browse Exams if you wish!', 12, true, '2026-07-11 20:58:49.192115+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (35, 12, 'ExamPublished', 'New exam available: ''STAB TEST Exam (auto) 1783837456'' is now live (starts 2026-07-12 06:23:16Z, 30 min). Take it from Browse Exams if you wish!', 13, true, '2026-07-12 11:54:16.74354+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (30, 5, 'ResultPublished', 'Results for ''General'' are published. Your score was 5.00.', 12, true, '2026-07-12 10:57:25.811079+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (33, 5, 'ExamPublished', 'New exam available: ''STAB TEST Exam (auto) 1783837456'' is now live (starts 2026-07-12 06:23:16Z, 30 min). Take it from Browse Exams if you wish!', 13, true, '2026-07-12 11:54:16.743454+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (38, 5, 'ExamPublished', 'New exam available: ''STAB TEST Exam2 (auto) 1783838167'' is now live (starts 2026-07-12 06:35:07Z, 30 min). Take it from Browse Exams if you wish!', 14, true, '2026-07-12 12:06:08.494991+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (46, 8, 'ExamPublished', 'New exam available: ''STAB Draft 1783866943'' is now live (starts 2026-07-12 14:40:42Z, 30 min). Take it from Browse Exams if you wish!', 15, false, '2026-07-12 20:05:43.543789+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (44, 6, 'ExamPublished', 'New exam available: ''STAB Draft 1783866943'' is now live (starts 2026-07-12 14:40:42Z, 30 min). Take it from Browse Exams if you wish!', 15, true, '2026-07-12 20:05:43.543621+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (42, 6, 'CertificateIssued', 'Congratulations! Your certificate for ''STAB TEST Exam2 (auto) 1783838167'' is now available.', 14, true, '2026-07-12 12:07:31.681879+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (43, 6, 'ResultPublished', 'Results for ''STAB TEST Exam2 (auto) 1783838167'' are published. You passed with a score of 40.00.', 14, true, '2026-07-12 12:07:31.682299+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (55, 8, 'ExamPublished', 'New exam available: ''Database Fundamentals'' is now live (starts 2026-07-13 12:50:00Z, 20 min). Take it from Browse Exams if you wish!', 16, false, '2026-07-13 18:35:27.715525+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (50, 6, 'IssueResolved', 'The issue you reported on ''STAB TEST Exam (auto) 1783837456'' has been resolved: Clarified.', 13, true, '2026-07-12 20:08:17.316906+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (53, 6, 'ExamPublished', 'New exam available: ''Database Fundamentals'' is now live (starts 2026-07-13 12:50:00Z, 20 min). Take it from Browse Exams if you wish!', 16, true, '2026-07-13 18:35:27.700911+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (51, 1, 'ExaminerRequestReceived', 'New examiner request from Diya T (diya_stabtest@test.local): "STAB TEST: requesting examiner access for testing.…"', NULL, true, '2026-07-12 20:08:46.816347+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (52, 1, 'ExaminerRequestReceived', 'New examiner request from Anon (anon_stab@test.local): "STAB anon test purpose here.…"', NULL, true, '2026-07-12 20:08:46.870945+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (40, 12, 'ExamPublished', 'New exam available: ''STAB TEST Exam2 (auto) 1783838167'' is now live (starts 2026-07-12 06:35:07Z, 30 min). Take it from Browse Exams if you wish!', 14, true, '2026-07-12 12:06:08.495104+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (47, 12, 'ExamPublished', 'New exam available: ''STAB Draft 1783866943'' is now live (starts 2026-07-12 14:40:42Z, 30 min). Take it from Browse Exams if you wish!', 15, true, '2026-07-12 20:05:43.543799+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (56, 12, 'ExamPublished', 'New exam available: ''Database Fundamentals'' is now live (starts 2026-07-13 12:50:00Z, 20 min). Take it from Browse Exams if you wish!', 16, true, '2026-07-13 18:35:27.715562+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (59, 6, 'ResultPublished', 'Results for ''Database Fundamentals'' are published. You passed with a score of 30.00.', 16, true, '2026-07-14 14:43:39.370344+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (58, 6, 'CertificateIssued', 'Congratulations! Your certificate for ''Database Fundamentals'' is now available.', 16, true, '2026-07-14 14:43:39.357238+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (49, 2, 'IssueReported', 'Diya reported an issue about a specific question on ''STAB TEST Exam (auto) 1783837456''. Open the Issue Reports panel to review it.', 13, true, '2026-07-12 20:08:17.032103+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (63, 1, 'ExaminerRequestReceived', 'New examiner request from Prof Bhuvana (chembubhuvana77@gmail.com): "I have a degree in B Ed.…"', NULL, true, '2026-07-14 16:55:16.021392+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (65, 1, 'ExaminerRequestReceived', 'New examiner request from Dr. Nikitha (nikisai.003@gmail.com): "I want to become an Examiner…"', NULL, true, '2026-07-16 10:36:50.336635+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (66, 18, 'ExaminerAccountApproved', 'Welcome, Dr. Nikitha! Your examiner account is ready. Please change your temporary password after your first login.', NULL, true, '2026-07-16 10:38:19.216863+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (67, 2, 'InstitutionVerified', 'Your institute ''Harvard Business School'' has been verified. The Institute Verified badge now appears on your premium exams.', NULL, true, '2026-07-16 14:11:28.828181+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (45, 5, 'ExamPublished', 'New exam available: ''STAB Draft 1783866943'' is now live (starts 2026-07-12 14:40:42Z, 30 min). Take it from Browse Exams if you wish!', 15, true, '2026-07-12 20:05:43.54377+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (54, 5, 'ExamPublished', 'New exam available: ''Database Fundamentals'' is now live (starts 2026-07-13 12:50:00Z, 20 min). Take it from Browse Exams if you wish!', 16, true, '2026-07-13 18:35:27.715411+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (70, 24, 'ExaminerAccountApproved', 'Welcome, QA Examiner! Your examiner account is ready. Please change your temporary password after your first login.', NULL, false, '2026-07-17 09:25:30.935209+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (68, 7, 'InstitutionVerified', 'Your institute ''IIM Mumbai'' has been verified. The Institute Verified badge now appears on your premium exams.', NULL, true, '2026-07-16 14:17:37.380662+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (69, 1, 'ExaminerRequestReceived', 'New examiner request from QA Examiner (qa.examiner.req@example.test): "I want to create exams for my computer science students at my institution.…"', NULL, true, '2026-07-17 08:48:57.335629+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (71, 21, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, false, '2026-07-17 11:44:40.209517+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (72, 22, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, false, '2026-07-17 11:44:40.212647+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (73, 23, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, false, '2026-07-17 11:44:40.212726+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (76, 8, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, false, '2026-07-17 11:44:40.212777+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (77, 12, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, false, '2026-07-17 11:44:40.212788+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (78, 19, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, false, '2026-07-17 11:44:40.212798+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (79, 20, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, false, '2026-07-17 11:44:40.212822+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (75, 5, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, true, '2026-07-17 11:44:40.212765+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (80, 5, 'CertificateIssued', 'Congratulations! Your certificate for ''Professional Computer Science Certification (Foundation Level)'' is now available.', 17, true, '2026-07-17 12:23:15.144579+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (81, 5, 'ResultPublished', 'Results for ''Professional Computer Science Certification (Foundation Level)'' are published. You passed with a score of 90.00.', 17, true, '2026-07-17 12:23:15.145108+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (82, 21, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, false, '2026-07-17 13:39:07.779373+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (83, 22, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, false, '2026-07-17 13:39:07.780194+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (84, 23, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, false, '2026-07-17 13:39:07.780238+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (87, 8, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, false, '2026-07-17 13:39:07.780273+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (88, 12, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, false, '2026-07-17 13:39:07.780289+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (89, 19, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, false, '2026-07-17 13:39:07.780294+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (90, 20, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, false, '2026-07-17 13:39:07.7803+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (85, 6, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, true, '2026-07-17 13:39:07.780245+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (91, 3, 'InstitutionVerified', 'Your institute ''Indian Institute of Madras'' has been verified. The Institute Verified badge now appears on your premium exams.', NULL, true, '2026-07-17 13:42:48.869403+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (92, 21, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, false, '2026-07-17 14:34:47.784962+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (93, 22, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, false, '2026-07-17 14:34:47.797714+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (94, 23, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, false, '2026-07-17 14:34:47.797812+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (97, 8, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, false, '2026-07-17 14:34:47.797865+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (98, 12, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, false, '2026-07-17 14:34:47.797879+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (99, 19, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, false, '2026-07-17 14:34:47.797896+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (100, 20, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, false, '2026-07-17 14:34:47.797912+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (60, 6, 'CertificateIssued', 'Congratulations! Your certificate for ''STAB TEST Exam (auto) 1783837456'' is now available.', 13, true, '2026-07-14 16:46:24.619874+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (61, 6, 'ResultPublished', 'Results for ''STAB TEST Exam (auto) 1783837456'' are published. You passed with a score of 30.00.', 13, true, '2026-07-14 16:46:24.620124+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (74, 6, 'ExamPublished', 'New exam available: ''Professional Computer Science Certification (Foundation Level)'' is now live (starts 2026-07-17 06:05:00Z, 90 min). Take it from Browse Exams if you wish!', 17, true, '2026-07-17 11:44:40.212749+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (95, 6, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, true, '2026-07-17 14:34:47.797831+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (86, 5, 'ExamPublished', 'New exam available: ''General exam - Web API'' is now live (starts 2026-07-17 07:17:00Z, 30 min). Take it from Browse Exams if you wish!', 18, true, '2026-07-17 13:39:07.780253+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (96, 5, 'ExamPublished', 'New exam available: ''Advanced Mathematics Aptitude Assessment'' is now live (starts 2026-07-17 08:14:00Z, 45 min). Take it from Browse Exams if you wish!', 19, true, '2026-07-17 14:34:47.797848+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (101, 21, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.021845+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (102, 22, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.043498+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (103, 23, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.043818+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (104, 6, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.043835+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (105, 5, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.043854+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (106, 8, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.043869+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (107, 12, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.043879+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (108, 19, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.043888+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (109, 20, 'ExamPublished', 'New exam available: ''SQL Practice Test'' is now live (starts 2026-07-18 13:49:08Z, 30 min). Take it from Browse Exams if you wish!', 28, false, '2026-07-19 19:43:21.043897+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (110, 21, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.74978+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (111, 22, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.749929+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (112, 23, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.749948+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (113, 6, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.74997+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (114, 5, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.749986+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (115, 8, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.749998+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (116, 12, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.750014+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (117, 19, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.750027+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (118, 20, 'ExamPublished', 'New exam available: ''Cloud Concepts Quiz'' is now live (starts 2026-07-18 13:49:08Z, 25 min). Take it from Browse Exams if you wish!', 29, false, '2026-07-19 19:43:34.750052+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (119, 1, 'ExaminerRequestReceived', 'New examiner request from Prof Raghavan (raghavnks@gmail.com): "I want to create exams and use this platform in place of BIRLA of Tech.…"', NULL, true, '2026-07-19 20:01:28.023391+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (121, 1, 'ExaminerRequestReceived', 'New examiner request from Bhuvana (sainikinsr@gmail.com): "ewiufhweiertekjhrtc3het34…"', NULL, false, '2026-07-19 20:15:24.810892+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (122, 1, 'ExaminerRequestReceived', 'New examiner request from Prof Raghav (raghavnks@gmail.com): "I want to be an examiner.…"', NULL, false, '2026-07-19 20:18:23.807451+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (123, 28, 'ExaminerAccountApproved', 'Welcome, Prof Raghav! Your examiner account is ready. Please change your temporary password after your first login.', NULL, true, '2026-07-19 20:19:02.083792+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (124, 21, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.397122+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (125, 22, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.398353+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (126, 23, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.398388+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (127, 8, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.398405+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (128, 6, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.39842+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (129, 5, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.398441+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (130, 12, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.398449+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (131, 19, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.398457+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (132, 20, 'ExamPublished', 'New exam available: ''AI fundamentals'' is now live (starts 2026-07-19 15:03:00Z, 20 min). Take it from Browse Exams if you wish!', 32, false, '2026-07-19 20:35:11.398466+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (133, 28, 'IssueReported', 'Riya reported an issue about a specific question on ''AI fundamentals''. Open the Issue Reports panel to review it.', 32, false, '2026-07-19 20:38:25.476774+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (134, 28, 'IntegrityFlag', 'candidate_phase1 left the exam tab 3 times while taking ''AI fundamentals''. Review their attempt for possible malpractice.', 32, false, '2026-07-19 20:42:09.211352+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (135, 6, 'CertificateIssued', 'Congratulations! Your certificate for ''AI fundamentals'' is now available.', 32, false, '2026-07-19 20:44:45.343625+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (136, 6, 'ResultPublished', 'Results for ''AI fundamentals'' are published. You passed with a score of 30.00.', 32, false, '2026-07-19 20:44:45.344017+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (137, 5, 'ResultPublished', 'Results for ''AI fundamentals'' are published. Your score was 10.00.', 32, false, '2026-07-19 20:44:45.344073+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (138, 12, 'CertificateIssued', 'Congratulations! Your certificate for ''AI fundamentals'' is now available.', 32, false, '2026-07-19 20:44:45.353764+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (139, 12, 'ResultPublished', 'Results for ''AI fundamentals'' are published. You passed with a score of 30.00.', 32, false, '2026-07-19 20:44:45.353819+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (140, 7, 'AttemptResumed', 'Diya reconnected and resumed their in-progress attempt for ''General exam - Web API'' (possible connection loss).', 18, false, '2026-07-20 09:10:27.297067+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (141, 7, 'AttemptResumed', 'Diya reconnected and resumed their in-progress attempt for ''General exam - Web API'' (possible connection loss).', 18, false, '2026-07-20 09:10:27.3555+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (142, 7, 'AttemptResumed', 'Diya reconnected and resumed their in-progress attempt for ''General exam - Web API'' (possible connection loss).', 18, false, '2026-07-20 09:10:29.088971+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (143, 7, 'AttemptResumed', 'Diya reconnected and resumed their in-progress attempt for ''General exam - Web API'' (possible connection loss).', 18, false, '2026-07-20 09:10:29.28311+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (144, 7, 'AttemptResumed', 'Diya reconnected and resumed their in-progress attempt for ''General exam - Web API'' (possible connection loss).', 18, false, '2026-07-20 09:16:47.254147+05:30') ON CONFLICT DO NOTHING;
INSERT INTO public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) VALUES (145, 7, 'AttemptResumed', 'Diya reconnected and resumed their in-progress attempt for ''General exam - Web API'' (possible connection loss).', 18, false, '2026-07-20 09:16:47.254139+05:30') ON CONFLICT DO NOTHING;



SELECT pg_catalog.setval('public.attempt_answers_answer_id_seq', 95, true);



SELECT pg_catalog.setval('public.attempts_attempt_id_seq', 27, true);



SELECT pg_catalog.setval('public.candidate_purchases_purchase_id_seq', 1, true);



SELECT pg_catalog.setval('public.certificates_certificate_id_seq', 12, true);



SELECT pg_catalog.setval('public.coupon_redemptions_redemption_id_seq', 1, false);



SELECT pg_catalog.setval('public.coupons_coupon_id_seq', 1, false);



SELECT pg_catalog.setval('public.exam_categories_cat_id_seq', 14, true);



SELECT pg_catalog.setval('public.exam_questions_exam_question_id_seq', 218, true);



SELECT pg_catalog.setval('public.examiner_requests_examiner_request_id_seq', 10, true);



SELECT pg_catalog.setval('public.examiner_subscriptions_subscription_id_seq', 5, true);



SELECT pg_catalog.setval('public.exams_exam_id_seq', 36, true);



SELECT pg_catalog.setval('public.institutions_institution_id_seq', 3, true);



SELECT pg_catalog.setval('public.invoices_invoice_id_seq', 1, true);



SELECT pg_catalog.setval('public.issue_reports_issue_report_id_seq', 3, true);



SELECT pg_catalog.setval('public.notifications_notification_id_seq', 145, true);



SELECT pg_catalog.setval('public.payments_payment_id_seq', 2, true);



SELECT pg_catalog.setval('public.question_options_option_id_seq', 740, true);



SELECT pg_catalog.setval('public.questions_question_id_seq', 184, true);



SELECT pg_catalog.setval('public.subscription_plans_plan_id_seq', 3, true);



SELECT pg_catalog.setval('public.users_user_id_seq', 34, true);

COMMIT;
