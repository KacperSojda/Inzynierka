using System.ComponentModel.DataAnnotations;

namespace INZYNIERKA.ViewModels
{
    public class TagViewModel
    {
        [Required(ErrorMessage = "Tag name is required")]
        [Display(Name = "Tag name")]
        public string TagName { get; set; }
    }
}
