using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Vision;
using Android.Gms.Vision.Barcodes;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using Plugin.Geolocator;
using static Android.Gms.Vision.Detector;
using Plugin.Geolocator.Abstractions;

namespace QRCodeReader
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ISurfaceHolderCallback, IProcessor
    {
        SurfaceView surfaceView;
        TextView txtResult;
        BarcodeDetector barcodeDetector;
        CameraSource cameraSource;
        Button BtnRetry;
        const int RequestCameraPermisionID = 1001;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            //Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            //SetSupportActionBar(toolbar);
            surfaceView = FindViewById<SurfaceView>(Resource.Id.cameraView);
            txtResult = FindViewById<TextView>(Resource.Id.txtResult);

            //Bitmap bitMap = BitmapFactory.DecodeResource(ApplicationContext
            //.Resources, Resource.Drawable.qrcode);

            BtnRetry = FindViewById<Button>(Resource.Id.btnretry);

            barcodeDetector = new BarcodeDetector.Builder(this)
                .SetBarcodeFormats(BarcodeFormat.QrCode & BarcodeFormat.Code128 & BarcodeFormat.Code39 & BarcodeFormat.DataMatrix & BarcodeFormat.Code93 & BarcodeFormat.Ean13 & BarcodeFormat.Ean8)
                .Build();
            cameraSource = new CameraSource
                .Builder(this, barcodeDetector)
                .SetRequestedPreviewSize(320, 240)
                .Build();
            surfaceView.Holder.AddCallback(this);
            barcodeDetector.SetProcessor(this);

            BtnRetry.Click += (s, e) =>
            {
                cameraSource.Start(surfaceView.Holder);
                BtnRetry.Enabled = false;
                getloc();
            };
           
            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);

            if (CheckSelfPermission(Manifest.Permission.AccessCoarseLocation) != (int)Permission.Granted)
            {
                RequestPermissions(new string[] { Manifest.Permission.AccessCoarseLocation, Manifest.Permission.AccessFineLocation }, 0);
            }
            StartGps();
        }
       
        private async void StartGps()
        {
            await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(1), 0.1, false, null);
            CrossGeolocator.Current.PositionChanged += Current_PositionChanged;
        }
        private void Current_PositionChanged(object sender, PositionEventArgs e)
        {            
            Console.WriteLine("Lat: " + e.Position.Latitude + " Lon: " + e.Position.Longitude + " Acc: " + e.Position.Accuracy);
        }

        async void getloc()
        {
            var locator = CrossGeolocator.Current;
            locator.DesiredAccuracy = 50.0;

            await locator.StartListeningAsync(TimeSpan.FromSeconds(5), 10, true, new Plugin.Geolocator.Abstractions.ListenerSettings
            {
                ActivityType = Plugin.Geolocator.Abstractions.ActivityType.AutomotiveNavigation,
                AllowBackgroundUpdates = true,
                DeferLocationUpdates = true,
                DeferralDistanceMeters = 1,
                DeferralTime = TimeSpan.FromSeconds(1),
                ListenForSignificantChanges = true,
                PauseLocationUpdatesAutomatically = false
            });           

            //Plugin.Geolocator.Abstractions.Position position = await locator.GetPositionAsync(TimeSpan.FromSeconds(10));

            locator.PositionChanged += Locator_PositionChanged;
            locator.PositionError += Locator_PositionError; 

            //Console.Write("Position Status: {0}", position.Timestamp.ToString());
            //Console.Write("Position Latitude: {0}", position.Latitude.ToString());
            //Console.Write("Position Longitude: {0}", position.Longitude.ToString());
        }

        private void Locator_PositionError(object sender, PositionErrorEventArgs e)
        {
            Title = "Error: " + e.Error;
        }

        private void Locator_PositionChanged(object sender, PositionEventArgs e)
        {
            Title = "Speed: " + e.Position.Speed.ToString();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            switch (requestCode)
            {
                case RequestCameraPermisionID:
                    {
                        if (grantResults[0] == Permission.Granted)
                        {
                            if (ActivityCompat.CheckSelfPermission(ApplicationContext, Manifest.Permission.Camera) != Android.Content.PM.Permission.Granted)
                            {
                                //Request Permision  
                                ActivityCompat.RequestPermissions(this, new string[]
                                {
                    Manifest.Permission.Camera
                                }, RequestCameraPermisionID);
                                return;
                            }
                            try
                            {
                                cameraSource.Start(surfaceView.Holder);
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    }
                    break;
            }
           
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            //MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }
        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
        }
        public void SurfaceCreated(ISurfaceHolder holder)
        {
            if (ActivityCompat.CheckSelfPermission(ApplicationContext, Manifest.Permission.Camera) != Android.Content.PM.Permission.Granted)
            {
                //Request Permision  
                ActivityCompat.RequestPermissions(this, new string[]
                {
                    Manifest.Permission.Camera
                }, RequestCameraPermisionID);
                return;
            }
            try
            {
                cameraSource.Start(surfaceView.Holder);
            }
            catch (InvalidOperationException)
            {
            }
        }
        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            cameraSource.Stop();
        }
        public void ReceiveDetections(Detections detections)
        {
            SparseArray qrcodes = detections.DetectedItems;
            if (qrcodes.Size() != 0)
            {
                txtResult.Post(() => {
                    Vibrator vibrator = (Vibrator)GetSystemService(Context.VibratorService);                            
                    vibrator.Vibrate(1000);
                    txtResult.Text = ((Barcode)qrcodes.ValueAt(0)).RawValue;
                    //Android.Media.MediaPlayer _player;
                    //_player = Android.Media.MediaPlayer.Create(this, Resource.Raw.test);
                    cameraSource.Stop();
                    BtnRetry.Enabled = true;
                });
            }
        }
        public void Release()
        {

        }

    }
}

