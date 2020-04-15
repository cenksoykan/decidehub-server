using Decidehub.Core.Attributes;

namespace Decidehub.Core.Enums
{
    public enum PollTypes
    {
        [DescriptionLang("Authority Distribution Poll","en")]
        [DescriptionLang("Yetki Dağılımı Oylaması", "tr")]
        AuthorityPoll = 1,
        
        [DescriptionLang("Manager Election Poll", "en")]
        [DescriptionLang("Yönetici Seçim Oylaması", "tr")]
        MultipleChoicePoll = 2,
     
        [DescriptionLang("Policy Change Poll", "en")]
        [DescriptionLang("Yönetmelik Değişiklik Oylaması", "tr")]
        PolicyChangePoll = 3,
        
        [DescriptionLang("Share Rate Poll", "en")]
        [DescriptionLang("Paylaşım Oylaması", "tr")]
        SharePoll = 4
    }
}