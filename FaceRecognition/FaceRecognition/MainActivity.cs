using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System;
using Android.Content;
using Android.Graphics;
using Android.Gms.Vision.Faces;
using Android.Gms.Vision;
using Android.Util;
using Android.Provider;
using AndroidX.Core.App;
using Android;
using System.IO;
using AndroidX.Core.Content;

namespace FaceRecognition
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        public const int PICK_IMAGE_GALERY = 1000;
        public const int REQUEST_IMAGE_CAPTURE = 2000;
        private ImageView _imageView;
        private FaceDetector detector;
        Bitmap editedBitmap;
        TextView txtDescription;
        private Android.Net.Uri imageUri;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            GetPermissions();
            detector = new FaceDetector.Builder(this)
                .SetTrackingEnabled(false)
                .SetLandmarkType(LandmarkDetectionType.All)
                .SetClassificationType(ClassificationType.All)
                .Build();


            _imageView = FindViewById<ImageView>(Resource.Id.imageviewface);
            Button btngalery = FindViewById<Button>(Resource.Id.btngalery);
            btngalery.Click += Btngalery_Click;

            Button btncamera = FindViewById<Button>(Resource.Id.btncamera);
            btncamera.Click += Btncamera_Click;

            txtDescription = FindViewById<TextView>(Resource.Id.txtDescription);
        }

        private void GetPermissions()
        {
            try
            {
                ActivityCompat.RequestPermissions(this, new string[]
                {
                    Manifest.Permission.Camera,
                    Manifest.Permission.ReadExternalStorage,
                    Manifest.Permission.WriteExternalStorage,
                }, 0);
            }
            catch
            {

            }
        }

        private void Btngalery_Click(object sender, EventArgs e)
        {
            Intent = new Intent();
            Intent.SetType("image/*");
            Intent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(Intent, "Select Picture"), PICK_IMAGE_GALERY);
        }

        private void Btncamera_Click(object sender, EventArgs e)
        {
            Intent takePictureIntent = new Intent(MediaStore.ActionImageCapture);
            Java.IO.File cameraFile = null;
            try
            {
                var documentsDirectry = GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
                cameraFile = new Java.IO.File(documentsDirectry, "example.png");
                if (cameraFile != null)
                {
                    using (var mediaStorageDir = new Java.IO.File(documentsDirectry, string.Empty))
                    {
                        if (!mediaStorageDir.Exists())
                        {
                            if (!mediaStorageDir.Mkdirs())
                                throw new IOException("Couldn't create directory, have you added the WRITE_EXTERNAL_STORAGE permission?");
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                // Error occurred while creating the File
            }
            imageUri = FileProvider.GetUriForFile(this, "com.ecoscadiz.facerecognition.fileprovider", cameraFile);
            takePictureIntent.PutExtra(MediaStore.ExtraOutput, imageUri);

            StartActivityForResult(takePictureIntent, REQUEST_IMAGE_CAPTURE);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            detector.Release();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if ((requestCode == PICK_IMAGE_GALERY) && (resultCode == Result.Ok) && (data != null))
            {
                imageUri = data.Data;
                LaunchMediaScanIntent();
                try
                {
                    ProcessCameraPicture();
                }
                catch (Exception e)
                {
                    Toast.MakeText(this, "Error al cargar la imagen", ToastLength.Short).Show();
                }
            }

            if (requestCode == REQUEST_IMAGE_CAPTURE && resultCode == Result.Ok)
            {
                if(imageUri != null)
                {
                    LaunchMediaScanIntent();
                    try
                    {
                        ProcessCameraPicture();
                    }
                    catch (Exception e)
                    {
                        Toast.MakeText(this, "Error al cargar la imagen", ToastLength.Short).Show();
                    }
                }
            }
        }

        private void LaunchMediaScanIntent()
        {
            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            mediaScanIntent.SetData(imageUri);
            this.SendBroadcast(mediaScanIntent);
        }

        private void ProcessCameraPicture()
        {
            Bitmap bitmap = DecodeBitmapUri(this, imageUri);
            if (detector.IsOperational && bitmap != null)
            {
                editedBitmap = Bitmap.CreateBitmap(bitmap.Width, bitmap
                    .Height, bitmap.GetConfig());
                float scale = Resources.DisplayMetrics.Density;
                Paint paint = new Paint(PaintFlags.AntiAlias);
                paint.Color = Color.Green;
                paint.TextSize = (int)(16 * scale);
                paint.SetShadowLayer(1f, 0f, 1f, Color.White);
                paint.SetStyle(Paint.Style.Stroke);
                paint.StrokeWidth = 6f;
                Canvas canvas = new Canvas(editedBitmap);
                canvas.DrawBitmap(bitmap, 0, 0, paint);
                Frame frame = new Frame.Builder().SetBitmap(editedBitmap).Build();
                SparseArray faces = detector.Detect(frame);               
                string text = "";
                for (int index = 0; index < faces.Size(); ++index)
                {
                    Face face = faces.ValueAt(index) as Face;
                    //canvas.DrawRect(
                    //        face.Position.X,
                    //                face.Position.Y,
                    //                face.Position.X + face.Width,
                    //                face.Position.Y + face.Height, paint); //CREA EL RECUADRO
                    text += "Cara " + (index + 1) + "\n";
                    text += "Probilidad de una sonrisa:" + " " + face.IsSmilingProbability * 100 + "\n";
                    text += "Probilidad que el ojo izquierdo este abierto : " + " " + face.IsLeftEyeOpenProbability * 100 + "\n";
                    text += "Probilidad que el ojo derecho este abierto: " + " " + face.IsRightEyeOpenProbability * 100 + "\n";
                    foreach (Landmark landmark in face.Landmarks)
                    {
                        int cx = (int)(landmark.Position.X);
                        int cy = (int)(landmark.Position.Y);
                        //canvas.DrawCircle(cx, cy, 8, paint); // CREA EL CIRCULO
                    }
                }

                if (faces.Size() == 0)
                {
                    txtDescription.Text = "Scaneo fallido";
                }
                else
                {
                    _imageView.SetImageBitmap(editedBitmap);
                    text += "\n\n" + "Numero de caras detectadas: " + " " + faces.Size().ToString() + "\n\n";
                }
                txtDescription.Text = text;
            }
            else
            {
                txtDescription.Text = "No se pudo configurar el detector!";
            }
        }

        private Bitmap DecodeBitmapUri(Context ctx, Android.Net.Uri uri)
        {
            int targetW = 300;
            int targetH = 300;
            BitmapFactory.Options bmOptions = new BitmapFactory.Options();
            bmOptions.InJustDecodeBounds = true;
            BitmapFactory.DecodeStream(ctx.ContentResolver.OpenInputStream(uri), null, bmOptions);
            int photoW = bmOptions.OutWidth;
            int photoH = bmOptions.OutHeight;

            int scaleFactor = Math.Min(photoW / targetW, photoH / targetH);
            bmOptions.InJustDecodeBounds = false;
            bmOptions.InSampleSize = scaleFactor;

            return BitmapFactory.DecodeStream(ctx.ContentResolver
                    .OpenInputStream(uri), null, bmOptions);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}