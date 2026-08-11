using Android.App;
using Android.Views;
using Android.Widget;
using TankenSonstNix.Models;

namespace TankenSonstNix.Adapters;

public class StationAdapter : BaseAdapter<GasStation>
{
    private readonly Activity _context;
    private readonly List<GasStation> _items;

    public StationAdapter(Activity context, List<GasStation> items)
    {
        _context = context;
        _items = items;
    }

    public override GasStation this[int position] => _items[position];
    public override int Count => _items.Count;
    public override long GetItemId(int position) => position;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        var view = convertView ?? _context.LayoutInflater!.Inflate(Resource.Layout.item_station, parent, false)!;
        var station = _items[position];

        view.FindViewById<TextView>(Resource.Id.stationName)!.Text = station.Name;
        view.FindViewById<TextView>(Resource.Id.stationAddress)!.Text = station.Address;
        view.FindViewById<TextView>(Resource.Id.stationDistance)!.Text = station.DistanceText;
        view.FindViewById<TextView>(Resource.Id.dieselValue)!.Text = station.DieselText;
        view.FindViewById<TextView>(Resource.Id.e5Value)!.Text = station.E5Text;
        view.FindViewById<TextView>(Resource.Id.e10Value)!.Text = station.E10Text;

        return view;
    }
}
