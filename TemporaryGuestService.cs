using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Api.PasswordAuth;
using Api.Permissions;
using Api.Startup;
using Api.Users;

namespace Api.TemporaryGuests
{
    /// <summary>
    /// Service to handle temporary guests
    /// </summary>
    public partial class TemporaryGuestService : AutoService<TemporaryGuest>
    {
        public TemporaryGuestService()
            : base(Events.TemporaryGuest)
        {
            // Example admin page install:
            InstallAdminPages("TemporaryGuests", "fa:fa-gavel", new string[] { "id", "email" , "startsutc" });

            Events.TemporaryGuest.BeforeSettable.AddEventListener((Context ctx, JsonField<TemporaryGuest> field) => {

                if (field == null)
                {
                    return new ValueTask<JsonField<TemporaryGuest>>(field);
                }

                return new ValueTask<JsonField<TemporaryGuest>>((field.ForRole == Roles.Admin || field.ForRole == Roles.Developer) ? field : null);
            });

            Events.TemporaryGuest.BeforeCreate.AddEventListener(
                (Context ctx, TemporaryGuest guest) =>
                {
                    //Create a hash
                    guest.Token = PasswordStorage.CreateHash(guest.Email);

                    return new ValueTask<TemporaryGuest>(guest);
                });

            Events.TemporaryGuest.BeforeUpdate.AddEventListener(
                (Context ctx, TemporaryGuest guest) =>
                {
                    //Update the token
                    guest.Token = PasswordStorage.CreateHash(guest.Email);

                    return new ValueTask<TemporaryGuest>(guest);
                });
        }

        public async Task<LoginResult> Login(Context context, TempLogin loginInfo)
        {
            var verified = PasswordStorage.VerifyPassword(loginInfo.Email , loginInfo.Token);

            if (verified)
            {
                var filter = new Filter<TemporaryGuest>();
                filter.EqualsField("Email", loginInfo.Email);
                
                var tempUser = (await List(context, filter, DataOptions.IgnorePermissions)).FirstOrDefault();

                if (tempUser != null && tempUser.ExpiresUtc > DateTime.UtcNow && tempUser.StartsUtc < DateTime.UtcNow)
                {
                    var userService = Services.Get<UserService>();

                    var user = await userService.GetByEmail(context, loginInfo.Email);
                    var password = PasswordStorage.CreateHash(loginInfo.Token);

                    if (user == null)
                    {
                        user = await userService.Create(context, new User() {Email = tempUser.Email, FullName = tempUser.FirstName + " " + tempUser.LastName, FirstName = tempUser.FirstName, LastName = tempUser.LastName, PasswordHash = password }, DataOptions.IgnorePermissions);
                    }

                    var result = await userService.Authenticate(context, new UserLogin() {EmailOrUsername = tempUser.Email, Password = loginInfo.Token });

                    if (result != null && result.Success)
                    {
                        return result;
                    }
                }
            }

            return new LoginResult(){Success = false};
        }
    }
}
 