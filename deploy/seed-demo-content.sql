BEGIN;
INSERT INTO exam_categories (cat_name, description)
SELECT 'Python Programming', 'Python syntax, data types, functions, OOP and standard-library essentials'
WHERE NOT EXISTS (SELECT 1 FROM exam_categories WHERE cat_name = 'Python Programming');
INSERT INTO exam_categories (cat_name, description)
SELECT 'Data Structures & Algorithms', 'Complexity analysis, lists, trees, graphs, sorting and searching'
WHERE NOT EXISTS (SELECT 1 FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms');
INSERT INTO exam_categories (cat_name, description)
SELECT 'Cybersecurity Fundamentals', 'Web security, authentication, common attacks and defensive practices'
WHERE NOT EXISTS (SELECT 1 FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals');
INSERT INTO exam_categories (cat_name, description)
SELECT 'AI & Machine Learning Basics', 'Core ML concepts: supervised learning, evaluation, overfitting and LLMs'
WHERE NOT EXISTS (SELECT 1 FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics');
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which HTTP method is idempotent and used to fully replace a resource?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Which HTTP method is idempotent and used to fully replace a resource?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'POST', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'PUT', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'PATCH', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'OPTIONS', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does HTTP status 201 indicate?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'What does HTTP status 201 indicate?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Request accepted for async processing', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Resource created successfully', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Resource moved permanently', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'No content to return', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'In REST, what does HATEOAS provide?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'In REST, what does HATEOAS provide?', 6, 'Hard', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Hypermedia links to navigate the API', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Automatic caching of responses', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Binary serialization', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Load balancing', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which header carries a JWT bearer token?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Which header carries a JWT bearer token?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'X-Api-Key', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Authorization', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Content-Type', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Accept', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What status code should a missing resource return?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'What status code should a missing resource return?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '400', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '401', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '404', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '500', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which HTTP status indicates the client sent invalid data?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Which HTTP status indicates the client sent invalid data?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '400 Bad Request', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '403 Forbidden', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '409 Conflict', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '502 Bad Gateway', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What is the main purpose of API versioning?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'What is the main purpose of API versioning?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Improve response speed', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Evolve the API without breaking existing clients', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Encrypt payloads', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Reduce server memory', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which is a common strategy to protect an API from abuse?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Which is a common strategy to protect an API from abuse?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Rate limiting', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Verbose logging', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Long-lived sessions', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Disabling HTTPS', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does CORS control?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'What does CORS control?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Database access rules', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Which browser origins may call the API', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Serialization format', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Password hashing', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'A 409 Conflict is most appropriate when…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'A 409 Conflict is most appropriate when…', 6, 'Hard', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'the token expired', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'a duplicate resource insert violates state', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'the payload is too large', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'the server is overloaded', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Find the next number: 2, 6, 12, 20, 30, ?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Find the next number: 2, 6, 12, 20, 30, ?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '36', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '40', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '42', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '44', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'If all Bloops are Razzies and all Razzies are Lubs, then all Bloops are…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'If all Bloops are Razzies and all Razzies are Lubs, then all Bloops are…', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Lubs', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Not Lubs', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Razzies only', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Cannot say', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'A clock shows 3:15. What is the angle between the hands?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'A clock shows 3:15. What is the angle between the hands?', 6, 'Hard', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '0°', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '7.5°', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '30°', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '37.5°', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Complete the series: A, C, F, J, O, ?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Complete the series: A, C, F, J, O, ?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'S', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'T', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'U', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'V', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'If MONDAY is coded as NPOEBZ, how is FRIDAY coded?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'If MONDAY is coded as NPOEBZ, how is FRIDAY coded?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'GSJEBZ', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'GSJEBX', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'GQJEBZ', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'GSKEBZ', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'A is B''s sister. C is B''s mother. D is C''s father. How is A related to D?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'A is B''s sister. C is B''s mother. D is C''s father. How is A related to D?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Granddaughter', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Daughter', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Aunt', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Niece', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which number is the odd one out: 3, 5, 11, 14, 17, 21?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Which number is the odd one out: 3, 5, 11, 14, 17, 21?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '11', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '14', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '17', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '21', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Pointing to a photo, Ram says ''She is the daughter of my grandfather''s only son.'' Who is she?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Pointing to a photo, Ram says ''She is the daughter of my grandfather''s only son.'' Who is she?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'His sister', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'His mother', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'His cousin', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'His aunt', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Water is to pipe as electricity is to…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'Water is to pipe as electricity is to…', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Bulb', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Wire', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Switch', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Battery', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'A train 150 m long passes a pole in 15 seconds. Its speed is…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (2, cid, 'A train 150 m long passes a pole in 15 seconds. Its speed is…', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '10 m/s', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '12 m/s', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '15 m/s', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '20 m/s', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What is the output of: print(type([]))?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'What is the output of: print(type([]))?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '<class ''list''>', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '<class ''tuple''>', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '<class ''dict''>', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '<class ''set''>', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which keyword defines an anonymous function in Python?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which keyword defines an anonymous function in Python?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'def', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'func', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'lambda', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'anon', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does a list comprehension [x*x for x in range(3)] produce?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'What does a list comprehension [x*x for x in range(3)] produce?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '[0, 1, 4]', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '[1, 4, 9]', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '[0, 1, 2]', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '(0, 1, 4)', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which collection type is immutable?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which collection type is immutable?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'list', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'dict', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'set', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'tuple', true);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does the ''self'' parameter refer to?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'What does the ''self'' parameter refer to?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'The class itself', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'The current instance', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'The parent class', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'A global object', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which statement handles exceptions?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which statement handles exceptions?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'catch/throw', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'try/except', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'begin/rescue', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'on/error', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What is the result of 7 // 2 in Python 3?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'What is the result of 7 // 2 in Python 3?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '3.5', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '3', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '4', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '2', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which decorator makes a method callable on the class rather than an instance?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which decorator makes a method callable on the class rather than an instance?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '@property', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '@staticmethod', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '@classmethod', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, '@abstractmethod', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does the GIL (Global Interpreter Lock) limit?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'What does the GIL (Global Interpreter Lock) limit?', 6, 'Hard', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Memory allocation', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'True parallel execution of Python threads', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'File I/O throughput', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Recursion depth', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which built-in creates key/value pairs from two lists?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which built-in creates key/value pairs from two lists?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'zip() with dict()', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'map() with list()', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'join()', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'enumerate() alone', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What is the average time complexity of binary search?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'What is the average time complexity of binary search?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(n)', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(log n)', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(n log n)', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(1)', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which data structure uses FIFO ordering?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which data structure uses FIFO ordering?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Stack', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Queue', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Tree', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Graph', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which structure gives O(1) average lookup by key?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which structure gives O(1) average lookup by key?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Linked list', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Hash table', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Binary tree', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Array', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What is the worst-case complexity of quicksort?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'What is the worst-case complexity of quicksort?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(n)', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(n log n)', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(n²)', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(log n)', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'In a max-heap, the largest element is at…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'In a max-heap, the largest element is at…', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'a leaf', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'the root', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'the middle', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'random position', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which traversal of a BST yields sorted order?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which traversal of a BST yields sorted order?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Pre-order', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Post-order', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'In-order', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Level-order', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which algorithm finds shortest paths from one source with non-negative weights?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which algorithm finds shortest paths from one source with non-negative weights?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Kruskal''s', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Dijkstra''s', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Prim''s', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'DFS', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'A stack overflow during recursion is caused by…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'A stack overflow during recursion is caused by…', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'too many stack frames', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'heap exhaustion', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'integer overflow', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'cache misses', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which structure best implements undo functionality?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Which structure best implements undo functionality?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Queue', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Stack', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Heap', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Graph', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Amortized time for appending to a dynamic array is…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (3, cid, 'Amortized time for appending to a dynamic array is…', 6, 'Hard', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(1)', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(n)', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(log n)', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'O(n²)', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does SQL injection exploit?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'What does SQL injection exploit?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Weak passwords', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Unsanitized user input in queries', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Open ports', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Expired certificates', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What is the purpose of hashing passwords?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'What is the purpose of hashing passwords?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Compression', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'One-way storage so plaintext is never kept', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Faster login', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Encryption for transport', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which attack tricks a user into clicking a disguised element?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Which attack tricks a user into clicking a disguised element?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Phishing', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Clickjacking', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'DDoS', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Brute force', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does HTTPS add over HTTP?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'What does HTTPS add over HTTP?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Compression', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Encryption in transit via TLS', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Faster routing', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Caching', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What is the principle of least privilege?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'What is the principle of least privilege?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Give users admin by default', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Grant only the access needed for the task', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Rotate passwords weekly', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Use one shared account', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'XSS attacks inject…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'XSS attacks inject…', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'SQL into the database', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'scripts into pages viewed by others', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'packets into routers', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'keys into TLS', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does 2FA add to authentication?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'What does 2FA add to authentication?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'A second independent verification factor', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'A longer password', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Biometric only', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Session caching', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'A CSRF attack abuses…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'A CSRF attack abuses…', 6, 'Hard', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'stored server sessions of a logged-in user', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'DNS resolution', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'weak hashing', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'open Wi-Fi', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Salting a password hash defends against…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Salting a password hash defends against…', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'brute force on live systems', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'rainbow-table precomputation', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'phishing', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'keyloggers', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which is an example of social engineering?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Which is an example of social engineering?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Port scanning', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Pretexting phone call to reveal a password', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'SYN flood', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'ARP spoofing', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Supervised learning requires…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Supervised learning requires…', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'unlabeled data', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'labeled training examples', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'a reward signal', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'no data', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which task is a classification problem?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Which task is a classification problem?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Predicting house price', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Detecting spam emails', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Forecasting temperature', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Estimating age in years', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Overfitting means the model…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Overfitting means the model…', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'performs poorly on training data', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'memorizes training data and fails to generalize', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'is too simple', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'trains too slowly', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which technique helps reduce overfitting?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Which technique helps reduce overfitting?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'More model parameters', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Regularization', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Higher learning rate', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Removing validation set', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What does a confusion matrix summarize?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'What does a confusion matrix summarize?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Feature correlations', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Classification prediction outcomes', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Training speed', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Data cleaning steps', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Gradient descent is used to…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Gradient descent is used to…', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'label data', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'minimize a loss function', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'split datasets', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'encrypt weights', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Which is an unsupervised method?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Which is an unsupervised method?', 5, 'Medium', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Linear regression', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'K-means clustering', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Logistic regression', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Decision-tree classification', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'A train/test split exists to…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'A train/test split exists to…', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'speed up training', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'measure generalization on unseen data', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'balance classes', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'reduce memory', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'Precision measures…') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'Precision measures…', 6, 'Hard', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'correct positives among predicted positives', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'correct positives among actual positives', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'overall accuracy', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'error variance', false);
  END IF;
END $$;
DO $$
DECLARE cid int; qid int;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF NOT EXISTS (SELECT 1 FROM questions WHERE question_text = 'What is a large language model trained to do at its core?') THEN
    INSERT INTO questions (examiner_id, cat_id, question_text, marks, difficulty, created_at, is_active)
    VALUES (9, cid, 'What is a large language model trained to do at its core?', 4, 'Easy', now(), true)
    RETURNING question_id INTO qid;
    INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Rank web pages', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Predict the next token in text', true); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Compress images', false); INSERT INTO question_options (question_id, option_text, is_correct) VALUES (qid, 'Solve equations symbolically', false);
  END IF;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Web API Mastery Certification') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (2, cid, 'Web API Mastery Certification', 'Covers REST fundamentals, status codes, security and versioning. Passing earns a verified certificate.', 40,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), true, 799, true, 1)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Web APIs');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Logical Reasoning';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Logical Reasoning Sprint') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (2, cid, 'Logical Reasoning Sprint', 'Timed aptitude practice. You have 3 attempts — your best score counts.', 25,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), false, NULL, false, 3)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Logical Reasoning');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Python Programming Essentials') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (3, cid, 'Python Programming Essentials', 'Core Python for beginners: types, functions, comprehensions and exceptions. Two attempts allowed.', 35,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), false, NULL, false, 2)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Python Programming');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Data Structures & Algorithms';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Data Structures Deep Dive') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (3, cid, 'Data Structures Deep Dive', 'Certification-level DSA assessment. Single attempt — read each question carefully.', 45,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), true, 999, false, 1)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Data Structures & Algorithms');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cybersecurity Fundamentals';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Cybersecurity Basics Challenge') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (9, cid, 'Cybersecurity Basics Challenge', 'Practical web-security awareness test: attacks, defenses and best practices.', 30,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), false, NULL, false, 2)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Cybersecurity Fundamentals');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'AI & Machine Learning Basics';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'AI & ML Foundations') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (9, cid, 'AI & ML Foundations', 'Introductory machine-learning concepts — no coding required.', 30,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), false, NULL, false, 2)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'AI & Machine Learning Basics');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web APIs';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Full Stack Developer Certification') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (7, cid, 'Full Stack Developer Certification', 'Premium certification spanning API design and security. Institute-verified certificate on passing.', 60,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), true, 1499, true, 1)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Web APIs');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Web Development';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Frontend Fundamentals Refresher') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (7, cid, 'Frontend Fundamentals Refresher', 'Quick HTML/CSS/JS/Angular refresher. Three attempts — best score counts.', 30,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), false, NULL, false, 3)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Web Development');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Database Systems (DBMS)';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'SQL Practice Test') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (18, cid, 'SQL Practice Test', 'Practice SQL queries, joins and normalization. Two attempts allowed.', 30,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), false, NULL, false, 2)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Database Systems (DBMS)');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Cloud & DevOps';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Cloud Concepts Quiz') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (18, cid, 'Cloud Concepts Quiz', 'Draft — cloud fundamentals quiz being prepared.', 25,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Draft', now(), false, NULL, false, 1)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Cloud & DevOps');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Software Engineering';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Software Engineering Principles') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (25, cid, 'Software Engineering Principles', 'SDLC, design patterns and testing fundamentals.', 35,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), false, NULL, false, 2)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Software Engineering');
  END LOOP;
END $$;
DO $$
DECLARE cid int; eid int; total numeric; passing numeric; n int := 0; r record;
BEGIN
  SELECT cat_id INTO cid FROM exam_categories WHERE cat_name = 'Python Programming';
  IF EXISTS (SELECT 1 FROM exams WHERE title = 'Python Aptitude Mix') THEN RETURN; END IF;

  SELECT COALESCE(SUM(marks), 0) INTO total FROM questions WHERE cat_id = cid AND is_active;
  IF total = 0 THEN RETURN; END IF;
  passing := round(total * 0.4);

  INSERT INTO exams (examiner_id, cat_id, title, instructions, duration_minutes,
                     total_marks, passing_marks, start_time, end_time, exam_status,
                     created_at, is_premium, price, is_featured, max_attempts)
  VALUES (25, cid, 'Python Aptitude Mix', 'Mixed-difficulty Python drill. Three attempts — best score counts.', 30,
          total, passing, now() - interval '1 day', now() + interval '60 days',
          'Published', now(), false, NULL, false, 3)
  RETURNING exam_id INTO eid;

  FOR r IN SELECT question_id FROM questions WHERE cat_id = cid AND is_active ORDER BY question_id LOOP
    n := n + 1;
    INSERT INTO exam_questions (exam_id, question_id, question_order, analytic_category)
    VALUES (eid, r.question_id, n, 'Python Programming');
  END LOOP;
END $$;
COMMIT;
