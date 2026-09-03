using Android.Content;
using AndroidX.Car.App;

namespace TankenSonstNix.Car;

public class TankenCarSession : Session
{
    public override Screen OnCreateScreen(Intent? intent)
    {
        return new StationListScreen(CarContext!);
    }
}
