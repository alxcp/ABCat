namespace Component.Infrastructure.Factory
{
    public class PerCallComponentInfoAttribute : ComponentInfoAttribute
    {
        public PerCallComponentInfoAttribute(string version, bool isEnabled = true)
            : base(InstanceModeEnum.PerCall, version, isEnabled)
        {
        }
    }
}