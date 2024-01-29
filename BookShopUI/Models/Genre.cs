using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookShopUI.Models
{
    [Table("Genre")]
    public class Genre
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        public string GenreName { get; set; }
    }
}
