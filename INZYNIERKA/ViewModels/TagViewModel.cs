using System.ComponentModel.DataAnnotations;

namespace INZYNIERKA.ViewModels
{
    public class TagViewModel
    {
        [Required(ErrorMessage = "Nazwa tagu jest wymagana")]
        [Display(Name = "Nazwa tagu")]
        public string TagName { get; set; }
    }
}
