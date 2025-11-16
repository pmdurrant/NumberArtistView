using Core.Business.Objects;
using netDxf;
using Newtonsoft.Json;
using NumberArtist.View.Views;
using NumberArtistView.Models;
using NumberArtistView.Services;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using netDxf;

namespace NumberArtistView
{
    public partial class MainPage : ContentPage
    {
        private PolylineDrawable polylineDrawable;
        private object dxfFileEntry;
        private readonly DatabaseService _databaseService;
        private readonly Guid _userId;

        public MainPage(DatabaseService databaseService)
        {
            InitializeComponent(); // This now correctly loads the UI from MainPage.xaml
            _databaseService = databaseService;

            var userIdString = Preferences.Get("userId", string.Empty); // Get the user ID as string
            if (!Guid.TryParse(userIdString, out _userId))
            {
                _userId = Guid.Empty;
            }


            polylineDrawable = new PolylineDrawable();
            this.GraphicsView.Drawable = (IDrawable)polylineDrawable; // Use the GraphicsView from XAML

            // The ListView in XAML is named LayersListView, let's bind its selection changed event
            LayersListView.ItemSelected += LayerPicker_ItemSelected;


            //var recordCount = Task.Run(async () => _databaseService.GetRecordCount().Result);

            //var files= Task.Run(async () => _databaseService.GetDxfFilesAsync(_userId).Result);
            //// Load files into picker on a background thread
            Task.Run(async () => await LoadDxfFilesIntoPicker());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Task.Run(async () => await _databaseService.CopyFilesFromServerToLocalDb());


            Task.Run(async () => await LoadDxfFilesIntoPicker());

            // You can also await the other tasks here if needed
            var recordCount = await _databaseService.GetRecordCount();
            var files = await _databaseService.GetDxfFilesAsync(_userId);
            // Now 'files' will contain the list of DxfFileEntry objects

        }

        private async Task LoadDxfFilesIntoPicker()
        {
            if (_userId == Guid.Empty)
            {
                await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Error", "Could not determine user.", "OK"));
                return;
            }

            await _databaseService.InitializeAsync();


            var dxfFiles = await _databaseService.GetDxfFilesAsync(_userId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                DxfFilePicker.ItemsSource = dxfFiles;
                DxfFilePicker.ItemDisplayBinding = new Binding("Name");

                if (dxfFiles.Any())
                {
                    DxfFilePicker.SelectedIndex = 0;
                }
            });
        }

        // Extracted method to address S1199
        private async Task ProcessDxfFileEntryAsync(DxfFileEntry   dxfFileEntry)
        {
            var _bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dxfFileEntry));
            if (_bytes == null || _bytes.Length == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DisplayAlert("Error", "DXF file data is empty.", "OK");
                });
                return;
            }

            try
            {
                using (Stream stream = new MemoryStream(_bytes))
                {
                    DxfDocument loaded = DxfDocument.Load(stream);

                    if (loaded == null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            DisplayAlert("Error", "Error loading DXF file.", "OK");
                        });
                        return;
                    }

                    polylineDrawable.Polylines = loaded.Entities.Polylines2D.Select(pline => new Pline2DModel
                    {
                        IsClosed = pline.IsClosed,
                        Layer = pline.Layer.Name,
                        LayerColour = new LayerColorObject() { R = 255, G = 0, B = 0 }, // You might want to set actual color values here
                        Vertices = pline.Vertexes.Select(v => new VertexModel
                        {
                            X = v.Position.X,
                            Y = v.Position.Y,
                            Bulge = v.Bulge
                        }).ToList()
                    }).ToList();

                    // Update the ItemsSource for the ListView defined in XAML
                    var layers = polylineDrawable.Layers.OrderBy(l => l).ToList();
                    LayersListView.ItemsSource = layers;

                    if (layers.Any())
                    {
                        LayersListView.SelectedItem = layers[0];
                    }
                    else
                    {
                        polylineDrawable.SelectedLayer = null;
                        GraphicsView.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DisplayAlert("Error", $"An error occurred while loading the DXF file: {ex.Message}", "OK");
                });
            }
        }

        private async Task LoadDxfData(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                await DisplayAlert("Error", "Invalid resource name.", "OK");
                return;
            }
    
            ResourceAccess resourceaccess =     new ResourceAccess();

          
            try
            { 
                
                var dxfFile= await resourceaccess.GetResourceAsync(resourceName);

                byte[] array = Encoding.ASCII.GetBytes(dxfFile);


                using (var stream = new MemoryStream(array))
                {
                    DxfDocument loaded = DxfDocument.Load(stream);
                    if (loaded == null)
                    {
                        await DisplayAlert("Error", "Error loading DXF file.", "OK");
                        return;
                    }

                    polylineDrawable.Polylines = loaded.Entities.Polylines2D.Select(pline => new Pline2DModel
                    {
                        IsClosed = pline.IsClosed,
                        Layer = pline.Layer.Name,
                        LayerColour = pline.Layer.Color.Index   ,
                        Vertices = pline.Vertexes.Select(v => new VertexModel
                        {
                            X = v.Position.X,
                            Y = v.Position.Y,
                            Bulge = v.Bulge,
                        }).ToList()
                    }).ToList();
                    var layers = polylineDrawable.Layers.OrderBy(l => l).ToList();
                   List<LayerItem> layers2 = new List<LayerItem>();
                    
                    
                    var itemsPos=1;
                    foreach (var layer in layers)
                    {

                        layers2.Add(new LayerItem() { LayerColour = new LayerColorObject() { R = 255, G = 0, B = 0 }, LayerIndex = itemsPos , LayerName= itemsPos.ToString() });
                   itemsPos++;
                    }
                    LayersListView.ItemsSource = layers2;

                    if (layers.Any())
                    {
                        LayersListView.SelectedItem = layers[0];
                    }
                    else
                    {
                        polylineDrawable.SelectedLayer = null;
                    }

                    GraphicsView.Invalidate();
                 
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while loading the DXF file: {ex.Message}", "OK");
            }
        }

        private void ColorBox_Tapped(object sender, TappedEventArgs e)
        {
            // Event handling logic goes here
        }

        private void DxfFilePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DxfFilePicker.SelectedItem is DxfFileEntry selectedFile)
            {
                LoadDxfData(selectedFile.ResourceName);

                GraphicsView.Invalidate(); // Redraw the view
            }
        }

        private void LayerPicker_ItemSelected(object? sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is LayerItem selectedLayer)
            {
                polylineDrawable.SelectedLayer = selectedLayer.LayerName;
                GraphicsView.Invalidate(); // Redraw the view
            }
            else if (e.SelectedItem is string selectedLayerName) // Fallback for old behavior
            {
                polylineDrawable.SelectedLayer = selectedLayerName;
                GraphicsView.Invalidate(); // Redraw the view
            }
        }

        private async void LogoutButton_Clicked(object sender, EventArgs e)
        {
            // Clear user session data
            Preferences.Clear("UserId");

            // Navigate back to the LoginPage by setting it as the new MainPage
            Application.Current.MainPage = new LoginPage();
        }

        private async void SelectDxfButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "com.autodesk.dxf" } }, // UTType for DXF
                        { DevicePlatform.Android, new[] { "application/dxf" } }, // MIME type
                        { DevicePlatform.WinUI, new[] { ".dxf" } }, // file extension
                        { DevicePlatform.Tizen, new[] { "*/*" } },
                        { DevicePlatform.macOS, new[] { "dxf" } }, // UTType
                    });

                var pickOptions = new PickOptions
                {
                    PickerTitle = "Please select a DXF file",
                    FileTypes = customFileType,
                };

                var result = await FilePicker.PickAsync(pickOptions);

                if (result != null)
                {
                    using (var stream = await result.OpenReadAsync())
                    {
                        DxfDocument loaded = DxfDocument.Load(stream);
                        //  ProcessDxfDocument(loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while selecting the DXF file: {ex.Message}", "OK");
            }
        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {

            Color chColor = null;
            
            
            var box = (BoxView)sender;
               
                switch (e.Direction)
            { case SwipeDirection.Right:
                    chColor = Colors.Red;
                    break;
            }

        }
    }
}