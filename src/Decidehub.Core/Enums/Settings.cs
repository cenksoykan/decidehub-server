using System.ComponentModel;

namespace Decidehub.Core.Enums
{
    public enum Settings
    {
        [Description("Yetki dağılımı oylaması tekrarlanma sıklığı")]
        VotingFrequency = 4,

        [Description("Oylamaların geçerli sayılabilmeleri için gerekli minimum yetki katılım oranı")]
        AuthorityVotingRequiredUserPercentage = 3,
        
        [Description("Oylama Süresi")] VotingDuration = 6

    }
}