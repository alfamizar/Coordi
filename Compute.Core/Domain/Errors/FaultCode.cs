namespace Compute.Core.Domain.Errors
{
    public enum FaultCode
    {
        // Get current location
        FeatureNotSupported,
        FeatureNotEnabled,
        PermissionException,
        GenericGetLocationException,
        DeviceLocationUnavailable,

        // Location listening
        CouldNotStartListeningDeciveGeoLocation,
        CouldNotStopListeningDeciveGeoLocation
    }
}