namespace TaskApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public int DurationSeconds { get; set; } = 60;
    }
}