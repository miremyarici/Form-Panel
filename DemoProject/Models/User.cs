using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoProject.Models
{
    [Table("User")]
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string? KullaniciAdi { get; set; }

        [Required(ErrorMessage = "E-Posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
        public string? EPosta { get; set; }

        [Required(ErrorMessage = "Parola zorunludur.")]
        [MinLength(6, ErrorMessage = "Parola en az 6 karakter olmalıdır.")]
        public string? Parola { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Role { get; set; } = "Admin";
    }
}
