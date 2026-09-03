using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerRoomMonitor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
private readonly UserManager<IdentityUser> _userManager;


public UsersModel(UserManager<IdentityUser> userManager)
{
    _userManager = userManager;
}

public List<UserViewModel> Users { get; set; } = new();

public string CurrentUserId { get; set; } = "";

[TempData]
public string? Message { get; set; }

[TempData]
public string? ErrorMessage { get; set; }

public async Task OnGetAsync()
{
    CurrentUserId = _userManager.GetUserId(User) ?? "";

    var users = _userManager.Users
        .OrderBy(u => u.Email)
        .ToList();

    foreach (var user in users)
    {
        var roles = await _userManager.GetRolesAsync(user);

        Users.Add(new UserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Role = roles.FirstOrDefault() ?? "No role"
        });
    }
}

public async Task<IActionResult> OnPostChangeRoleAsync(string userId)
{
    CurrentUserId = _userManager.GetUserId(User) ?? "";

    if (userId == CurrentUserId)
    {
        ErrorMessage = "You cannot change the role of your own account.";
        return RedirectToPage();
    }

    var user = await _userManager.FindByIdAsync(userId);

    if (user == null)
    {
        ErrorMessage = "User not found.";
        return RedirectToPage();
    }

    var currentRoles = await _userManager.GetRolesAsync(user);

    string newRole;

    if (currentRoles.Contains("Admin"))
    {
        newRole = "Technician";
    }
    else
    {
        newRole = "Admin";
    }

    if (currentRoles.Any())
    {
        var removeResult = await _userManager.RemoveFromRolesAsync(
            user,
            currentRoles);

        if (!removeResult.Succeeded)
        {
            ErrorMessage = "Could not change the user's role.";
            return RedirectToPage();
        }
    }

    var addResult = await _userManager.AddToRoleAsync(
        user,
        newRole);

    if (!addResult.Succeeded)
    {
        ErrorMessage = "Could not assign the new role.";
        return RedirectToPage();
    }

    Message = $"Role changed to {newRole} for {user.Email}.";

    return RedirectToPage();
}

public async Task<IActionResult> OnPostDeleteAsync(string userId)
{
    CurrentUserId = _userManager.GetUserId(User) ?? "";

    if (userId == CurrentUserId)
    {
        ErrorMessage = "You cannot delete your own account.";
        return RedirectToPage();
    }

    var user = await _userManager.FindByIdAsync(userId);

    if (user == null)
    {
        ErrorMessage = "User not found.";
        return RedirectToPage();
    }

    var result = await _userManager.DeleteAsync(user);

    if (!result.Succeeded)
    {
        ErrorMessage = "Could not delete the user.";
        return RedirectToPage();
    }

    Message = $"User {user.Email} was deleted.";

    return RedirectToPage();
}

public class UserViewModel
{
    public string Id { get; set; } = "";

    public string Email { get; set; } = "";

    public string Role { get; set; } = "";
}


}
