using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Locations;
using Location.Droid.Services;
using Android.Content.PM;
using Android.Telephony;
using Android.Content;
using System.IO;
using Android.Net;

namespace Location.Droid
{
	[Activity (Label = "Captura Dinâmica de Posição V2", MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.ScreenLayout)]
	public class MainActivity : Activity
	{
		readonly string logTag = "MainActivity";


        int count = 0;
        string _server = "";


        // make our labels
        TextView latText;
		TextView longText;
		TextView altText;
		TextView speedText;
		TextView bearText;
		TextView accText;
        TextView txt_horarios;

        Spinner spn_text;

 
        #region Lifecycle

        //Lifecycle stages
        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			Log.Debug (logTag, "OnCreate: Location app is becoming active");

			SetContentView (Resource.Layout.Main);
            spn_text = FindViewById<Spinner>(Resource.Id.spn_server);
            spn_text.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(Spinner_ItemSelected);
            var adapter = ArrayAdapter.CreateFromResource(
                    this, Resource.Array.server_array, Android.Resource.Layout.SimpleSpinnerItem);

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spn_text.Adapter = adapter;


            // This event fires when the ServiceConnection lets the client (our App class) know that
            // the Service is connected. We use this event to start updating the UI with location
            // updates from the Service
            App.Current.LocationServiceConnected += (object sender, ServiceConnectedEventArgs e) => {
				Log.Debug (logTag, "ServiceConnected Event Raised");
				// notifies us of location changes from the system
				App.Current.LocationService.LocationChanged += HandleLocationChanged;
				//notifies us of user changes to the location provider (ie the user disables or enables GPS)
				App.Current.LocationService.ProviderDisabled += HandleProviderDisabled;
				App.Current.LocationService.ProviderEnabled += HandleProviderEnabled;
				// notifies us of the changing status of a provider (ie GPS no longer available)
				App.Current.LocationService.StatusChanged += HandleStatusChanged;
			};

			latText = FindViewById<TextView> (Resource.Id.lat);
			longText = FindViewById<TextView> (Resource.Id.longx);
			altText = FindViewById<TextView> (Resource.Id.alt);
			speedText = FindViewById<TextView> (Resource.Id.speed);
			bearText = FindViewById<TextView> (Resource.Id.bear);
			accText = FindViewById<TextView> (Resource.Id.acc);
            txt_horarios = FindViewById<TextView>(Resource.Id.txt_horarios);


            altText.Text = "altitude";
			speedText.Text = "speed";
			bearText.Text = "bearing";
			accText.Text = "accuracy";
            txt_horarios.Text = "current time";

            // Start the location service:
            App.StartLocationService();
		}


		protected override void OnPause()
		{
			Log.Debug (logTag, "OnPause: Location app is moving to background");
			base.OnPause();
		}

	
		protected override void OnResume()
		{
			Log.Debug (logTag, "OnResume: Location app is moving into foreground");
			base.OnResume();
		}
        private void Spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            string toast = string.Format("The server is {0}", spinner.GetItemAtPosition(e.Position));
            Toast.MakeText(this, toast, ToastLength.Long).Show();

            _server = spinner.GetItemAtPosition(e.Position).ToString();

        }



        protected override void OnDestroy ()
		{
			Log.Debug (logTag, "OnDestroy: Location app is becoming inactive");
			base.OnDestroy ();

            // Stop the location service:
            //App.StopLocationService();
		}

		#endregion

		#region Android Location Service methods

		///<summary>
		/// Updates UI with location data
		/// </summary>
		public void HandleLocationChanged(object sender, LocationChangedEventArgs e)
		{
			Android.Locations.Location location = e.Location;
			Log.Debug (logTag, "Foreground updating");

			// these events are on a background thread, need to update on the UI thread
			RunOnUiThread (() => {
				latText.Text = String.Format ("Latitude: {0}", location.Latitude);
				longText.Text = String.Format ("Longitude: {0}", location.Longitude);
				altText.Text = String.Format ("Altitude: {0}", location.Altitude);
				speedText.Text = String.Format ("Speed: {0}", location.Speed);
				accText.Text = String.Format ("Accuracy: {0}", location.Accuracy);
				bearText.Text = String.Format ("Bearing: {0}", location.Bearing);
                txt_horarios.Text = String.Format("Ultima Coleta: {0} \n Total de {1} coletas", DateTime.Now.ToString(),count.ToString());

                SavetoSd(location.Latitude.ToString(), location.Longitude.ToString());
                count++;


            });

		}

		public void HandleProviderDisabled(object sender, ProviderDisabledEventArgs e)
		{
			Log.Debug (logTag, "Location provider disabled event raised");
		}

		public void HandleProviderEnabled(object sender, ProviderEnabledEventArgs e)
		{
			Log.Debug (logTag, "Location provider enabled event raised");
		}

		public void HandleStatusChanged(object sender, StatusChangedEventArgs e)
		{
			Log.Debug (logTag, "Location status changed, event raised");
		}



        private void SavetoSd(String lat, String lon)
        {

            //Get IMEI
            var telephonyManager = (TelephonyManager)GetSystemService(TelephonyService);
            var id = telephonyManager.DeviceId;


            var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
            sdCardPath += "/Download";
            var filePath = System.IO.Path.Combine(sdCardPath, DateTime.Now.ToString("yyyy-MM-dd_HH:mm") + "_" + id.ToString() + "_monioring.txt");
            //var filePath = System.IO.Path.Combine(sdCardPath, DateTime.Now.ToString("yyyy-MM-dd_HH") + "_" + id.ToString() + "_monioring.txt");
            //Log.Debug(filePath, "");

            var filter = new IntentFilter(Intent.ActionBatteryChanged);
            var battery = RegisterReceiver(null, filter);
            int level = battery.GetIntExtra(BatteryManager.ExtraLevel, -1);
            int scale = battery.GetIntExtra(BatteryManager.ExtraScale, -1);

            int level_0_to_100 = (int)Math.Floor(level * 100D / scale);
            //Log.Debug("Battery Level: "+level_0_to_100.ToString(), "");





            //           if (!System.IO.File.Exists(filePath))
            //           {
            using (System.IO.StreamWriter write = new System.IO.StreamWriter(filePath, true))
            {
                var list = Directory.GetFiles(sdCardPath, "*.txt");

                write.Write(DateTime.Now.ToString() + ";" + id.ToString() + ";" + level_0_to_100.ToString() + ";" + lat + ";" + lon + "\n");
                write.Dispose();
                count++;

                ConnectivityManager connectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);
                NetworkInfo networkInfo = connectivityManager.ActiveNetworkInfo;

                if (networkInfo != null)
                {
                    bool isOnline = networkInfo.IsConnected;

                    bool isWifi = networkInfo.Type == ConnectivityType.Wifi;
                    if (isWifi)
                    {
                        Log.Debug("", "Wifi connected.");

                        if (list.Length > 1)
                        {
                            for (int i = 0; i < list.Length; i++)
                            {
                                if (list[i].ToString() != filePath)
                                {

                                    string source = list[i];
                                    string destination = @"/srv/shiny-server/bases de dados/Mobile";
                                    //string host = "186.201.214.56"; //CASA
                                    //string host = "10.97.0.11"; //LAB
                                    string host = _server;
                                    string username = "lcv";
                                    string password = "15u3@dsf";
                                    int port = 22;  //Port 22 is defaulted for SFTP upload

                                    try
                                    {
                                        SFTP.UploadSFTPFile(host, username, password, source, destination, port);
                                        File.Delete(list[i]);
                                    }
                                    catch
                                    {
                                        Log.Debug("Upload not complete!", "");
                                        write.Write(DateTime.Now.ToString() + "; Upload Failed" + "\n");
                                        write.Dispose();
                                        //Toast.MakeText(ApplicationContext, "WiFi OK - Upload Failed!", ToastLength.Long).Show();
                                    }
                                }
                            }

                        }



                    }
                    else
                    {
                        Log.Debug("", "Wifi disconnected.");
                    }
                }







            }
            //           }

        }

        #endregion

    }
}


