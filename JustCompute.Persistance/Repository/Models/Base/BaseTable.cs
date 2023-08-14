using SQLite;

namespace JustCompute.Persistance.Repository.Models.Base
{
    public class BaseTable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }
}
