namespace Compute.Core.Utils
{
    public static class DistanceUtils
    {
        public static double GetDistanceOnSphereByHaversineFormula(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // Earth's radius in meters
            double radLat1 = ToRadians(lat1);
            double radLat2 = ToRadians(lat2);
            double deltaLat = ToRadians(lat2 - lat1);
            double deltaLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(radLat1) * Math.Cos(radLat2) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}