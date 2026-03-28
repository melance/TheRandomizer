using TheRandomizer.Maui.Interfaces;

namespace TheRandomizer.Maui.Services;

public class NavigationService : INavigationService
{
    public Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }

    public Task GoToAsync(String route, Boolean modal = false, params (String Name, Object Value)[]? parameters)
    {
        var paramDict = parameters?.ToDictionary(p => p.Name, p => p.Value) ?? [];
        return Shell.Current.GoToAsync(route, modal, paramDict);
    }
}

