using SQLite;

namespace parserColorBackground.Models
{
    [Table("ColorOptions")]
    public class ColorOption
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(50)]
        public string ColorName { get; set; }

        public bool IsActive { get; set; }
    }
}