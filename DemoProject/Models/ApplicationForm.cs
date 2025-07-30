using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoProject.Models
{
    [Table("ApplicationForm")]
    public class ApplicationForm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string projectName { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string applicantUnit { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string appliedProject { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string appliedType { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string participantType { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string applicationPeriod { get; set; } = "";

        [Required]
        public DateTime applicationDate { get; set; }

        [Required]
        [StringLength(50)]
        public string applicationState { get; set; } = "";

        [Required]
        public DateTime stateDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? grantAmount { get; set; }

    }
}
