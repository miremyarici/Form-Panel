using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoProject.Models
{
    [Table("Reference")]
    public class Reference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ReferenceType { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string ReferenceKey { get; set; } = "";

        [Required]
        [StringLength(255)]
        public string ReferenceValue { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}