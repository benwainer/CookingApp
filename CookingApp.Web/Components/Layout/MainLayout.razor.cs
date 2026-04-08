using Microsoft.AspNetCore.Components;

namespace CookingApp.Web.Components.Layout;

public partial class MainLayout
{
    private bool isMobileNavOpen = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Auth.InitializeAsync();
            StateHasChanged();
        }
    }

    private void ToggleMobileNav() => isMobileNavOpen = !isMobileNavOpen;
    private void CloseMobileNav() => isMobileNavOpen = false;

    private async Task Logout()
    {
        await Auth.LogoutAsync();
        isMobileNavOpen = false;
        Nav.NavigateTo("/");
    }
}
