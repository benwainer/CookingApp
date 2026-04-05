using Microsoft.AspNetCore.Components;

namespace CookingApp.Web.Components.Layout;

public partial class MainLayout
{
    private void Logout()
    {
        Auth.Logout();
        Nav.NavigateTo("/");
    }
}
