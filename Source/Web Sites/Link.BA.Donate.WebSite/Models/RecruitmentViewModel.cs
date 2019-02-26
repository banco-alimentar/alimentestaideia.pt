using System.ComponentModel.DataAnnotations;
using System.Web;
using Link.BA.Donate.Business;

namespace Link.BA.Donate.WebSite.Models
{
    public class RecruitmentViewModel
    {
        [Required(ErrorMessage = "O {0} deve ser preenchido.")]
        [StringLength(120, ErrorMessage = "O Tamanho máximo são {1} caracteres.")]
        [DisplayAttribute(Name = "Nome")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O {0} deve ser preenchido.")]
        [StringLength(120, ErrorMessage = "O Tamanho máximo do {0} são {1} caracteres.")]
        [RegularExpression(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", ErrorMessage = "{0} inválido")]
        [DisplayAttribute(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O {0} deve ser preenchido.")]
        [StringLength(120, ErrorMessage = "O Tamanho máximo são {1} caracteres.")]
        [DisplayAttribute(Name = "Telefone")]
        public string Phone { get; set; }
    }
}