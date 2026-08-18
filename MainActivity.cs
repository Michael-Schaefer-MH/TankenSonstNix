using System.Globalization;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using TankenSonstNix.Adapters;
using TankenSonstNix.Models;
using TankenSonstNix.Services;
using AndroidUri = Android.Net.Uri;

namespace TankenSonstNix;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    private const int LocationPermissionRequestCode = 100;
    private const string ApiKey = "4d0f89db-c7e2-471f-9583-eda994ef2050";

    private static readonly string[] LocationPermissions =
    {
        "android.permission.ACCESS_FINE_LOCATION",
        "android.permission.ACCESS_COARSE_LOCATION"
    };

    private readonly TankerkoenigService _service = new(new HttpClient());

    private Button _refreshButton = null!;
    private ProgressBar _loadingIndicator = null!;
    private TextView _statusLabel = null!;
    private TextView _emptyView = null!;
    private ListView _stationsList = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        _refreshButton = FindViewById<Button>(Resource.Id.refreshButton)!;
        _loadingIndicator = FindViewById<ProgressBar>(Resource.Id.loadingIndicator)!;
        _statusLabel = FindViewById<TextView>(Resource.Id.statusLabel)!;
        _emptyView = FindViewById<TextView>(Resource.Id.emptyView)!;
        _stationsList = FindViewById<ListView>(Resource.Id.stationsList)!;

        _refreshButton.Click += (_, _) => OnRefreshClicked();
        _stationsList.ItemClick += (_, e) => OnStationClicked(((StationAdapter)_stationsList.Adapter!)[e.Position]);

        OnRefreshClicked();
    }

    private void OnRefreshClicked()
    {
        if (HasLocationPermission())
        {
            _ = LoadStationsAsync(ApiKey);
        }
        else
        {
            RequestPermissions(LocationPermissions, LocationPermissionRequestCode);
        }
    }

    private bool HasLocationPermission()
    {
        return CheckSelfPermission("android.permission.ACCESS_FINE_LOCATION") == Permission.Granted
               || CheckSelfPermission("android.permission.ACCESS_COARSE_LOCATION") == Permission.Granted;
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode != LocationPermissionRequestCode)
            return;

        if (grantResults.Length > 0 && grantResults.Any(r => r == Permission.Granted))
        {
            _ = LoadStationsAsync(ApiKey);
        }
        else
        {
            ShowError("Ohne Standortzugriff können keine Tankstellen in der Nähe gesucht werden.");
        }
    }

    private async Task LoadStationsAsync(string apiKey)
    {
        try
        {
            SetLoading(true);

            var location = GetBestLastKnownLocation();
            if (location is null)
            {
                ShowError("Kein Standort verfügbar. GPS/Standort aktivieren und erneut versuchen.");
                return;
            }

            var stations = await _service.GetNearbyStationsAsync(location.Latitude, location.Longitude, apiKey);

            _stationsList.Adapter = new StationAdapter(this, stations);
            _emptyView.Visibility = stations.Count == 0 ? ViewStates.Visible : ViewStates.Gone;

            if (stations.Count == 0)
                ShowError("Keine Tankstellen im Umkreis gefunden.");
        }
        catch (TankerkoenigApiKeyException)
        {
            ShowError("Der API-Key ist ungültig.");
        }
        catch (Exception ex)
        {
            ShowError($"Fehler beim Laden der Preise: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    /// <summary>
    /// Liefert die aktuellste zuletzt bekannte Position über alle verfügbaren Provider.
    /// Hinweis: Im frisch gestarteten Emulator kann das anfangs null liefern, bis einmal
    /// ein Standort gesetzt wurde (z. B. über die Emulator-Extended-Controls oder Google Maps).
    /// </summary>
    private Location? GetBestLastKnownLocation()
    {
        var locationManager = (LocationManager)GetSystemService(LocationService)!;
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

    private void SetLoading(bool isLoading)
    {
        _refreshButton.Enabled = !isLoading;
        _loadingIndicator.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
        if (isLoading)
            _statusLabel.Visibility = ViewStates.Gone;
    }

    private void OnStationClicked(GasStation station)
    {
        var lat = station.Lat.ToString(CultureInfo.InvariantCulture);
        var lng = station.Lng.ToString(CultureInfo.InvariantCulture);
        var label = Uri.EscapeDataString(station.Name);

        var uri = AndroidUri.Parse($"geo:{lat},{lng}?q={lat},{lng}({label})")!;
        var intent = new Intent(Intent.ActionView, uri);
        intent.SetPackage("com.google.android.apps.maps");

        if (intent.ResolveActivity(PackageManager!) is null)
            intent.SetPackage(null);

        StartActivity(intent);
    }

    private void ShowError(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.Visibility = ViewStates.Visible;
    }
}
