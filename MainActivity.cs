using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using TankenSonstNix.Adapters;
using TankenSonstNix.Services;

namespace TankenSonstNix;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    private const int LocationPermissionRequestCode = 100;
    private const string PrefsName = "TankenSonstNixPrefs";
    private const string ApiKeyPrefKey = "apikey";

    private static readonly string[] LocationPermissions =
    {
        "android.permission.ACCESS_FINE_LOCATION",
        "android.permission.ACCESS_COARSE_LOCATION"
    };

    private readonly TankerkoenigService _service = new(new HttpClient());

    private EditText _apiKeyInput = null!;
    private Button _refreshButton = null!;
    private ProgressBar _loadingIndicator = null!;
    private TextView _statusLabel = null!;
    private TextView _emptyView = null!;
    private ListView _stationsList = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        _apiKeyInput = FindViewById<EditText>(Resource.Id.apiKeyInput)!;
        _refreshButton = FindViewById<Button>(Resource.Id.refreshButton)!;
        _loadingIndicator = FindViewById<ProgressBar>(Resource.Id.loadingIndicator)!;
        _statusLabel = FindViewById<TextView>(Resource.Id.statusLabel)!;
        _emptyView = FindViewById<TextView>(Resource.Id.emptyView)!;
        _stationsList = FindViewById<ListView>(Resource.Id.stationsList)!;

        var prefs = GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
        _apiKeyInput.Text = prefs.GetString(ApiKeyPrefKey, string.Empty);

        _refreshButton.Click += (_, _) => OnRefreshClicked();
    }

    private void OnRefreshClicked()
    {
        var apiKey = _apiKeyInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ShowError("Bitte zuerst einen Tankerkönig API-Key eingeben (kostenlos unter creativecommons.tankerkoenig.de).");
            return;
        }

        // API-Key lokal merken
        GetSharedPreferences(PrefsName, FileCreationMode.Private)!
            .Edit()!
            .PutString(ApiKeyPrefKey, apiKey)!
            .Apply();

        if (HasLocationPermission())
        {
            _ = LoadStationsAsync(apiKey);
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
            var apiKey = _apiKeyInput.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(apiKey))
                _ = LoadStationsAsync(apiKey);
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

    private void ShowError(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.Visibility = ViewStates.Visible;
    }
}
