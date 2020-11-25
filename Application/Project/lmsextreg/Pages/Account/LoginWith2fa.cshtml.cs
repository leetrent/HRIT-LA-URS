using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using lmsextreg.Data;
using lmsextreg.Models;
using lmsextreg.Constants;
using lmsextreg.Services;

namespace lmsextreg.Pages.Account
{
   //[Authorize(Roles = "STUDENT,APPROVER")]
   [AllowAnonymous]
    public class LoginWith2faModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginWith2faModel> _logger;
        private readonly IEventLogService _eventLogService;

        public LoginWith2faModel
        (
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginWith2faModel> logger,
            IEventLogService eventLogSvc
        )
        {
            _signInManager          = signInManager;
            _logger                 = logger;
            _eventLogService        = eventLogSvc;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Authenticator code")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Remember this machine")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ReturnUrl = returnUrl;
            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            string logSnippet = new StringBuilder("[")
                    .Append(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"))
                    .Append("][LoginWith2fa][OnPostAsync] => ")
                    .ToString();

            if (!ModelState.IsValid)
            {
                return Page();
            }
 
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            Console.WriteLine(logSnippet + $"(user == null): {user == null}");

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            Console.WriteLine(logSnippet + $"({user.UserName}): {user.UserName}");

            var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

            if (result.Succeeded)
            {
                Console.WriteLine(logSnippet + $"{user.UserName} successfully logged in using 2FA.");

                ///////////////////////////////////////////////////////////////////
                // Log the 'LOGIN_WITH_2FA' event
                ///////////////////////////////////////////////////////////////////
                _eventLogService.LogEvent(EventTypeCodeConstants.LOGIN_WITH_2FA, user);  

                return RedirectToPage("./LoginWith2fa2"); 
            }
            else if (result.IsLockedOut)
            {
                Console.WriteLine(logSnippet + $"{user.UserName} has been locked out.");
                return RedirectToPage("./Lockout");
            }
            else
            {
                Console.WriteLine(logSnippet + $"Invalid authenticator code entered for {user.UserName}.");
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return Page();
            }
        }       
    }
}
