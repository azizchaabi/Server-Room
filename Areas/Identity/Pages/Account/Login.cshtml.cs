#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerRoomMonitor.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
private readonly SignInManager<IdentityUser> _signInManager;
private readonly UserManager<IdentityUser> _userManager;
private readonly ILogger<LoginModel> _logger;


public LoginModel(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    ILogger<LoginModel> logger)
{
    _signInManager = signInManager;
    _userManager = userManager;
    _logger = logger;
}

[BindProperty]
public InputModel Input { get; set; }

public string ReturnUrl { get; set; }

[TempData]
public string ErrorMessage { get; set; }

public class InputModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}

public async Task OnGetAsync(string returnUrl = null)
{
    if (!string.IsNullOrEmpty(ErrorMessage))
    {
        ModelState.AddModelError(string.Empty, ErrorMessage);
    }

    await HttpContext.SignOutAsync(
        IdentityConstants.ExternalScheme);

    ReturnUrl = returnUrl ?? Url.Content("~/");
}

public async Task<IActionResult> OnPostAsync(string returnUrl = null)
{
    if (!ModelState.IsValid)
    {
        return Page();
    }

    var result = await _signInManager.PasswordSignInAsync(
        Input.Email,
        Input.Password,
        Input.RememberMe,
        lockoutOnFailure: false);

    if (result.Succeeded)
    {
        _logger.LogInformation(
            "User {Email} logged in.",
            Input.Email);

        var user = await _userManager.FindByEmailAsync(Input.Email);

        if (user == null)
        {
            await _signInManager.SignOutAsync();

            ModelState.AddModelError(
                string.Empty,
                "Unable to load the user account.");

            return Page();
        }

        // Admin → Admin dashboard
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToPage("/Admin/Index");
        }

        // Technician → Technician dashboard
        if (await _userManager.IsInRoleAsync(user, "Technician"))
        {
            return RedirectToPage("/Technician/Index");
        }

        // User has no recognized role.
        await _signInManager.SignOutAsync();

        ModelState.AddModelError(
            string.Empty,
            "Your account does not have a valid role.");

        return Page();
    }

    if (result.RequiresTwoFactor)
    {
        return RedirectToPage(
            "./LoginWith2fa",
            new
            {
                ReturnUrl = returnUrl,
                RememberMe = Input.RememberMe
            });
    }

    if (result.IsLockedOut)
    {
        _logger.LogWarning(
            "User account {Email} is locked out.",
            Input.Email);

        return RedirectToPage("./Lockout");
    }

    ModelState.AddModelError(
        string.Empty,
        "Invalid login attempt.");

    return Page();
}


}
