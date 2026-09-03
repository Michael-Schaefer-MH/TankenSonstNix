using Android.App;
using AndroidX.Car.App;
using AndroidX.Car.App.Validation;

namespace TankenSonstNix.Car;

[Service(Exported = true, Label = "@string/app_name")]
[IntentFilter(new[] { "androidx.car.app.CarAppService" }, Categories = new[] { "androidx.car.app.category.POI" })]
public class TankenCarAppService : CarAppService
{
    // ALLOW_ALL_HOSTS_VALIDATOR ist fuer produktive Play-Store-Apps ungeeignet,
    // reicht aber fuer den persoenlichen Test ueber Desktop Head Unit / Entwicklermodus.
    public override HostValidator CreateHostValidator()
    {
        return HostValidator.AllowAllHostsValidator!;
    }

    public override Session OnCreateSession(SessionInfo? sessionInfo)
    {
        return new TankenCarSession();
    }
}
