namespace HRService.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RetryAttribute : Attribute
    {
        public int MaxAttempts { get; }

        public int DelayMilliseconds { get; }

        public RetryAttribute(int maxAttempts = 3, int delayMilliseconds = 1000)
        {
            MaxAttempts = maxAttempts;
            DelayMilliseconds = delayMilliseconds;
        }
    }
}