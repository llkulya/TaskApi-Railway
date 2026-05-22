namespace TaskApi.Utilities
{
    public static class MaskingHelper
    {
        public static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "unknown";

            var parts = email.Split('@');
            if (parts.Length != 2)
                return "***";

            var name = parts[0];
            var domain = parts[1];

            if (name.Length <= 2)
                return $"***@{domain}";

            return $"{name[0]}***{name[^1]}@{domain}";
        }

        public static string MaskToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return "empty";

            if (token.Length <= 10)
                return "***";

            return $"{token[..4]}***{token[^4..]}";
        }

        public static string MaskApiKey(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "empty";

            if (apiKey.Length <= 8)
                return "***";

            return $"{apiKey[..4]}***{apiKey[^4..]}";
        }
    }
}