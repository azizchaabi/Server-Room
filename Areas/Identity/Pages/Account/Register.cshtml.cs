using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerRoomMonitor.Areas.Identity.Pages.Account;

[Authorize(Roles = "Admin")]
public class RegisterModel : PageModel
{
private readonly UserManager<IdentityUser> _userManager;
private readonly ILogger<RegisterModel> _logger;


public RegisterModel(
    UserManager<IdentityUser> userManager,
    ILogger<RegisterModel> logger)
{
    _userManager = userManager;
    _logger = logger;
}

[BindProperty]
public InputModel Input { get; set; } = new();

public class InputModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(
        100,
        ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
        MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(
        "Password",
        ErrorMessage = "The password and confirmation password do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;
}

public void OnGet()
{
}

public async Task<IActionResult> OnPostAsync()
{
    // Only allow the roles used by this application.
    if (Input.Role != "Admin" && Input.Role != "Technician")
    {
        ModelState.AddModelError(
            "Input.Role",
            "Please select a valid role.");
    }

    if (!ModelState.IsValid)
    {
        return Page();
    }

    // Check whether the email is already registered.
    var existingUser = await _userManager.FindByEmailAsync(Input.Email);

    if (existingUser != null)
    {
        ModelState.AddModelError(
            "Input.Email",
            "A user with this email address already exists.");

        return Page();
    }

    // Create the new Identity user.
    var user = new IdentityUser
    {
        UserName = Input.Email,
        Email = Input.Email,
        EmailConfirmed = true
    };

    var result = await _userManager.CreateAsync(
        user,
        Input.Password);

    if (!result.Succeeded)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return Page();
    }

    // Assign the selected role.
    var roleResult = await _userManager.AddToRoleAsync(
        user,
        Input.Role);

    if (!roleResult.Succeeded)
    {
        foreach (var error in roleResult.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        // Remove the account if role assignment failed.
        await _userManager.DeleteAsync(user);

        return Page();
    }

    _logger.LogInformation(
        "Administrator {Admin} created user {Email} with role {Role}.",
        User.Identity?.Name,
        Input.Email,
        Input.Role);

    // IMPORTANT:
    // Do NOT call SignInAsync here.
    //
    // The administrator stays logged in.
    // The newly created user is NOT automatically logged in.

    TempData["SuccessMessage"] =
        $"User {Input.Email} was successfully created as {Input.Role}.";

    return RedirectToPage("/Account/Register");
}


}
