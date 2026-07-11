namespace ExamPlatform.BLL.Services
{
    // Static factory for HTML email bodies.
    // Each method returns a self-contained HTML document.
    // Styles are intentionally inlined — most email clients strip <style> blocks.
    internal static class EmailTemplates
    {
        private const string Blue   = "#2563eb";
        private const string Green  = "#16a34a";
        private const string Red    = "#dc2626";
        private const string Purple = "#7c3aed";
        private const string Footer = "<div style=\"background:#f9fafb;padding:16px 32px;text-align:center;\"><p style=\"color:#9ca3af;font-size:12px;margin:0;\">This is an automated notification from Exam Platform. Please do not reply to this email.</p></div>";

        // ════════════════════════════════════════════════════════════════
        //   RESULT PUBLISHED  (sent to all candidates — pass and fail)
        // ════════════════════════════════════════════════════════════════
        public static string ResultPublished(
            string candidateName,
            string examTitle,
            decimal score,
            decimal totalMarks,
            bool passed)
        {
            var badgeColor  = passed ? Green : Red;
            var badgeLabel  = passed ? "PASSED ✓" : "NOT PASSED";
            var scoreDetail = passed
                ? $"Congratulations! You passed with a score of <strong>{score} / {totalMarks}</strong>."
                : $"Your score was <strong>{score} / {totalMarks}</strong>. You did not meet the passing mark for this exam.";

            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="UTF-8" /><title>Exam Results Published</title></head>
                <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:24px;">
                  <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1);">
                    <div style="background:{Blue};padding:28px 32px;">
                      <h1 style="color:#fff;margin:0;font-size:22px;">Exam Platform</h1>
                      <p  style="color:#bfdbfe;margin:6px 0 0;font-size:14px;">Results have been published</p>
                    </div>
                    <div style="padding:32px;">
                      <p style="color:#374151;font-size:16px;">Dear <strong>{candidateName}</strong>,</p>
                      <p style="color:#374151;">Results for the following exam have been published:</p>
                      <div style="background:#f0f9ff;border-left:4px solid {Blue};padding:16px;margin:20px 0;border-radius:4px;">
                        <p style="margin:0;font-size:18px;font-weight:bold;color:#1e3a5f;">{examTitle}</p>
                      </div>
                      <div style="text-align:center;margin:24px 0;">
                        <span style="display:inline-block;background:{badgeColor};color:#fff;padding:8px 24px;border-radius:20px;font-size:15px;font-weight:bold;letter-spacing:1px;">{badgeLabel}</span>
                      </div>
                      <p style="color:#374151;text-align:center;">{scoreDetail}</p>
                      <p style="color:#6b7280;font-size:14px;">Log in to the Exam Platform dashboard to view your detailed results and interactive performance chart.</p>
                    </div>
                    {Footer}
                  </div>
                </body>
                </html>
                """;
        }

        // ════════════════════════════════════════════════════════════════
        //   CONGRATULATIONS — YOU PASSED  (sent only to passing candidates)
        // ════════════════════════════════════════════════════════════════
        public static string YouPassed(
            string candidateName,
            string examTitle,
            decimal score,
            decimal totalMarks)
        {
            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="UTF-8" /><title>Congratulations — You Passed!</title></head>
                <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:24px;">
                  <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1);">
                    <div style="background:{Green};padding:28px 32px;">
                      <h1 style="color:#fff;margin:0;font-size:22px;">Congratulations!</h1>
                      <p  style="color:#bbf7d0;margin:6px 0 0;font-size:14px;">You have successfully passed the exam</p>
                    </div>
                    <div style="padding:32px;">
                      <p style="color:#374151;font-size:16px;">Dear <strong>{candidateName}</strong>,</p>
                      <p style="color:#374151;">We are pleased to inform you that you have <strong>passed</strong> the following exam:</p>
                      <div style="background:#f0fdf4;border-left:4px solid {Green};padding:16px;margin:20px 0;border-radius:4px;">
                        <p style="margin:0 0 8px;font-size:18px;font-weight:bold;color:#14532d;">{examTitle}</p>
                        <p style="margin:0;color:#166534;font-size:15px;">Score: <strong>{score} / {totalMarks}</strong></p>
                      </div>
                      <p style="color:#374151;">Your certificate is being issued and you will receive a separate notification once it is available for download.</p>
                      <p style="color:#6b7280;font-size:14px;">Keep up the great work!</p>
                    </div>
                    {Footer}
                  </div>
                </body>
                </html>
                """;
        }

        // ════════════════════════════════════════════════════════════════
        //   CERTIFICATE AVAILABLE  (sent when a certificate is issued)
        // ════════════════════════════════════════════════════════════════
        public static string CertificateAvailable(
            string candidateName,
            string examTitle,
            string certificateCode)
        {
            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="UTF-8" /><title>Your Certificate is Ready</title></head>
                <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:24px;">
                  <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1);">
                    <div style="background:{Purple};padding:28px 32px;">
                      <h1 style="color:#fff;margin:0;font-size:22px;">Certificate Available</h1>
                      <p  style="color:#ddd6fe;margin:6px 0 0;font-size:14px;">Your certificate is ready for download</p>
                    </div>
                    <div style="padding:32px;">
                      <p style="color:#374151;font-size:16px;">Dear <strong>{candidateName}</strong>,</p>
                      <p style="color:#374151;">Your certificate for the following exam has been issued and is now available:</p>
                      <div style="background:#faf5ff;border-left:4px solid {Purple};padding:16px;margin:20px 0;border-radius:4px;">
                        <p style="margin:0 0 8px;font-size:18px;font-weight:bold;color:#4c1d95;">{examTitle}</p>
                        <p style="margin:0;color:#6d28d9;font-size:14px;">Certificate Code: <strong>{certificateCode}</strong></p>
                      </div>
                      <p style="color:#374151;">Log in to the Exam Platform to download and share your certificate.</p>
                    </div>
                    {Footer}
                  </div>
                </body>
                </html>
                """;
        }

        // ════════════════════════════════════════════════════════════════
        //   NEW EXAM AVAILABLE  (sent to all active candidates on publish)
        // ════════════════════════════════════════════════════════════════
        public static string NewExamAvailable(
            string candidateName,
            string examTitle,
            DateTime startTime,
            DateTime endTime,
            int durationMinutes,
            decimal totalMarks,
            decimal passingMarks)
        {
            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="UTF-8" /><title>New Exam Available</title></head>
                <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:24px;">
                  <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1);">
                    <div style="background:{Blue};padding:28px 32px;">
                      <h1 style="color:#fff;margin:0;font-size:22px;">New Exam Available</h1>
                      <p  style="color:#bfdbfe;margin:6px 0 0;font-size:14px;">A new exam has just been published</p>
                    </div>
                    <div style="padding:32px;">
                      <p style="color:#374151;font-size:16px;">Dear <strong>{candidateName}</strong>,</p>
                      <p style="color:#374151;">A new exam is now open for attempts:</p>
                      <div style="background:#f0f9ff;border-left:4px solid {Blue};padding:16px;margin:20px 0;border-radius:4px;">
                        <p style="margin:0 0 10px;font-size:18px;font-weight:bold;color:#1e3a5f;">{examTitle}</p>
                        <p style="margin:0 0 4px;color:#374151;font-size:14px;">Available: <strong>{startTime:u}</strong> — <strong>{endTime:u}</strong></p>
                        <p style="margin:0 0 4px;color:#374151;font-size:14px;">Duration: <strong>{durationMinutes} minutes</strong></p>
                        <p style="margin:0;color:#374151;font-size:14px;">Total marks: <strong>{totalMarks}</strong> &nbsp;|&nbsp; Passing marks: <strong>{passingMarks}</strong></p>
                      </div>
                      <p style="color:#374151;">Log in to the Exam Platform and open <strong>Browse Exams</strong> to take it. Remember: starting before the end time gives you the full duration.</p>
                    </div>
                    {Footer}
                  </div>
                </body>
                </html>
                """;
        }

        // ════════════════════════════════════════════════════════════════
        //   CERTIFICATE REVOKED  (sent when an admin revokes a certificate)
        // ════════════════════════════════════════════════════════════════
        public static string CertificateRevoked(string candidateName, string examTitle)
        {
            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="UTF-8" /><title>Certificate Revoked</title></head>
                <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:24px;">
                  <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1);">
                    <div style="background:{Red};padding:28px 32px;">
                      <h1 style="color:#fff;margin:0;font-size:22px;">Certificate Revoked</h1>
                      <p  style="color:#fecaca;margin:6px 0 0;font-size:14px;">Important notice regarding your certification</p>
                    </div>
                    <div style="padding:32px;">
                      <p style="color:#374151;font-size:16px;">Dear <strong>{candidateName}</strong>,</p>
                      <p style="color:#374151;">We regret to inform you that your certificate for the following exam has been <strong>revoked</strong> by an administrator:</p>
                      <div style="background:#fff1f2;border-left:4px solid {Red};padding:16px;margin:20px 0;border-radius:4px;">
                        <p style="margin:0;font-size:18px;font-weight:bold;color:#7f1d1d;">{examTitle}</p>
                      </div>
                      <p style="color:#374151;">If you believe this is an error, please contact your platform administrator.</p>
                    </div>
                    {Footer}
                  </div>
                </body>
                </html>
                """;
        }
    }
}
