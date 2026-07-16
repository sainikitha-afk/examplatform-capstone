// CoachContextBuilderTests — the deterministic core of the AI Learning Coach.
//
// These tests exist because the whole security and honesty story of the feature rests on
// this class: the numbers the coach quotes are computed HERE, in C#, never by Gemini. So
// we pin down (a) which attempts are allowed in, (b) that the maths is stable and correct,
// and (c) that a candidate can never see another candidate's data through it.

using ExamPlatform.BLL.Models;
using ExamPlatform.BLL.Services;
using ExamPlatform.BLL.Tests.Helpers;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class CoachContextBuilderTests
    {
        private const int CandidateId = 1;
        private const int OtherCandidateId = 2;

        // ── Local builders ────────────────────────────────────────────────
        // TestDataBuilder.BuildSubmittedAttempt does not attach the Exam navigation, and
        // the publication gate is expressed on Exam.ExamStatus — so these helpers wire the
        // exam on explicitly. Kept local rather than editing the shared TestDataBuilder.

        private static Attempt SubmittedOn(
            Exam exam,
            int attemptId,
            int userId = CandidateId,
            decimal score = 40m,
            bool isPassed = true,
            int minutesAgo = 5)
        {
            var attempt = TestDataBuilder.BuildSubmittedAttempt(attemptId, userId, exam.ExamId, score, isPassed);
            attempt.SubmittedAt = DateTime.UtcNow.AddMinutes(-minutesAgo);
            attempt.Exam = exam;
            return attempt;
        }

        private static Attempt InProgressOn(Exam exam, int attemptId, int userId = CandidateId)
        {
            var attempt = TestDataBuilder.BuildInProgressAttempt(attemptId, userId, exam.ExamId);
            attempt.Exam = exam;
            return attempt;
        }

        private static IReadOnlyDictionary<int, IReadOnlyDictionary<string, decimal>> Topics(
            params (int AttemptId, (string Topic, decimal Score)[] Scores)[] entries)
        {
            return entries.ToDictionary(
                e => e.AttemptId,
                e => (IReadOnlyDictionary<string, decimal>)e.Scores.ToDictionary(s => s.Topic, s => s.Score));
        }

        private static CoachSnapshot Build(
            IEnumerable<Attempt> attempts,
            IReadOnlyDictionary<int, IReadOnlyDictionary<string, decimal>>? topics = null,
            IEnumerable<Certificate>? certificates = null,
            int userId = CandidateId,
            string fullName = "Diya Sharma")
            => CoachContextBuilder.Build(
                userId,
                fullName,
                attempts,
                certificates ?? Array.Empty<Certificate>(),
                topics ?? new Dictionary<int, IReadOnlyDictionary<string, decimal>>());

        // ════════════════════════════════════════════════════════════════
        //   AUTHORIZATION FILTER — what the coach is allowed to see
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Build_NoAttempts_ReturnsNoDataSnapshot()
        {
            var snapshot = Build(Array.Empty<Attempt>());

            snapshot.HasData.Should().BeFalse();
            snapshot.TotalAttempts.Should().Be(0);
            snapshot.Topics.Should().BeEmpty();
            snapshot.FocusAreas.Should().BeEmpty();
        }

        [Test]
        public void Build_UnpublishedResults_AreExcluded()
        {
            // The exam is Published (candidates could sit it) but the examiner has NOT
            // released results. The score exists in the DB — it must not reach the coach.
            var unpublished = TestDataBuilder.BuildPublishedExam(examId: 1);
            var attempts = new[] { SubmittedOn(unpublished, attemptId: 1, score: 45m) };

            var snapshot = Build(attempts, Topics((1, [("Recursion", 90m)])));

            snapshot.HasData.Should().BeFalse("results for this exam have not been published");
            snapshot.TotalAttempts.Should().Be(0);
            snapshot.Topics.Should().BeEmpty();
        }

        [Test]
        public void Build_InProgressAttempt_IsExcluded()
        {
            // No score has been computed yet — there is nothing to coach on.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempts = new[] { InProgressOn(exam, attemptId: 1) };

            var snapshot = Build(attempts);

            snapshot.HasData.Should().BeFalse();
            snapshot.TotalAttempts.Should().Be(0);
        }

        [Test]
        public void Build_AutoSubmittedAttempt_IsIncluded()
        {
            // A timed-out attempt is still an evaluated attempt.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempt = SubmittedOn(exam, attemptId: 1, score: 30m, isPassed: false);
            attempt.AttemptStatus = AttemptStatus.AutoSubmitted;

            var snapshot = Build(new[] { attempt });

            snapshot.HasData.Should().BeTrue();
            snapshot.TotalAttempts.Should().Be(1);
        }

        [Test]
        public void Build_AnotherCandidatesAttempt_IsExcluded()
        {
            // Defence in depth: even if a repository change ever leaked a foreign row into
            // the list, the builder refuses to use it.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempts = new[]
            {
                SubmittedOn(exam, attemptId: 1, userId: CandidateId,      score: 20m, isPassed: false),
                SubmittedOn(exam, attemptId: 2, userId: OtherCandidateId, score: 95m, isPassed: true),
            };

            var snapshot = Build(attempts, Topics(
                (1, [("Recursion", 20m)]),
                (2, [("Recursion", 95m)])));

            snapshot.TotalAttempts.Should().Be(1);
            snapshot.AverageScore.Should().Be(20, "only the authenticated candidate's attempt counts");
            snapshot.Topics.Single().Score.Should().Be(20m, "the other candidate's 95% must not bleed in");
        }

        [Test]
        public void Build_MixedSet_KeepsOnlyOwnedSubmittedAndPublished()
        {
            var published = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var unpublished = TestDataBuilder.BuildPublishedExam(examId: 2);

            var attempts = new[]
            {
                SubmittedOn(published,   attemptId: 1, score: 40m),                          // ✔ counted
                SubmittedOn(unpublished, attemptId: 2, score: 50m),                          // ✘ not published
                InProgressOn(published,  attemptId: 3),                                      // ✘ no score
                SubmittedOn(published,   attemptId: 4, userId: OtherCandidateId, score: 90m) // ✘ not owned
            };

            var snapshot = Build(attempts);

            snapshot.TotalAttempts.Should().Be(1);
            snapshot.AverageScore.Should().Be(40);
        }

        // ════════════════════════════════════════════════════════════════
        //   DETERMINISTIC MATHS — the numbers Gemini is never asked for
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Build_ComputesAverageScoreAndPassRate()
        {
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempts = new[]
            {
                SubmittedOn(exam, attemptId: 1, score: 30m, isPassed: false),
                SubmittedOn(exam, attemptId: 2, score: 50m, isPassed: true),
                SubmittedOn(exam, attemptId: 3, score: 40m, isPassed: true),
            };

            var snapshot = Build(attempts);

            snapshot.TotalAttempts.Should().Be(3);
            snapshot.AverageScore.Should().Be(40);
            snapshot.PassRate.Should().BeApproximately(66.67, 0.01);
        }

        [Test]
        public void Build_CountsOnlyIssuedCertificates()
        {
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var certs = new[]
            {
                TestDataBuilder.BuildCertificate(certId: 1, status: CertificateStatus.Issued),
                TestDataBuilder.BuildCertificate(certId: 2, status: CertificateStatus.Revoked),
            };

            var snapshot = Build(new[] { SubmittedOn(exam, attemptId: 1) }, certificates: certs);

            snapshot.CertificatesEarned.Should().Be(1, "a revoked certificate must not inflate the count");
        }

        [Test]
        public void Build_AveragesTopicScoresAcrossAttempts()
        {
            // The same topic appears in two attempts — a single good exam must not erase
            // a consistent weakness, so we average rather than take the latest.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempts = new[]
            {
                SubmittedOn(exam, attemptId: 1, minutesAgo: 10),
                SubmittedOn(exam, attemptId: 2, minutesAgo: 5),
            };

            var snapshot = Build(attempts, Topics(
                (1, [("LINQ", 40m)]),
                (2, [("LINQ", 80m)])));

            snapshot.Topics.Single().Should().BeEquivalentTo(new TopicScore("LINQ", 60m));
        }

        [Test]
        public void Build_IdentifiesStrongTopicsAndFocusAreas()
        {
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempts = new[] { SubmittedOn(exam, attemptId: 1) };

            var snapshot = Build(attempts, Topics((1,
            [
                ("OOP Concepts",       85m),   // strength
                ("LINQ",               75m),   // strength
                ("Exception Handling", 35m),   // High priority   (< 40)
                ("Async/Await",        55m),   // Medium priority (40–69)
            ])));

            snapshot.StrongTopics.Select(t => t.Topic)
                .Should().Equal("OOP Concepts", "LINQ");

            // Focus areas are ordered weakest-first.
            snapshot.FocusAreas.Select(f => f.Topic)
                .Should().Equal("Exception Handling", "Async/Await");

            snapshot.FocusAreas[0].Priority.Should().Be(FocusPriority.High);
            snapshot.FocusAreas[1].Priority.Should().Be(FocusPriority.Medium);
            snapshot.FocusAreas[0].Reason.Should().Contain("Lowest topic score");
        }

        [Test]
        public void Build_NoTopicReachesThreshold_ClaimsNoStrengths()
        {
            // Honesty check: a 45% is not a "strength". We would rather return an empty
            // list than dress up a weak score — and the model cannot add one back,
            // because LearningCoachService drops topics that are not in this context.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);

            var snapshot = Build(new[] { SubmittedOn(exam, attemptId: 1) },
                Topics((1, [("LINQ", 45m), ("Delegates", 30m)])));

            snapshot.StrongTopics.Should().BeEmpty();
            snapshot.FocusAreas.Should().HaveCount(2);
        }

        [TestCase(0, FocusPriority.High)]
        [TestCase(39.99, FocusPriority.High)]
        [TestCase(40, FocusPriority.Medium)]
        [TestCase(69.99, FocusPriority.Medium)]
        [TestCase(70, FocusPriority.Low)]
        [TestCase(100, FocusPriority.Low)]
        public void PriorityFor_UsesBackendOwnedScoreBands(decimal score, FocusPriority expected)
        {
            CoachContextBuilder.PriorityFor(score).Should().Be(expected);
        }

        [Test]
        public void Build_IsDeterministic_SameInputProducesSameOrdering()
        {
            // Ties are broken by topic name so two runs can never disagree — important
            // because these values are quoted back to the candidate as fact.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempts = new[] { SubmittedOn(exam, attemptId: 1) };
            var topics = Topics((1, [("Beta", 50m), ("Alpha", 50m), ("Gamma", 50m)]));

            var first = Build(attempts, topics);
            var second = Build(attempts, topics);

            first.Topics.Select(t => t.Topic).Should().Equal("Alpha", "Beta", "Gamma");
            second.Topics.Should().BeEquivalentTo(first.Topics, o => o.WithStrictOrdering());
            second.FocusAreas.Should().BeEquivalentTo(first.FocusAreas, o => o.WithStrictOrdering());
        }

        // ════════════════════════════════════════════════════════════════
        //   BOUNDS — what may be sent to Gemini stays small
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Build_CapsRecentAttempts()
        {
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempts = Enumerable.Range(1, 9)
                .Select(i => SubmittedOn(exam, attemptId: i, minutesAgo: i))
                .ToList();

            var snapshot = Build(attempts);

            snapshot.TotalAttempts.Should().Be(9, "every attempt still counts toward the stats");
            snapshot.RecentAttempts.Should().HaveCount(CoachAttemptRules.MaxRecentAttempts,
                "but only the newest few are sent to the model");
            snapshot.RecentAttempts[0].SubmittedAt.Should()
                .BeAfter(snapshot.RecentAttempts[1].SubmittedAt, "newest first");
        }

        [Test]
        public void Build_CapsTopics_KeepingBothWeakestAndStrongest()
        {
            // With more topics than the cap we keep both ends of the distribution: the
            // weakest (what to fix) and the strongest (what to build on).
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var many = Enumerable.Range(1, 20)
                .Select(i => ($"Topic {i:D2}", (decimal)(i * 5)))
                .ToArray();

            var snapshot = Build(new[] { SubmittedOn(exam, attemptId: 1) }, Topics((1, many)));

            snapshot.Topics.Should().HaveCount(CoachAttemptRules.MaxTopics);
            snapshot.Topics.Select(t => t.Score).Max().Should().Be(100m, "the strongest survives the cap");
            snapshot.Topics.Select(t => t.Score).Min().Should().Be(5m, "the weakest survives the cap");
        }

        [Test]
        public void Build_AttemptWithNoTopicData_StillCountsTowardStats()
        {
            // Analytics may still be processing, the JSON may be missing, or it may be
            // malformed. The attempt is real and its score is real — only its topics are
            // unknown, and we say nothing about topics we do not have.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);
            var attempts = new[]
            {
                SubmittedOn(exam, attemptId: 1, score: 40m, minutesAgo: 10),
                SubmittedOn(exam, attemptId: 2, score: 60m, minutesAgo: 5),
            };

            // Only attempt 1 has topic data.
            var snapshot = Build(attempts, Topics((1, [("LINQ", 30m)])));

            snapshot.TotalAttempts.Should().Be(2);
            snapshot.AverageScore.Should().Be(50);
            snapshot.Topics.Should().ContainSingle().Which.Topic.Should().Be("LINQ");
            snapshot.RecentAttempts.Single(a => a.Score == 60m).Topics.Should().BeEmpty();
        }

        [Test]
        public void Build_SendsFirstNameOnly_NeverFullNameOrEmail()
        {
            // Privacy: the prompt greets the candidate by first name. Surname, username and
            // email have no coaching value and are never handed to the model.
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);

            var snapshot = Build(new[] { SubmittedOn(exam, attemptId: 1) }, fullName: "Diya Sharma");

            snapshot.FirstName.Should().Be("Diya");
        }

        [Test]
        public void Build_BlankFullName_YieldsEmptyFirstName()
        {
            var exam = TestDataBuilder.BuildResultsPublishedExam(examId: 1);

            var snapshot = Build(new[] { SubmittedOn(exam, attemptId: 1) }, fullName: "   ");

            snapshot.FirstName.Should().BeEmpty();
        }
    }
}
