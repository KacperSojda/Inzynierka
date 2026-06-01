using System.ComponentModel.DataAnnotations;

namespace INZYNIERKA.Services.ViewModels
{
    public class EditGroupViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa grupy jest wymagana.")]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
