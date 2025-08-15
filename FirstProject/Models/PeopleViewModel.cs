using FirstProject.Models;

namespace FirstProject.Models
{
    public class PeopleViewModel
    {
        public List<PersonViewModel> People { get; set; } = new();
    }

    public class PersonViewModel
    {
        public bool IsSelected { get; set; }
        public Person Person { get; set; } = new Person
        {
            Forename = string.Empty,
            FamilyName = string.Empty,
            Gender = string.Empty,
            YearOfBirth = 0
        };
    }
}