using System.Security.Cryptography;

namespace ExamPlatform.BLL.Services
{
    // Shared by ExaminerSubscriptionService and CandidatePurchaseService — the
    // fake identifiers a Payment row carries. Never real: no gateway is ever
    // contacted.
    public static class PaymentReferenceGenerator
    {
        private const string AlnumChars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

        public static string TransactionId(DateTime now)
        {
            return $"TXN-{now:yyyyMMddHHmmss}-{RandomChars(6)}";
        }

        public static string GatewayReference()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            return $"GTW-{Convert.ToHexString(bytes)}";
        }

        private static string RandomChars(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return new string(bytes.Select(b => AlnumChars[b % AlnumChars.Length]).ToArray());
        }
    }
}
