namespace TaskApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class NoInterceptAttribute : Attribute
    {
    }
}