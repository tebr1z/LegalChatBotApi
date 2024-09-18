using Microsoft.AspNetCore.Identity;

namespace HuquqApi.Model
{
    public class User : IdentityUser
    {
        public bool IsPremium { get; set; }
        public DateTime? PremiumExpirationDate { get; set; }
        public int RequestCount { get; set; } 

        public int MonthlyQuestionCount { get; set; }
        public DateTime LastQuestionDate { get; set; }
    }

}
