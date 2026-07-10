--
-- PostgreSQL database dump
--

\restrict f1VSo2Vhz7nzrA06559C5NeFwCoD8Zwl7x5r9m473Nqyv7wx4a7CXYfndkxZqOp

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3 (Homebrew)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

ALTER TABLE IF EXISTS ONLY public.questions DROP CONSTRAINT IF EXISTS fk_questions_exam_categories_cat_id;
ALTER TABLE IF EXISTS ONLY public.questions DROP CONSTRAINT IF EXISTS fk_questions__users_examiner_id;
ALTER TABLE IF EXISTS ONLY public.question_options DROP CONSTRAINT IF EXISTS fk_question_options_questions_question_id;
ALTER TABLE IF EXISTS ONLY public.notifications DROP CONSTRAINT IF EXISTS fk_notifications_exams_related_exam_id;
ALTER TABLE IF EXISTS ONLY public.notifications DROP CONSTRAINT IF EXISTS fk_notifications__users_user_id;
ALTER TABLE IF EXISTS ONLY public.exams DROP CONSTRAINT IF EXISTS fk_exams__users_examiner_id;
ALTER TABLE IF EXISTS ONLY public.exams DROP CONSTRAINT IF EXISTS fk_exams__exam_categories_cat_id;
ALTER TABLE IF EXISTS ONLY public.exam_questions DROP CONSTRAINT IF EXISTS fk_exam_questions_exams_exam_id;
ALTER TABLE IF EXISTS ONLY public.exam_questions DROP CONSTRAINT IF EXISTS fk_exam_questions__questions_question_id;
ALTER TABLE IF EXISTS ONLY public.certificates DROP CONSTRAINT IF EXISTS fk_certificates_attempts_attempt_id;
ALTER TABLE IF EXISTS ONLY public.certificates DROP CONSTRAINT IF EXISTS fk_certificates__users_user_id;
ALTER TABLE IF EXISTS ONLY public.certificates DROP CONSTRAINT IF EXISTS fk_certificates__exams_exam_id;
ALTER TABLE IF EXISTS ONLY public.attempts DROP CONSTRAINT IF EXISTS fk_attempts__users_user_id;
ALTER TABLE IF EXISTS ONLY public.attempts DROP CONSTRAINT IF EXISTS fk_attempts__exams_exam_id;
ALTER TABLE IF EXISTS ONLY public.attempt_answers DROP CONSTRAINT IF EXISTS fk_attempt_answers_attempts_attempt_id;
ALTER TABLE IF EXISTS ONLY public.attempt_answers DROP CONSTRAINT IF EXISTS fk_attempt_answers__questions_question_id;
ALTER TABLE IF EXISTS ONLY public.attempt_answers DROP CONSTRAINT IF EXISTS fk_attempt_answers__question_options_selected_option_id;
DROP INDEX IF EXISTS public.uix_attempts_user_exam_inprogress;
DROP INDEX IF EXISTS public.ix_users_username;
DROP INDEX IF EXISTS public.ix_users_email;
DROP INDEX IF EXISTS public.ix_questions_examiner_id;
DROP INDEX IF EXISTS public.ix_questions_cat_id;
DROP INDEX IF EXISTS public.ix_question_options_question_id;
DROP INDEX IF EXISTS public.ix_notifications_user_id;
DROP INDEX IF EXISTS public.ix_notifications_related_exam_id;
DROP INDEX IF EXISTS public.ix_exams_examiner_id;
DROP INDEX IF EXISTS public.ix_exams_cat_id;
DROP INDEX IF EXISTS public.ix_examiner_requests_email;
DROP INDEX IF EXISTS public.ix_exam_questions_question_id;
DROP INDEX IF EXISTS public.ix_exam_questions_exam_id_question_id;
DROP INDEX IF EXISTS public.ix_exam_categories_cat_name;
DROP INDEX IF EXISTS public.ix_certificates_user_id_exam_id;
DROP INDEX IF EXISTS public.ix_certificates_exam_id;
DROP INDEX IF EXISTS public.ix_certificates_certificate_code;
DROP INDEX IF EXISTS public.ix_certificates_attempt_id;
DROP INDEX IF EXISTS public.ix_attempts_exam_id;
DROP INDEX IF EXISTS public.ix_attempt_answers_selected_option_id;
DROP INDEX IF EXISTS public.ix_attempt_answers_question_id;
DROP INDEX IF EXISTS public.ix_attempt_answers_attempt_id_question_id;
ALTER TABLE IF EXISTS ONLY public.users DROP CONSTRAINT IF EXISTS pk_users;
ALTER TABLE IF EXISTS ONLY public.questions DROP CONSTRAINT IF EXISTS pk_questions;
ALTER TABLE IF EXISTS ONLY public.question_options DROP CONSTRAINT IF EXISTS pk_question_options;
ALTER TABLE IF EXISTS ONLY public.notifications DROP CONSTRAINT IF EXISTS pk_notifications;
ALTER TABLE IF EXISTS ONLY public.exams DROP CONSTRAINT IF EXISTS pk_exams;
ALTER TABLE IF EXISTS ONLY public.examiner_requests DROP CONSTRAINT IF EXISTS pk_examiner_requests;
ALTER TABLE IF EXISTS ONLY public.exam_questions DROP CONSTRAINT IF EXISTS pk_exam_questions;
ALTER TABLE IF EXISTS ONLY public.exam_categories DROP CONSTRAINT IF EXISTS pk_exam_categories;
ALTER TABLE IF EXISTS ONLY public.certificates DROP CONSTRAINT IF EXISTS pk_certificates;
ALTER TABLE IF EXISTS ONLY public.attempts DROP CONSTRAINT IF EXISTS pk_attempts;
ALTER TABLE IF EXISTS ONLY public.attempt_answers DROP CONSTRAINT IF EXISTS pk_attempt_answers;
ALTER TABLE IF EXISTS ONLY public."__EFMigrationsHistory" DROP CONSTRAINT IF EXISTS "PK___EFMigrationsHistory";
DROP TABLE IF EXISTS public.users;
DROP TABLE IF EXISTS public.questions;
DROP TABLE IF EXISTS public.question_options;
DROP TABLE IF EXISTS public.notifications;
DROP TABLE IF EXISTS public.exams;
DROP TABLE IF EXISTS public.examiner_requests;
DROP TABLE IF EXISTS public.exam_questions;
DROP TABLE IF EXISTS public.exam_categories;
DROP TABLE IF EXISTS public.certificates;
DROP TABLE IF EXISTS public.attempts;
DROP TABLE IF EXISTS public.attempt_answers;
DROP TABLE IF EXISTS public."__EFMigrationsHistory";
SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: attempt_answers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.attempt_answers (
    answer_id integer NOT NULL,
    attempt_id integer NOT NULL,
    question_id integer NOT NULL,
    selected_option_id integer,
    is_correct boolean,
    marks_awarded numeric(5,2),
    answered_at timestamp with time zone NOT NULL
);


--
-- Name: attempt_answers_answer_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.attempt_answers ALTER COLUMN answer_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.attempt_answers_answer_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: attempts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.attempts (
    attempt_id integer NOT NULL,
    user_id integer NOT NULL,
    exam_id integer NOT NULL,
    started_at timestamp with time zone NOT NULL,
    submitted_at timestamp with time zone,
    attempt_status character varying(20) NOT NULL,
    score numeric(6,2),
    is_passed boolean,
    time_taken_seconds integer
);


--
-- Name: attempts_attempt_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.attempts ALTER COLUMN attempt_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.attempts_attempt_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: certificates; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.certificates (
    certificate_id integer NOT NULL,
    certificate_code character varying(30) NOT NULL,
    user_id integer NOT NULL,
    exam_id integer NOT NULL,
    attempt_id integer NOT NULL,
    score numeric(6,2) NOT NULL,
    issued_at timestamp with time zone NOT NULL,
    cert_status character varying(20) NOT NULL,
    revoked_at timestamp with time zone
);


--
-- Name: certificates_certificate_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.certificates ALTER COLUMN certificate_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.certificates_certificate_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: exam_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.exam_categories (
    cat_id integer NOT NULL,
    cat_name character varying(100) NOT NULL,
    description character varying(500)
);


--
-- Name: exam_categories_cat_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.exam_categories ALTER COLUMN cat_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.exam_categories_cat_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: exam_questions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.exam_questions (
    exam_question_id integer NOT NULL,
    exam_id integer NOT NULL,
    question_id integer NOT NULL,
    question_order integer NOT NULL
);


--
-- Name: exam_questions_exam_question_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.exam_questions ALTER COLUMN exam_question_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.exam_questions_exam_question_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: examiner_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.examiner_requests (
    examiner_request_id integer NOT NULL,
    full_name character varying(100) NOT NULL,
    email character varying(150) NOT NULL,
    purpose character varying(1000) NOT NULL,
    status character varying(20) DEFAULT 'Pending'::character varying NOT NULL,
    requested_at timestamp with time zone NOT NULL,
    processed_at timestamp with time zone,
    admin_note character varying(500)
);


--
-- Name: examiner_requests_examiner_request_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.examiner_requests ALTER COLUMN examiner_request_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.examiner_requests_examiner_request_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: exams; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.exams (
    exam_id integer NOT NULL,
    examiner_id integer NOT NULL,
    cat_id integer NOT NULL,
    title character varying(200) NOT NULL,
    instructions character varying(2000),
    duration_minutes integer NOT NULL,
    total_marks numeric(6,2) NOT NULL,
    passing_marks numeric(6,2) NOT NULL,
    start_time timestamp with time zone NOT NULL,
    end_time timestamp with time zone NOT NULL,
    exam_status character varying(30) NOT NULL,
    created_at timestamp with time zone NOT NULL
);


--
-- Name: exams_exam_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.exams ALTER COLUMN exam_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.exams_exam_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: notifications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notifications (
    notification_id integer NOT NULL,
    user_id integer NOT NULL,
    notif_type character varying(30) NOT NULL,
    message character varying(500) NOT NULL,
    related_exam_id integer,
    is_read boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone NOT NULL
);


--
-- Name: notifications_notification_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.notifications ALTER COLUMN notification_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.notifications_notification_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: question_options; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.question_options (
    option_id integer NOT NULL,
    question_id integer NOT NULL,
    option_text character varying(500) NOT NULL,
    is_correct boolean NOT NULL
);


--
-- Name: question_options_option_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.question_options ALTER COLUMN option_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.question_options_option_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: questions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.questions (
    question_id integer NOT NULL,
    examiner_id integer NOT NULL,
    cat_id integer NOT NULL,
    question_text character varying(1000) NOT NULL,
    marks numeric(5,2) NOT NULL,
    difficulty character varying(10) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: questions_question_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.questions ALTER COLUMN question_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.questions_question_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    user_id integer NOT NULL,
    username character varying(50) NOT NULL,
    email character varying(150) NOT NULL,
    password_hash character varying(255) NOT NULL,
    full_name character varying(100) NOT NULL,
    role character varying(20) NOT NULL,
    date_of_joining timestamp with time zone NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: users_user_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.users ALTER COLUMN user_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.users_user_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20260604094717_InitialCreate	10.0.8
20260605052538_RemovePasswordSalt	10.0.8
20260616100138_AddAttemptInProgressUniqueIndex	10.0.8
20260629114527_AddExaminerRequest	10.0.8
\.


--
-- Data for Name: attempt_answers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.attempt_answers (answer_id, attempt_id, question_id, selected_option_id, is_correct, marks_awarded, answered_at) FROM stdin;
2	3	16	61	t	10.00	2026-06-16 20:08:25.20829+05:30
3	3	17	65	t	10.00	2026-06-16 20:08:54.782025+05:30
4	3	18	69	t	10.00	2026-06-16 20:09:54.765512+05:30
5	3	19	73	t	10.00	2026-06-16 20:10:22.465843+05:30
6	3	20	77	t	10.00	2026-06-16 20:10:28.5615+05:30
7	3	36	141	t	10.00	2026-06-16 20:10:40.653567+05:30
8	3	37	145	t	10.00	2026-06-16 20:10:49.743538+05:30
9	4	1	2	f	0.00	2026-06-17 19:30:27.166197+05:30
10	4	2	7	f	0.00	2026-06-17 19:30:51.554897+05:30
11	4	3	9	t	10.00	2026-06-17 19:31:22.28692+05:30
12	4	4	14	f	0.00	2026-06-17 19:31:34.348857+05:30
13	4	5	19	f	0.00	2026-06-17 19:32:15.292064+05:30
14	4	6	22	f	0.00	2026-06-17 19:33:12.784158+05:30
15	4	7	28	f	0.00	2026-06-17 19:33:24.127864+05:30
16	4	8	30	f	0.00	2026-06-17 19:33:48.312331+05:30
17	4	9	35	f	0.00	2026-06-17 19:33:52.563381+05:30
18	4	10	40	f	0.00	2026-06-17 19:33:57.413017+05:30
24	6	1	2	f	0.00	2026-06-22 11:51:35.609267+05:30
25	6	2	5	t	10.00	2026-06-22 11:51:45.431147+05:30
19	5	1	1	t	10.00	2026-06-22 11:47:56.21892+05:30
20	5	2	5	t	10.00	2026-06-22 11:48:02.983299+05:30
21	5	3	9	t	10.00	2026-06-22 11:48:12.506266+05:30
22	5	4	14	f	0.00	2026-06-22 11:48:18.734856+05:30
23	5	5	17	t	10.00	2026-06-22 11:48:27.835107+05:30
26	7	38	149	t	10.00	2026-07-03 17:19:33.927629+05:30
27	7	39	153	t	10.00	2026-07-03 17:19:38.352215+05:30
28	7	40	157	t	10.00	2026-07-03 17:19:44.095924+05:30
29	7	50	197	t	10.00	2026-07-03 17:21:12.375604+05:30
30	7	49	193	t	10.00	2026-07-03 17:21:15.336208+05:30
31	7	48	189	t	10.00	2026-07-03 17:21:19.809791+05:30
32	8	38	149	t	10.00	2026-07-08 10:53:38.043137+05:30
33	8	39	153	t	10.00	2026-07-08 10:53:40.54862+05:30
34	8	40	158	f	0.00	2026-07-08 10:53:42.261838+05:30
35	8	48	191	f	0.00	2026-07-08 10:53:43.104697+05:30
36	8	49	193	t	10.00	2026-07-08 10:53:44.624052+05:30
37	8	50	197	t	10.00	2026-07-08 10:53:49.223421+05:30
\.


--
-- Data for Name: attempts; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.attempts (attempt_id, user_id, exam_id, started_at, submitted_at, attempt_status, score, is_passed, time_taken_seconds) FROM stdin;
2	5	7	2026-06-16 17:21:55.394552+05:30	2026-06-16 19:42:50.494456+05:30	AutoSubmitted	0.00	f	8455
3	5	7	2026-06-16 20:07:17.441394+05:30	2026-06-16 20:14:49.630904+05:30	Submitted	70.00	t	452
4	6	1	2026-06-17 19:19:25.65679+05:30	2026-06-17 19:34:42.991807+05:30	Submitted	10.00	f	917
6	6	9	2026-06-22 11:41:46.03686+05:30	2026-06-22 11:51:49.388576+05:30	AutoSubmitted	10.00	f	603
5	5	9	2026-06-22 11:39:55.265562+05:30	2026-06-22 11:52:28.34149+05:30	AutoSubmitted	40.00	t	753
7	5	8	2026-07-03 17:19:30.781303+05:30	2026-07-03 17:21:22.20053+05:30	Submitted	60.00	t	111
8	5	8	2026-07-08 10:53:35.811362+05:30	2026-07-08 10:54:41.85119+05:30	Submitted	40.00	t	66
\.


--
-- Data for Name: certificates; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.certificates (certificate_id, certificate_code, user_id, exam_id, attempt_id, score, issued_at, cert_status, revoked_at) FROM stdin;
2	CERT-2026-000002	5	7	3	70.00	2026-06-16 21:55:17.353903+05:30	Issued	\N
3	CERT-2026-000003	5	9	5	40.00	2026-06-22 12:51:40.048312+05:30	Issued	\N
\.


--
-- Data for Name: exam_categories; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.exam_categories (cat_id, cat_name, description) FROM stdin;
3	Web Development	HTML, CSS, JavaScript, Angular and frontend concepts
4	Software Engineering	Design patterns, SDLC, architecture and testing
5	Cloud & DevOps	AWS, Azure, CI/CD, containers and deployment fundamentals
6	Web APIs	Web api fundaentals
1	C# & .NET	C# programming using Microsoft .NET
7	Logical Reasoning	Math and more!
2	Database Systems (DBMS)	SQL, PostgreSQL, normalization and database concepts
\.


--
-- Data for Name: exam_questions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.exam_questions (exam_question_id, exam_id, question_id, question_order) FROM stdin;
2	1	1	1
3	1	2	2
4	1	3	3
5	1	4	4
6	1	5	5
7	1	6	6
8	1	7	7
9	1	8	8
10	1	9	9
11	1	10	10
12	7	16	1
13	7	17	2
14	7	18	3
15	7	19	4
16	7	20	5
17	7	36	6
18	7	37	7
19	8	38	1
20	8	39	2
21	8	40	3
22	8	48	4
23	8	49	5
24	8	50	6
25	9	1	1
26	9	2	2
27	9	3	3
28	9	4	4
29	9	5	5
\.


--
-- Data for Name: examiner_requests; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.examiner_requests (examiner_request_id, full_name, email, purpose, status, requested_at, processed_at, admin_note) FROM stdin;
1	Dr. Ashok Gupta	profashok@gmail.com	I just want to bro. I have a PhD, just let me become one.	Approved	2026-06-30 17:14:21.781642+05:30	2026-06-30 17:21:29.775838+05:30	\N
2	Dr Sai	sainikinsr@gmail.com	Test i want to be examiner	Rejected	2026-06-30 17:22:37.139751+05:30	2026-07-06 14:50:35.442331+05:30	
\.


--
-- Data for Name: exams; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.exams (exam_id, examiner_id, cat_id, title, instructions, duration_minutes, total_marks, passing_marks, start_time, end_time, exam_status, created_at) FROM stdin;
7	3	4	Software Engineering Assessment	\N	60	70.00	42.00	2026-06-15 05:30:00+05:30	2026-06-17 05:29:59+05:30	ResultsPublished	2026-06-15 10:38:34.193184+05:30
1	2	1	C# Certification Assessment	\N	60	100.00	60.00	2026-06-17 05:30:00+05:30	2026-06-19 05:29:59+05:30	ResultsPublished	2026-06-12 17:21:20.598292+05:30
8	7	5	Full Stack & DevOps Capstone Demo Exam	Answer all 6 questions covering Cloud/DevOps and Web Development. You have 30 minutes.	30	60.00	36.00	2026-06-19 05:30:00+05:30	2027-01-01 05:29:59+05:30	Published	2026-06-19 15:02:51.488742+05:30
9	2	1	C# Quick Check — Concurrent Test	5 quick C# questions. 10 minutes.	10	50.00	25.00	2026-06-19 05:30:00+05:30	2028-01-01 05:29:59+05:30	ResultsPublished	2026-06-22 10:51:52.722006+05:30
\.


--
-- Data for Name: notifications; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.notifications (notification_id, user_id, notif_type, message, related_exam_id, is_read, created_at) FROM stdin;
1	5	ResultPublished	Results for 'Software Engineering Assessment' are published. You passed with a score of 70.00.	7	t	2026-06-16 20:33:39.002466+05:30
2	5	CertificateRevoked	Your certificate for 'Software Engineering Assessment' has been revoked.	7	t	2026-06-16 21:43:53.4092+05:30
3	5	CertificateIssued	Congratulations! Your certificate for 'Software Engineering Assessment' is now available.	7	t	2026-06-16 21:55:17.490771+05:30
4	5	ResultPublished	Results for 'Software Engineering Assessment' are published. You passed with a score of 70.00.	7	t	2026-06-16 21:55:17.499432+05:30
5	6	ResultPublished	Results for 'C# Certification Assessment' are published. Your score was 10.00.	1	t	2026-06-17 19:45:46.446789+05:30
6	5	CertificateIssued	Congratulations! Your certificate for 'C# Quick Check — Concurrent Test' is now available.	9	t	2026-06-22 12:51:40.207587+05:30
7	5	ResultPublished	Results for 'C# Quick Check — Concurrent Test' are published. You passed with a score of 40.00.	9	t	2026-06-22 12:51:40.218325+05:30
8	6	ResultPublished	Results for 'C# Quick Check — Concurrent Test' are published. Your score was 10.00.	9	t	2026-06-22 12:51:40.218609+05:30
9	1	ExaminerRequestReceived	New examiner request from Dr. Ashok Gupta (profashok@gmail.com): "I just want to bro. I have a PhD, just let me become one.…"	\N	t	2026-06-30 17:14:21.876176+05:30
10	9	ExaminerAccountApproved	Welcome, Dr. Ashok Gupta! Your examiner account is ready. Please change your temporary password after your first login.	\N	f	2026-06-30 17:21:29.774613+05:30
11	1	ExaminerRequestReceived	New examiner request from Dr Sai (sainikinsr@gmail.com): "Test i want to be examiner…"	\N	t	2026-06-30 17:22:37.145102+05:30
14	7	AttemptResumed	Riya reconnected and resumed their in-progress attempt for 'Full Stack & DevOps Capstone Demo Exam' (possible connection loss).	8	t	2026-07-03 17:20:59.653037+05:30
12	7	AttemptResumed	Riya reconnected and resumed their in-progress attempt for 'Full Stack & DevOps Capstone Demo Exam' (possible connection loss).	8	t	2026-07-03 17:19:48.317884+05:30
13	7	AttemptResumed	Riya reconnected and resumed their in-progress attempt for 'Full Stack & DevOps Capstone Demo Exam' (possible connection loss).	8	t	2026-07-03 17:20:10.040795+05:30
\.


--
-- Data for Name: question_options; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.question_options (option_id, question_id, option_text, is_correct) FROM stdin;
1	1	class	t
2	1	object	f
3	1	new	f
4	1	struct	f
5	2	:	t
6	2	::	f
7	2	=>	f
8	2	->	f
9	3	interface	t
10	3	contract	f
11	3	implements	f
12	3	abstract	f
13	4	abstract	t
14	4	virtual	f
15	4	sealed	f
16	4	override	f
17	5	List<T>	t
18	5	Dictionary<TKey,TValue>	f
19	5	HashSet<T>	f
20	5	Queue<T>	f
21	6	Where()	t
22	6	Select()	f
23	6	OrderBy()	f
24	6	GroupBy()	f
25	7	async	t
26	7	await	f
27	7	Task	f
28	7	thread	f
29	8	Func<>	t
30	8	List<>	f
31	8	Task<>	f
32	8	IEnumerable<>	f
33	9	Entity Framework Core	t
34	9	Dapper	f
35	9	NHibernate	f
36	9	ADO.NET Only	f
37	10	Reducing coupling between components	t
38	10	Creating database tables	f
39	10	Improving SQL performance	f
40	10	Replacing interfaces	f
41	11	Uniquely identifies a row	t
42	11	Connects two tables	f
43	11	Stores duplicate values	f
44	11	Sorts data automatically	f
45	12	Links tables together	t
46	12	Identifies rows uniquely	f
47	12	Creates indexes	f
48	12	Deletes records	f
49	13	Reduce data redundancy	t
50	13	Increase duplicates	f
51	13	Speed up networking	f
52	13	Encrypt tables	f
53	14	INNER JOIN	t
54	14	LEFT JOIN	f
55	14	RIGHT JOIN	f
56	14	FULL JOIN	f
57	15	Improve query performance	t
58	15	Increase storage	f
59	15	Delete records	f
60	15	Create backups	f
61	16	Software Development Life Cycle	t
62	16	System Design Logic Cycle	f
63	16	Software Delivery Layer Control	f
64	16	System Development Link Chain	f
65	17	Singleton	t
66	17	Factory	f
67	17	Adapter	f
68	17	Strategy	f
69	18	Test individual components	t
70	18	Deploy applications	f
71	18	Manage databases	f
72	18	Create APIs	f
73	19	Sprint Review	t
74	19	Sprint Planning	f
75	19	Daily Standup	f
76	19	Retrospective	f
77	20	Dependency Injection	t
78	20	Global State	f
79	20	Tight Coupling	f
80	20	Static Dependencies	f
81	21	HTML	t
82	21	CSS	f
83	21	Java	f
84	21	SQL	f
85	22	CSS	t
86	22	HTML	f
87	22	C#	f
88	22	Python	f
89	23	Angular	t
90	23	Django	f
91	23	Spring	f
92	23	Laravel	f
93	24	*ngFor	t
94	24	*ngIf	f
95	24	*ngLoop	f
96	24	*for	f
97	25	RouterModule	t
98	25	FormsModule	f
99	25	HttpClient	f
100	25	NgZone	f
101	26	class	t
102	26	object	f
103	26	new	f
104	26	struct	f
105	27	:	t
106	27	::	f
107	27	=>	f
108	27	->	f
109	28	interface	t
110	28	contract	f
111	28	implements	f
112	28	abstract	f
113	29	abstract	t
114	29	virtual	f
115	29	sealed	f
116	29	override	f
117	30	List<T>	t
118	30	Dictionary<TKey,TValue>	f
119	30	HashSet<T>	f
120	30	Queue<T>	f
121	31	Where()	t
122	31	Select()	f
123	31	OrderBy()	f
124	31	GroupBy()	f
125	32	async	t
126	32	await	f
127	32	Task	f
128	32	thread	f
129	33	Func<>	t
130	33	List<>	f
131	33	Task<>	f
132	33	IEnumerable<>	f
133	34	Entity Framework Core	t
134	34	Dapper	f
135	34	NHibernate	f
136	34	ADO.NET Only	f
137	35	Reducing coupling between components	t
138	35	Creating database tables	f
139	35	Improving SQL performance	f
140	35	Replacing interfaces	f
141	36	Design Principles	t
142	36	Database Rules	f
143	36	Testing Framework	f
144	36	Deployment Strategy	f
145	37	Testing individual components	t
146	37	Deploying applications	f
147	37	Database optimization	f
148	37	Version control	f
149	38	Continuous Integration / Continuous Delivery	t
150	38	Computed Integration / Computed Delivery	f
151	38	Cloud Integration / Cloud Distribution	f
152	38	Control Integration / Control Deployment	f
153	39	S3	t
154	39	EC2	f
155	39	RDS	f
156	39	Lambda	f
157	40	docker build	t
158	40	docker run	f
159	40	docker pull	f
160	40	docker compose	f
161	41	Container orchestration	t
162	41	Database management	f
163	41	Source control	f
164	41	Code compilation	f
165	42	Dockerfile	t
166	42	docker-compose.yml	f
167	42	package.json	f
168	42	Makefile	f
169	43	Managing infra through machine-readable definition files	t
170	43	Writing application code on infrastructure servers	f
171	43	A type of cloud database	f
172	43	An operating system feature	f
173	44	Azure Functions	t
174	44	Azure SQL	f
175	44	Azure Blob Storage	f
176	44	Azure DevOps	f
177	45	Distribute traffic across multiple servers	t
178	45	Store backup data	f
179	45	Encrypt network traffic	f
180	45	Compile code remotely	f
181	46	Jenkins	t
182	46	Postman	f
183	46	Photoshop	f
184	46	Tableau	f
185	47	Zero-downtime releases with easy rollback	t
186	47	Faster CPU performance	f
187	47	Reduced storage cost	f
188	47	Smaller container sizes	f
189	48	GET	t
190	48	POST	f
191	48	DELETE	f
192	48	PATCH	f
193	49	Object representing eventual async completion	t
194	49	A synchronous function	f
195	49	A loop construct	f
196	49	A CSS property	f
197	50	[(ngModel)]	t
198	50	*ngFor	f
199	50	(click)	f
200	50	[hidden]	f
201	51	Cross-Origin Resource Sharing	t
202	51	Common Origin Request System	f
203	51	Cross-Origin Restricted Service	f
204	51	Content Origin Resource Standard	f
205	52	Decoupling object creation from usage	t
206	52	Compressing database queries	f
207	52	Validating HTTP requests	f
208	52	Encrypting configuration files	f
\.


--
-- Data for Name: questions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.questions (question_id, examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active) FROM stdin;
48	7	3	Which HTTP method is idempotent and reads data?	10.00	Easy	2026-06-19 14:56:29.115296+05:30	t
49	7	3	Which lifecycle hook runs first in Angular?	10.00	Medium	2026-06-19 14:56:29.154358+05:30	t
50	7	3	What is a Promise in JavaScript?	10.00	Medium	2026-06-19 14:56:29.203403+05:30	t
51	7	3	Which Angular feature enables two-way binding?	10.00	Medium	2026-06-19 14:56:29.221599+05:30	t
1	2	1	Which keyword is used to define a class in C#?	10.00	Easy	2026-06-12 16:19:37.715165+05:30	t
2	2	1	Which symbol is used for inheritance in C#?	10.00	Easy	2026-06-12 16:19:37.71533+05:30	t
3	2	1	What keyword is used to define an interface?	10.00	Easy	2026-06-12 16:19:37.715344+05:30	t
4	2	1	Which keyword is used to declare an abstract class?	10.00	Easy	2026-06-12 16:19:37.715345+05:30	t
5	2	1	Which collection stores a sequence of elements in C#?	10.00	Easy	2026-06-12 16:19:37.715346+05:30	t
6	2	1	Which LINQ method is used to filter records?	10.00	Medium	2026-06-12 16:19:37.715348+05:30	t
7	2	1	Which keyword is used to define an asynchronous method?	10.00	Medium	2026-06-12 16:19:37.715348+05:30	t
8	2	1	Which type represents a delegate in modern C#?	10.00	Medium	2026-06-12 16:19:37.715349+05:30	t
9	2	1	Which ORM framework are we using in this project?	10.00	Medium	2026-06-12 16:19:37.71535+05:30	t
10	2	1	What is Dependency Injection primarily used for?	10.00	Hard	2026-06-12 16:19:37.71535+05:30	t
11	2	2	What is a Primary Key?	10.00	Easy	2026-06-12 16:31:37.127335+05:30	t
12	2	2	What is a Foreign Key?	10.00	Easy	2026-06-12 16:31:37.127392+05:30	t
13	2	2	What is normalization used for?	10.00	Medium	2026-06-12 16:31:37.127393+05:30	t
14	2	2	Which JOIN returns matching rows from both tables?	10.00	Medium	2026-06-12 16:31:37.127394+05:30	t
15	2	2	Why are indexes used?	10.00	Medium	2026-06-12 16:31:37.127395+05:30	t
16	3	4	What does SDLC stand for?	10.00	Easy	2026-06-12 16:39:03.952014+05:30	t
17	3	4	Which design pattern creates only one instance of a class?	10.00	Medium	2026-06-12 16:39:03.952037+05:30	t
18	3	4	What is the purpose of unit testing?	10.00	Medium	2026-06-12 16:39:03.952038+05:30	t
19	3	4	Which Agile ceremony reviews completed work?	10.00	Medium	2026-06-12 16:39:03.952039+05:30	t
20	3	4	Which architecture principle promotes loose coupling?	10.00	Hard	2026-06-12 16:39:03.952039+05:30	t
21	3	3	Which language is used to structure web pages?	10.00	Easy	2026-06-12 16:39:10.264836+05:30	t
22	3	3	Which language is used for styling web pages?	10.00	Easy	2026-06-12 16:39:10.264848+05:30	t
23	3	3	Which JavaScript framework are we learning?	10.00	Easy	2026-06-12 16:39:10.264849+05:30	t
24	3	3	What directive is used for looping in Angular?	10.00	Medium	2026-06-12 16:39:10.26485+05:30	t
25	3	3	Which Angular feature manages application routes?	10.00	Medium	2026-06-12 16:39:10.264856+05:30	t
26	2	1	Which keyword is used to define a class in C#?	10.00	Easy	2026-06-13 08:04:51.923279+05:30	t
27	2	1	Which symbol is used for inheritance in C#?	10.00	Easy	2026-06-13 08:04:51.923303+05:30	t
28	2	1	What keyword is used to define an interface?	10.00	Easy	2026-06-13 08:04:51.923304+05:30	t
29	2	1	Which keyword is used to declare an abstract class?	10.00	Easy	2026-06-13 08:04:51.923306+05:30	t
30	2	1	Which collection stores a sequence of elements in C#?	10.00	Easy	2026-06-13 08:04:51.923313+05:30	t
31	2	1	Which LINQ method is used to filter records?	10.00	Medium	2026-06-13 08:04:51.923314+05:30	t
32	2	1	Which keyword is used to define an asynchronous method?	10.00	Medium	2026-06-13 08:04:51.923314+05:30	t
33	2	1	Which type represents a delegate in modern C#?	10.00	Medium	2026-06-13 08:04:51.923315+05:30	t
34	2	1	Which ORM framework are we using in this project?	10.00	Medium	2026-06-13 08:04:51.923316+05:30	t
35	2	1	What is Dependency Injection primarily used for?	10.00	Hard	2026-06-13 08:04:51.923317+05:30	t
36	3	4	What does SOLID stand for?	10.00	Easy	2026-06-15 08:27:06.727658+05:30	t
37	3	4	What is Unit Testing?	10.00	Easy	2026-06-15 08:27:06.731075+05:30	t
38	7	5	What does CI/CD stand for?	10.00	Easy	2026-06-19 14:47:47.353979+05:30	t
39	7	5	Which AWS service is used for object storage?	10.00	Easy	2026-06-19 14:47:47.356507+05:30	t
40	7	5	Which command builds a Docker image from a Dockerfile?	10.00	Easy	2026-06-19 14:47:47.357384+05:30	t
41	7	5	What is Kubernetes primarily used for?	10.00	Medium	2026-06-19 14:47:47.358538+05:30	t
42	7	5	Which file defines a Docker image's build steps?	10.00	Easy	2026-06-19 14:47:47.359451+05:30	t
43	7	5	What is Infrastructure as Code (IaC)?	10.00	Medium	2026-06-19 14:47:47.360173+05:30	t
44	7	5	Which Azure service hosts serverless functions?	10.00	Medium	2026-06-19 14:47:47.36089+05:30	t
45	7	5	What is the purpose of a load balancer?	10.00	Medium	2026-06-19 14:47:47.366564+05:30	t
46	7	5	Which tool is commonly used for CI/CD pipelines in DevOps?	10.00	Medium	2026-06-19 14:47:47.367385+05:30	t
47	7	5	What is the main benefit of blue-green deployment?	10.00	Hard	2026-06-19 14:47:47.36796+05:30	t
52	3	1	What is Dependency Injection primarily used for?	10.00	Medium	2026-07-03 11:29:19.834758+05:30	t
\.


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.users (user_id, username, email, password_hash, full_name, role, date_of_joining, is_active) FROM stdin;
2	proframu	proframu@gmail.com	$2a$11$FuLQ63ickbeDMfr3qIRa9O48bqiqPGRhSzjbD1Bgwr39Ab59cIYYu	Prof Ramu	Examiner	2026-06-12 15:11:47.850092+05:30	t
3	profraju	profraju@gmail.com	$2a$11$S5Shvqd65uUbbZQ2sstzreD8gCynm5mZWzdxklFYUZlEm2bgaaDFy	Prof Raju	Examiner	2026-06-12 15:12:08.718961+05:30	t
4	johncandidate	john@gmail.com	$2a$11$WGCgiKBeY5IlmLLP3otuVufTvhpgBrx81WJOb7gTo54Cq8BUo.i1W	John Candidate	Candidate	2026-06-15 10:54:28.688299+05:30	f
6	diya	diya@gmail.com	$2a$11$tjrZ9eS8GI8.XjxGylhMtOPlDut0o6Zo8YkYq7c6vV92rkSc2nX3G	Diya	Candidate	2026-06-15 13:58:08.161901+05:30	t
5	riya	sainikinsr@gmail.com	$2a$11$fmP/zVPt0Sg91E/AEsL8WOEc.21wZTvo4F4ZtxJVyu1qBA5YvemWS	Riya	Candidate	2026-06-15 13:57:18.150591+05:30	t
8	profrahul	profrahul@gmail.com	$2a$11$cJiVz2tVh19ED/L9wkuKJ.4xt17bFORwU.xIGnXgg6iEsVB9djRPu	Rahul Singh	Candidate	2026-06-30 15:07:14.995359+05:30	t
9	profashok	profashok@gmail.com	$2a$11$xfjMl1g145VnzRNgpn5qM.x/6XW45KO2rXERtZ6maHjz3o/WSGwN6	Dr. Ashok Gupta	Examiner	2026-06-30 17:21:29.753349+05:30	t
1	admin	admin@gmail.com	$2a$11$jm5H7dcZeFMsa/OGPuuloeCCb./1P3KZ2YDz2MYHgXsgzNxXqsG9y	Admin	Admin	2026-06-05 12:48:40.705458+05:30	t
12	candidate_phase1	candidate_phase1@gmail.com	$2a$11$Oczfd3t61oGvJQeSKI9m8uxIIyZs5wVS82lxoyaeV5vuEEX3IQhua	candidate_phase1	Candidate	2026-07-09 12:53:04.010128+05:30	t
7	profdemo	profdemo@gmail.com	$2a$11$XHGYPDAAoqn4T4L1YIYk6ej0qhT.ok3KwOC/FQ8XGJXf76iyu3U2u	Prof Demo	Examiner	2026-06-19 14:38:53.427358+05:30	t
\.


--
-- Name: attempt_answers_answer_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.attempt_answers_answer_id_seq', 37, true);


--
-- Name: attempts_attempt_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.attempts_attempt_id_seq', 8, true);


--
-- Name: certificates_certificate_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.certificates_certificate_id_seq', 3, true);


--
-- Name: exam_categories_cat_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.exam_categories_cat_id_seq', 7, true);


--
-- Name: exam_questions_exam_question_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.exam_questions_exam_question_id_seq', 29, true);


--
-- Name: examiner_requests_examiner_request_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.examiner_requests_examiner_request_id_seq', 2, true);


--
-- Name: exams_exam_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.exams_exam_id_seq', 9, true);


--
-- Name: notifications_notification_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.notifications_notification_id_seq', 14, true);


--
-- Name: question_options_option_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.question_options_option_id_seq', 208, true);


--
-- Name: questions_question_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.questions_question_id_seq', 52, true);


--
-- Name: users_user_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.users_user_id_seq', 12, true);


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: attempt_answers pk_attempt_answers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attempt_answers
    ADD CONSTRAINT pk_attempt_answers PRIMARY KEY (answer_id);


--
-- Name: attempts pk_attempts; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attempts
    ADD CONSTRAINT pk_attempts PRIMARY KEY (attempt_id);


--
-- Name: certificates pk_certificates; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.certificates
    ADD CONSTRAINT pk_certificates PRIMARY KEY (certificate_id);


--
-- Name: exam_categories pk_exam_categories; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exam_categories
    ADD CONSTRAINT pk_exam_categories PRIMARY KEY (cat_id);


--
-- Name: exam_questions pk_exam_questions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exam_questions
    ADD CONSTRAINT pk_exam_questions PRIMARY KEY (exam_question_id);


--
-- Name: examiner_requests pk_examiner_requests; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.examiner_requests
    ADD CONSTRAINT pk_examiner_requests PRIMARY KEY (examiner_request_id);


--
-- Name: exams pk_exams; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exams
    ADD CONSTRAINT pk_exams PRIMARY KEY (exam_id);


--
-- Name: notifications pk_notifications; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT pk_notifications PRIMARY KEY (notification_id);


--
-- Name: question_options pk_question_options; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.question_options
    ADD CONSTRAINT pk_question_options PRIMARY KEY (option_id);


--
-- Name: questions pk_questions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questions
    ADD CONSTRAINT pk_questions PRIMARY KEY (question_id);


--
-- Name: users pk_users; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT pk_users PRIMARY KEY (user_id);


--
-- Name: ix_attempt_answers_attempt_id_question_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_attempt_answers_attempt_id_question_id ON public.attempt_answers USING btree (attempt_id, question_id);


--
-- Name: ix_attempt_answers_question_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_attempt_answers_question_id ON public.attempt_answers USING btree (question_id);


--
-- Name: ix_attempt_answers_selected_option_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_attempt_answers_selected_option_id ON public.attempt_answers USING btree (selected_option_id);


--
-- Name: ix_attempts_exam_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_attempts_exam_id ON public.attempts USING btree (exam_id);


--
-- Name: ix_certificates_attempt_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_certificates_attempt_id ON public.certificates USING btree (attempt_id);


--
-- Name: ix_certificates_certificate_code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_certificates_certificate_code ON public.certificates USING btree (certificate_code);


--
-- Name: ix_certificates_exam_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_certificates_exam_id ON public.certificates USING btree (exam_id);


--
-- Name: ix_certificates_user_id_exam_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_certificates_user_id_exam_id ON public.certificates USING btree (user_id, exam_id);


--
-- Name: ix_exam_categories_cat_name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_exam_categories_cat_name ON public.exam_categories USING btree (cat_name);


--
-- Name: ix_exam_questions_exam_id_question_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_exam_questions_exam_id_question_id ON public.exam_questions USING btree (exam_id, question_id);


--
-- Name: ix_exam_questions_question_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_exam_questions_question_id ON public.exam_questions USING btree (question_id);


--
-- Name: ix_examiner_requests_email; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_examiner_requests_email ON public.examiner_requests USING btree (email);


--
-- Name: ix_exams_cat_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_exams_cat_id ON public.exams USING btree (cat_id);


--
-- Name: ix_exams_examiner_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_exams_examiner_id ON public.exams USING btree (examiner_id);


--
-- Name: ix_notifications_related_exam_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_notifications_related_exam_id ON public.notifications USING btree (related_exam_id);


--
-- Name: ix_notifications_user_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_notifications_user_id ON public.notifications USING btree (user_id);


--
-- Name: ix_question_options_question_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_question_options_question_id ON public.question_options USING btree (question_id);


--
-- Name: ix_questions_cat_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_questions_cat_id ON public.questions USING btree (cat_id);


--
-- Name: ix_questions_examiner_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_questions_examiner_id ON public.questions USING btree (examiner_id);


--
-- Name: ix_users_email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_users_email ON public.users USING btree (email);


--
-- Name: ix_users_username; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ix_users_username ON public.users USING btree (username);


--
-- Name: uix_attempts_user_exam_inprogress; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX uix_attempts_user_exam_inprogress ON public.attempts USING btree (user_id, exam_id) WHERE ((attempt_status)::text = 'InProgress'::text);


--
-- Name: attempt_answers fk_attempt_answers__question_options_selected_option_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attempt_answers
    ADD CONSTRAINT fk_attempt_answers__question_options_selected_option_id FOREIGN KEY (selected_option_id) REFERENCES public.question_options(option_id) ON DELETE RESTRICT;


--
-- Name: attempt_answers fk_attempt_answers__questions_question_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attempt_answers
    ADD CONSTRAINT fk_attempt_answers__questions_question_id FOREIGN KEY (question_id) REFERENCES public.questions(question_id) ON DELETE RESTRICT;


--
-- Name: attempt_answers fk_attempt_answers_attempts_attempt_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attempt_answers
    ADD CONSTRAINT fk_attempt_answers_attempts_attempt_id FOREIGN KEY (attempt_id) REFERENCES public.attempts(attempt_id) ON DELETE CASCADE;


--
-- Name: attempts fk_attempts__exams_exam_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attempts
    ADD CONSTRAINT fk_attempts__exams_exam_id FOREIGN KEY (exam_id) REFERENCES public.exams(exam_id) ON DELETE RESTRICT;


--
-- Name: attempts fk_attempts__users_user_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attempts
    ADD CONSTRAINT fk_attempts__users_user_id FOREIGN KEY (user_id) REFERENCES public.users(user_id) ON DELETE RESTRICT;


--
-- Name: certificates fk_certificates__exams_exam_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.certificates
    ADD CONSTRAINT fk_certificates__exams_exam_id FOREIGN KEY (exam_id) REFERENCES public.exams(exam_id) ON DELETE RESTRICT;


--
-- Name: certificates fk_certificates__users_user_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.certificates
    ADD CONSTRAINT fk_certificates__users_user_id FOREIGN KEY (user_id) REFERENCES public.users(user_id) ON DELETE RESTRICT;


--
-- Name: certificates fk_certificates_attempts_attempt_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.certificates
    ADD CONSTRAINT fk_certificates_attempts_attempt_id FOREIGN KEY (attempt_id) REFERENCES public.attempts(attempt_id) ON DELETE RESTRICT;


--
-- Name: exam_questions fk_exam_questions__questions_question_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exam_questions
    ADD CONSTRAINT fk_exam_questions__questions_question_id FOREIGN KEY (question_id) REFERENCES public.questions(question_id) ON DELETE RESTRICT;


--
-- Name: exam_questions fk_exam_questions_exams_exam_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exam_questions
    ADD CONSTRAINT fk_exam_questions_exams_exam_id FOREIGN KEY (exam_id) REFERENCES public.exams(exam_id) ON DELETE CASCADE;


--
-- Name: exams fk_exams__exam_categories_cat_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exams
    ADD CONSTRAINT fk_exams__exam_categories_cat_id FOREIGN KEY (cat_id) REFERENCES public.exam_categories(cat_id) ON DELETE RESTRICT;


--
-- Name: exams fk_exams__users_examiner_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.exams
    ADD CONSTRAINT fk_exams__users_examiner_id FOREIGN KEY (examiner_id) REFERENCES public.users(user_id) ON DELETE RESTRICT;


--
-- Name: notifications fk_notifications__users_user_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT fk_notifications__users_user_id FOREIGN KEY (user_id) REFERENCES public.users(user_id) ON DELETE CASCADE;


--
-- Name: notifications fk_notifications_exams_related_exam_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT fk_notifications_exams_related_exam_id FOREIGN KEY (related_exam_id) REFERENCES public.exams(exam_id) ON DELETE RESTRICT;


--
-- Name: question_options fk_question_options_questions_question_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.question_options
    ADD CONSTRAINT fk_question_options_questions_question_id FOREIGN KEY (question_id) REFERENCES public.questions(question_id) ON DELETE CASCADE;


--
-- Name: questions fk_questions__users_examiner_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questions
    ADD CONSTRAINT fk_questions__users_examiner_id FOREIGN KEY (examiner_id) REFERENCES public.users(user_id) ON DELETE RESTRICT;


--
-- Name: questions fk_questions_exam_categories_cat_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questions
    ADD CONSTRAINT fk_questions_exam_categories_cat_id FOREIGN KEY (cat_id) REFERENCES public.exam_categories(cat_id) ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--

\unrestrict f1VSo2Vhz7nzrA06559C5NeFwCoD8Zwl7x5r9m473Nqyv7wx4a7CXYfndkxZqOp

