namespace Compute.Core.Domain.Errors
{
    public enum FaultCode
    {
        FeatureNotSupported,
        FeatureNotEnabled,
        PermissionException,
        GenericGetLocationException,
        DeviceLocationUnavailable,
        CouldNotStartListeningDeviceGeoLocation,
        CouldNotStopListeningDeviceGeoLocation
    }
}