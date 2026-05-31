using System.ComponentModel;

namespace SSMS_AI_Buddy.Models
{
    public class AIProviderTypeConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            => new StandardValuesCollection(AIProviderOptions.All);
    }
}
