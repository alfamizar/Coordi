namespace Compute.Core.Domain.Errors
{
    public enum FaultCode
    {
        // Get current location
        FeatureNotSupported = 0,
        FeatureNotEnabled = 1,
        PermissionException = 2,
        GenericGetLocationException = 3,
    }
}