﻿using aehyok.IdentityServer.Helper;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace aehyok.IdentityServer.Controllers
{
    public class AccountController: BaseController
    {
        private readonly TestUserStore _users;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly AccountService _account;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            TestUserStore users = null)
        {
            _users = new TestUserStore(TestUsers.Users); //初始化测试数据（可更改为数据库）
            _interaction = interaction;
            _account = new AccountService(interaction, httpContextAccessor, clientStore);
        }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var vm = await _account.BuildLoginViewModelAsync(returnUrl);
            return View(vm);
        }

        /// <summary>
        /// 登录验证
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 验证用户和密码
                if (_users.ValidateCredentials(model.Username, model.Password))
                {
                    AuthenticationProperties props = null;
                    
                    //此处可通过配置设置是否可记住密码，并配合前台进行勾选是否记住密码
                    if (model.RememberLogin)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,  //是否为持久性
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(30))  //设置超期时间（30天）
                        };
                    };

                    //通过用户名找寻用户信息
                    var user = _users.FindByUsername(model.Username);
                    //http://www.cnblogs.com/xishuai/p/aspnet-5-identity-part-one.html
                    await HttpContext.Authentication.SignInAsync(user.SubjectId, user.Username, props);

                    //验证url是否有效
                    if (_interaction.IsValidReturnUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return Redirect("~/");
                }

                ModelState.AddModelError("", "无效的用户名和密码！");
            }

            // something went wrong, show form with error
            var vm = await _account.BuildLoginViewModelAsync(model);
            return View(vm);
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            var vm = await _account.BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // no need to show prompt
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutViewModel model)
        {
            var vm = await _account.BuildLoggedOutViewModelAsync(model.LogoutId);
            if (vm.TriggerExternalSignout)
            {
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });
                try
                {
                    // hack: try/catch to handle social providers that throw
                    await HttpContext.Authentication.SignOutAsync(vm.ExternalAuthenticationScheme,
                        new AuthenticationProperties { RedirectUri = url });
                }
                catch (NotSupportedException) // this is for the external providers that don't have signout
                {
                }
                catch (InvalidOperationException) // this is for Windows/Negotiate
                {
                }
            }

            // delete local authentication cookie
            await HttpContext.Authentication.SignOutAsync();

            return View("LoggedOut", vm);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        //[HttpGet]
        //public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
        //{
        //    returnUrl = Url.Action("ExternalLoginCallback", new { returnUrl = returnUrl });

        //    // windows authentication is modeled as external in the asp.net core authentication manager, so we need special handling
        //    if (AccountOptions.WindowsAuthenticationSchemes.Contains(provider))
        //    {
        //        // but they don't support the redirect uri, so this URL is re-triggered when we call challenge
        //        if (HttpContext.User is WindowsPrincipal)
        //        {
        //            var props = new AuthenticationProperties();
        //            props.Items.Add("scheme", HttpContext.User.Identity.AuthenticationType);

        //            var id = new ClaimsIdentity(provider);
        //            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, HttpContext.User.Identity.Name));
        //            id.AddClaim(new Claim(ClaimTypes.Name, HttpContext.User.Identity.Name));

        //            await HttpContext.Authentication.SignInAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme, new ClaimsPrincipal(id), props);
        //            return Redirect(returnUrl);
        //        }
        //        else
        //        {
        //            // this triggers all of the windows auth schemes we're supporting so the browser can use what it supports
        //            return new ChallengeResult(AccountOptions.WindowsAuthenticationSchemes);
        //        }
        //    }
        //    else
        //    {
        //        // start challenge and roundtrip the return URL
        //        var props = new AuthenticationProperties
        //        {
        //            RedirectUri = returnUrl,
        //            Items = { { "scheme", provider } }
        //        };
        //        return new ChallengeResult(provider, props);
        //    }
        //}

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        //[HttpGet]
        //public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
        //{
        //    // read external identity from the temporary cookie
        //    var info = await HttpContext.Authentication.GetAuthenticateInfoAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
        //    var tempUser = info?.Principal;
        //    if (tempUser == null)
        //    {
        //        throw new Exception("External authentication error");
        //    }

        //    // retrieve claims of the external user
        //    var claims = tempUser.Claims.ToList();

        //    // try to determine the unique id of the external user - the most common claim type for that are the sub claim and the NameIdentifier
        //    // depending on the external provider, some other claim type might be used
        //    var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
        //    if (userIdClaim == null)
        //    {
        //        userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
        //    }
        //    if (userIdClaim == null)
        //    {
        //        throw new Exception("Unknown userid");
        //    }

        //    // remove the user id claim from the claims collection and move to the userId property
        //    // also set the name of the external authentication provider
        //    claims.Remove(userIdClaim);
        //    var provider = info.Properties.Items["scheme"];
        //    var userId = userIdClaim.Value;

        //    // check if the external user is already provisioned
        //    var user = _users.FindByExternalProvider(provider, userId);
        //    if (user == null)
        //    {
        //        // this sample simply auto-provisions new external user
        //        // another common approach is to start a registrations workflow first
        //        user = _users.AutoProvisionUser(provider, userId, claims);
        //    }

        //    var additionalClaims = new List<Claim>();

        //    // if the external system sent a session id claim, copy it over
        //    var sid = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
        //    if (sid != null)
        //    {
        //        additionalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
        //    }

        //    // if the external provider issued an id_token, we'll keep it for signout
        //    AuthenticationProperties props = null;
        //    var id_token = info.Properties.GetTokenValue("id_token");
        //    if (id_token != null)
        //    {
        //        props = new AuthenticationProperties();
        //        props.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
        //    }

        //    // issue authentication cookie for user
        //    await HttpContext.Authentication.SignInAsync(user.SubjectId, user.Username, provider, props, additionalClaims.ToArray());

        //    // delete temporary cookie used during external authentication
        //    await HttpContext.Authentication.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

        //    // validate return URL and redirect back to authorization endpoint
        //    if (_interaction.IsValidReturnUrl(returnUrl))
        //    {
        //        return Redirect(returnUrl);
        //    }

        //    return Redirect("~/");
        //}
    }
}