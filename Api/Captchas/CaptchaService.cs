using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Api.Users;
using Google.Protobuf.Reflection;

namespace Api.Captchas
{
    /// <summary>
    /// Handles captchas.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class CaptchaService : AutoService<Captcha>
    {
        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public CaptchaService() : base(Events.Captcha)
        {
            // Example admin page install:
            InstallAdminPages("Captchas", "fa:fa-user-alt-slash", new string[] { "id", "description" });

            //// hide the can vote field for external user updates
            Events.User.BeforeSettable.AddEventListener((Context ctx, JsonField<User, uint> field) =>
            {
                if (field == null)
                {
                    return new ValueTask<JsonField<User, uint>>(field);
                }
                if (field.ForRole != Roles.Admin && field.ForRole != Roles.Developer && field.Name == "CanVote")
                {
                    // Not settable by anyone apart from admin, only the api. This hides it from the admin panel as well
                    field = null;
                }
                return new ValueTask<JsonField<User, uint>>(field);
            });

            // do not expose the expected tag with the other captcha data 
            Events.Captcha.BeforeGettable.AddEventListener((Context ctx, JsonField<Captcha, uint> field) =>
            {
                if (field == null)
                {
                    return new ValueTask<JsonField<Captcha, uint>>(field);
                }
                if (field.Name == "ExpectedTag" && !field.ForRole.CanViewAdmin)
                {
                    // only admin can see this value externally
                    field = null;
                }
                return new ValueTask<JsonField<Captcha, uint>>(field);
            });
        }

        /// <summary>
        /// Get a random active captcha
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async ValueTask<Captcha> Random(Context context)
        {
            var captcha = await Where("IsActive=?", DataOptions.IgnorePermissions)
                .Bind(true)
                .ListAll(context);

            if (captcha == null || !captcha.Any())
            {
                // return blank entry so that we know the request 'worked' 
                return new Captcha();
            }

            var rnd = new Random();
            int index = rnd.Next(captcha.Count);

            return captcha[index];
        }

        /// <summary>
        /// Check a captcha response and update the user 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="captchaId"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async ValueTask<bool> Check(Context context, uint captchaId, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            var captcha = await Get(context, captchaId);

            if (captcha == null)
            {
                return false;
            }

            var success  =  captcha.ExpectedTag.Trim().ToLower() == tag.Trim().ToLower();

            if(success)
            {
                await Services.Get<UserService>().Update(context, context.User, (Context ctx, User usr, User orig) => {
                    usr.CanVote = true;
                }, DataOptions.IgnorePermissions);
            }

            return success;
        }
    }
}
