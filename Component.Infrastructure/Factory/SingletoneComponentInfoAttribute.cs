namespace Component.Infrastructure.Factory
{
    public class SingletoneComponentInfoAttribute : ComponentInfoAttribute
    {
        public SingletoneComponentInfoAttribute(string version, bool isEnabled = true)
            : base(InstanceModeEnum.Singletone, version, isEnabled)
        {
        }
    }
}