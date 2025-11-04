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

        // ImageUrl теперь необязательное поле - будет парситься автоматически
        public string ImageUrl { get; set; }

        public bool IsActive { get; set; }
    }
}