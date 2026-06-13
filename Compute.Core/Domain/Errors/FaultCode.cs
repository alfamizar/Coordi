namespace Compute.Core.Domain.Errors
{
    public enum FaultCode
    {
        FeatureNotSupported,
        FeatureNotEnabled,
        PermissionException,
        GenericGetLocationException,
        DeviceLocationUnavailable,
        CouldNotStartListeningDeciveGeoLocation,
        CouldNotStopListeningDeciveGeoLocation
    }
}