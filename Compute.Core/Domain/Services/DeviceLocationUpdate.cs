namespace Compute.Core.Domain.Services
{
    public class DeviceLocationUpdate(
        double latitude,
        double longitude,
        double? speed,
        double? course,
        double? accuracy,
        double? verticalAccuracy,
        double? altitude) : EventArgs
    {
        public double Latitude { get; } = latitude;
        public double Longitude { get; } = longitude;
        public double? Speed { get; } = speed;
        public double? Course { get; } = course;
        public double? Accuracy { get; } = accuracy;
        public double? VerticalAccuracy { get; } = verticalAccuracy;
        public double? Altitude { get; } = altitude;
    }

    public class DeviceLocationListeningFailure(string reason) : EventArgs
    {
        public string Reason { get; } = reason;
    }
}
