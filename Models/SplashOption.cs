using SQLite;

namespace parserColorBackground.Models
{
    [Table("SplashOptions")]
    public class SplashOption
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100)]
        public string SplashName { get; set; }

        public string ImageUrl { get; set; }

        public bool IsActive { get; set; }
    }
}