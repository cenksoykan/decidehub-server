using System;
using System.ComponentModel;

namespace Decidehub.Core.Attributes
{

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class DescriptionLangAttribute : DescriptionAttribute
    {
        public string Lang { get; set; }
        public DescriptionLangAttribute( string description, string lang) : base(description)
        {
            DescriptionValue = description;
            Lang = lang;
        }

      
    }

  
}
