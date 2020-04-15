using Decidehub.Core.Attributes;

namespace Decidehub.Core.Enums
{
    public enum PollResults
    {
        [DescriptionLang("Completed", "en")] [DescriptionLang("Tamamlanmış", "tr")]
        Completed = 1,

        [DescriptionLang("Undecided", "en")] [DescriptionLang("Kararsız", "tr")]
        Undecided = 2,

        [DescriptionLang("Positive", "tr")] [DescriptionLang("Olumlu", "tr")]
        Positive = 3,

        [DescriptionLang("Negative", "en")] [DescriptionLang("Olumsuz", "tr")]
        Negative = 4,

        [DescriptionLang("Insufficient Authority", "en")] [DescriptionLang("Yetersiz yetki", "tr")]
        InsufficientAuthority = 5,

        [DescriptionLang("Due to insufficient participation, authority scores didn't change", "en")]
        [DescriptionLang("Yetersiz katılım sebebiyle, yetki puanları değişmemiştir.", "tr")]
        InsufficientParticipation = 6
    }
}