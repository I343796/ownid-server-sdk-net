using System.ComponentModel.DataAnnotations;
using OwnID.Attributes;
using OwnID.Extensibility.Configuration.Profile;

namespace OwnID.Integrations.Gigya
{
    public class GigyaUserProfile : IGigyaUserProfile
    {
        [OwnIdField(Constants.DefaultEmailLabel, Constants.DefaultEmailLabel)]
        [OwnIdFieldType(ProfileFieldType.Email)]
        [Required]
        [MaxLength(200)]
        public string Email { get; set; }

        [OwnIdField(Constants.DefaultFirstNameLabel, Constants.DefaultFirstNameLabel)]
        [MaxLength(200)]
        public string FirstName { get; set; }

        [OwnIdField(Constants.DefaultLastNameLabel, Constants.DefaultLastNameLabel)]
        [MaxLength(200)]
        public string LastName { get; set; }
    }
}