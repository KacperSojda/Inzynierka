using System.ComponentModel.DataAnnotations;

namespace INZYNIERKA.ViewModels
{
    public class TagViewModel
    {
        private string _tagName;

        [Required(ErrorMessage = "Tag name is required")]
        [StringLength(15, MinimumLength = 2, ErrorMessage = "Tag must be between 2 and 30 characters long.")]
        [Display(Name = "Tag name")]
        public string TagName
        {
            get => _tagName;
            set => _tagName = value?.Trim();
        }
    }
}
