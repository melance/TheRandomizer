namespace TheRandomizer.Maui.Interfaces;

public interface INavigationService
{
    Task GoToAsync(String route, Boolean modal = false, params (String Name, Object Value)[]? parameters);
    Task GoBackAsync();
}
