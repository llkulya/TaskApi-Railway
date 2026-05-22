namespace TaskApi.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SensitiveDataAttribute : Attribute
    {
    }
}