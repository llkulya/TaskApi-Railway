namespace TaskApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class MeasureTimeAttribute : Attribute
    {
        public int WarningThresholdMs { get; set; } = 1000;
        public int ErrorThresholdMs { get; set; } = 5000;
    }
}