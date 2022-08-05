using Api.AutoForms;
using Api.Permissions;

namespace Api.Users

{
    /// <summary>
    /// A User
    /// </summary>
    
    public partial class User
    {
        /// <summary>
        /// Has this user been checked via a captcha 
        /// </summary>
        [Data("hint", "Has this user been confirmed via captcha")]
        [Permissions(HideFieldByDefault = false)]
        public bool CanVote;

    }
}