using System;
using System.Collections.Generic;
using System.Linq;
using Decidehub.Core.Attributes;

namespace Decidehub.Core.Extensions
{
    public static class EnumExtensions
    {
        public static string DescriptionLang(this Enum @enum, string lang)
        {
            var description = string.Empty;
            var fields = @enum.GetType().GetFields();
            foreach (var field in fields)
            {
                var descriptionAttributes =
                    Attribute.GetCustomAttributes(field, typeof(DescriptionLangAttribute)) as
                        IEnumerable<DescriptionLangAttribute>;

                var descriptionAttribute =
                    descriptionAttributes?.FirstOrDefault(x => x.Lang == lang);
                if (descriptionAttribute == null ||
                    !field.Name.Equals(@enum.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    continue;
                description = descriptionAttribute.Description;
                break;
            }

            return description;
        }
    }
}