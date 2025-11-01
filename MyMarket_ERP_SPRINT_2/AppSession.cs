namespace MyMarket_ERP
{
    public static class AppSession
    {
        public static string Role { get; private set; } = string.Empty;
        public static string UserEmail { get; private set; } = string.Empty;
        public static int? CustomerId { get; private set; }

        public static bool HasActiveSession =>
            !string.IsNullOrWhiteSpace(Role) && !string.IsNullOrWhiteSpace(UserEmail);

        public static void StartSession(string email, string role, int? customerId = null)
        {
            UserEmail = email ?? string.Empty;
            Role = role ?? string.Empty;
            CustomerId = customerId;
        }

        public static void Clear()
        {
            Role = string.Empty;
            UserEmail = string.Empty;
            CustomerId = null;
        }
    }
}
