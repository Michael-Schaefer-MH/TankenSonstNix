using System.Globalization;
using Android.Content;
using Android.Locations;
using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using TankenSonstNix.Models;
using TankenSonstNix.Services;
using AndroidUri = Android.Net.Uri;
using CarAction = AndroidX.Car.App.Model.Action;

namespace TankenSonstNix.Car;

public class StationListScreen : Screen
{
    private const string PrefsName = "TankenSonstNixPrefs";
    private const string ApiKeyPrefKey = "apikey";

    private static readonly string[] LocationPermissions = { "android.permission.ACCESS_FINE_LOCATION" };

    private readonly TankerkoenigService _service = new(new HttpClient());

    private bool _isLoading = true;
    private string? _errorMessage;
    private List<GasStation> _stations = new();

    public StationListScreen(CarContext carContext) : base(carContext)
    {
        RequestLocationAndLoad();
    }

    private void RequestLocationAndLoad()
    {
        CarContext.RequestPermissions(LocationPermissions, new PermissionsListener((granted, rejected) =>
        {
            if (granted.Contains("android.permission.ACCESS_FINE_LOCATION"))
            {
                _ = LoadStationsAsync();
            }
            else
            {
                _isLoading = false;
                _errorMessage = "Standortzugriff im Auto nicht erlaubt.";
                Invalidate();
            }
        }));
    }

    private async Task LoadStationsAsync()
    {
        try
        {
            var prefs = CarContext.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
            var apiKey = prefs.GetString(ApiKeyPrefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _errorMessage = "Bitte zuerst am Handy einen Tankerkönig API-Key eingeben.";
                return;
            }

            var location = GetBestLastKnownLocation();
            if (location is null)
            {
                _errorMessage = "Kein Standort verfügbar.";
                return;
            }

            _stations = await _service.GetNearbyStationsAsync(location.Latitude, location.Longitude, apiKey);
            if (_stations.Count == 0)
                _errorMessage = "Keine Tankstellen im Umkreis gefunden.";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Invalidate();
        }
    }

    private Location? GetBestLastKnownLocation()
    {
        var locationManager = (LocationManager)CarContext.GetSystemService(Context.LocationService)!;
        string[] providers = { LocationManager.GpsProvider, LocationManager.NetworkProvider, LocationManager.PassiveProvider };

        Location? best = null;
        foreach (var provider in providers)
        {
            if (!locationManager.IsProviderEnabled(provider))
                continue;

            var location = locationManager.GetLastKnownLocation(provider);
            if (location is null)
                continue;

            if (best is null || location.Time > best.Time)
                best = location;
        }

        return best;
    }

    public override ITemplate OnGetTemplate()
    {
        var builder = new ListTemplate.Builder()
            .SetTitle("Tankstellen in der Nähe")
            .SetHeaderAction(CarAction.AppIcon!);

        if (_isLoading)
        {
            builder.SetLoading(true);
            return builder.Build()!;
        }

        if (_stations.Count == 0)
        {
            var items = new ItemList.Builder()
                .AddItem(new Row.Builder().SetTitle(_errorMessage ?? "Keine Daten.").Build()!);
            builder.SetSingleList(items.Build()!);
            return builder.Build()!;
        }

        var itemList = new ItemList.Builder();
        foreach (var station in _stations)
        {
            var row = new Row.Builder()
                .SetTitle(station.Name)
                .AddText(station.Address)
                .AddText($"{station.DistanceText} · Diesel {station.DieselText} · E5 {station.E5Text} · E10 {station.E10Text}")
                .SetOnClickListener(new ClickListener(() => NavigateTo(station)))
                .Build();
            itemList.AddItem(row!);
        }

        builder.SetSingleList(itemList.Build()!);
        return builder.Build()!;
    }

    private void NavigateTo(GasStation station)
    {
        var lat = station.Lat.ToString(CultureInfo.InvariantCulture);
        var lng = station.Lng.ToString(CultureInfo.InvariantCulture);
        var intent = new Intent(CarContext.ActionNavigate, AndroidUri.Parse($"geo:{lat},{lng}"));
        CarContext.StartCarApp(intent);
    }

    private sealed class ClickListener : Java.Lang.Object, IOnClickListener
    {
        private readonly System.Action _onClick;

        public ClickListener(System.Action onClick)
        {
            _onClick = onClick;
        }

        public void OnClick() => _onClick();
    }

    private sealed class PermissionsListener : Java.Lang.Object, IOnRequestPermissionsListener
    {
        private readonly System.Action<IList<string>, IList<string>> _onResult;

        public PermissionsListener(System.Action<IList<string>, IList<string>> onResult)
        {
            _onResult = onResult;
        }

        public void OnRequestPermissionsResult(IList<string> approved, IList<string> rejected) => _onResult(approved, rejected);
    }
}
