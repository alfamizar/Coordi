namespace Compute.Core.Domain.Entities.Models.Time
{
    public class TimeModel
    {
        public int OffsetHours { get; private set; }
        public DateTime CurrentTime { get; private set; }

        public TimeModel(int offsetHours = 0)
        {
            OffsetHours = offsetHours;
            UpdateTime();
        }

        public void UpdateTime()
        {
            CurrentTime = DateTime.UtcNow.AddHours(OffsetHours);
        }
    }
}
