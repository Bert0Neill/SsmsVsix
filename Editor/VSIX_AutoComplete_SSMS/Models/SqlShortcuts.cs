using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace VSIX_AutoComplete_SSMS.Models
{
    public class SqlShortcuts
    {
        [Description("SQL Shortcut")]
        [Required(ErrorMessage = "SQL Shortcut is required and cannot be empty")]
        public string SqlShort { get; set; }

        [Description("Expanded SQL")]
        [Required(ErrorMessage = "SQL Complete is required and cannot be empty")]
        public string SqlComplete { get; set; }

        // Method to validate the instance
        public bool Validate(out ICollection<ValidationResult> validationResults)
        {
            validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, validationResults, true);
        }

        // Helper method to return validation messages
        public IEnumerable<string> GetValidationErrors()
        {
            ICollection<ValidationResult> results;
            Validate(out results);
            return results.Select(r => r.ErrorMessage);
        }

        // Simple validation that returns true/false
        public bool IsValid()
        {
            ICollection<ValidationResult> results;
            return Validate(out results);
        }
    }
}
