namespace CBT_Task.Models
{
    public class MigrationRecord
    {
        public int Id { get; set; }
        public string OldSystemUserId { get; set; } = string.Empty;
        public int NewSystemUserId { get; set; }
        public DateTime MigratedAt { get; set; } = DateTime.UtcNow;
    }
}
