namespace Compute.Core.Common.Exceptions.Location
{
    public class DeviceLocationUnavailableException : Exception
    {
        public DeviceLocationUnavailableException()
        {
        }

        public DeviceLocationUnavailableException(string message)
            : base(message)
        {
        }

        public DeviceLocationUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
