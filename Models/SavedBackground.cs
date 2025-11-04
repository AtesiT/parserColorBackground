using SQLite;

namespace parserColorBackground.Models
{
    [Table("SavedBackgrounds")]
    public class SavedBackground
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ImageUrl { get; set; }

        public string ColorName { get; set; }

        public bool IsCurrent { get; set; }

        public DateTime SavedAt { get; set; }
    }
}