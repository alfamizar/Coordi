namespace Compute.Core.Domain.Services
{
    public class DeviceLocationUpdate(
        double latitude,
        double longitude,
        double? speed,
        double? course,
        double? accuracy,
        double? verticalAccuracy,
        double? altitude,
        DateTimeOffset timestamp) : EventArgs
    {
        public double Latitude { get; } = latitude;
        public double Longitude { get; } = longitude;
        public double? Speed { get; } = speed;
        public double? Course { get; } = course;
        public double? Accuracy { get; } = accuracy;
        public double? VerticalAccuracy { get; } = verticalAccuracy;
        public double? Altitude { get; } = altitude;

        /// <summary>
        /// When the OS actually fixed this position. Speed has to be derived from these, not from
        /// when the callback happened to reach us — a stalled main thread would otherwise stretch
        /// the interval and understate the speed.
        /// </summary>
        public DateTimeOffset Timestamp { get; } = timestamp;
    }

    public class DeviceLocationListeningFailure(string reason) : EventArgs
    {
        public string Reason { get; } = reason;
    }
}
