using SQLite;

namespace JustCompute.Persistence.Repository.Models.Base
{
    public class BaseTable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }
}
