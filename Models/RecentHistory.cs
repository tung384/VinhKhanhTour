using SQLite;

namespace OneSProject.Models;

public class RecentHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int POIId { get; set; }
    public DateTime VisitedDate { get; set; }
}