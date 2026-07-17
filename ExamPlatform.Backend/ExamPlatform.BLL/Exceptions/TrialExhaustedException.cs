namespace ExamPlatform.BLL.Exceptions
{
    // Thrown when an examiner on Trial/FreeForever tries to create an exam
    // beyond their lifetime cap. Carries the state the controller needs to
    // tell the frontend which dialog to render (initial upgrade offer, the
    // one-time 25%-off retention offer, or "already on Free Forever").
    // Maps to HTTP 402 Payment Required.
    public class TrialExhaustedException : BusinessException
    {
        public int UpgradeDeclineCount { get; }
        public int TrialExamsUsed { get; }
        public int MaxExamsLifetime { get; }

        public TrialExhaustedException(int upgradeDeclineCount, int trialExamsUsed, int maxExamsLifetime)
            : base($"You've created {trialExamsUsed} exams on your free trial — you've experienced the platform! " +
                   "Upgrade to Professional to keep creating exams.")
        {
            UpgradeDeclineCount = upgradeDeclineCount;
            TrialExamsUsed = trialExamsUsed;
            MaxExamsLifetime = maxExamsLifetime;
        }
    }
}
