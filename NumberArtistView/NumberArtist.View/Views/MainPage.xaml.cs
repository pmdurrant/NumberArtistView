using Core.Business.Objects;
using Core.Business.Objects.Models;
using Microsoft.Maui.Controls;
using netDxf;
using Newtonsoft.Json;
using NumberArtist.View.Views;
using NumberArtistView.Models;
using NumberArtistView.Services;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NumberArtistView
{
    public partial class MainPage : ContentPage
    {
        private PolylineDrawable polylineDrawable;
        private object dxfFileEntry;
        private readonly DatabaseService _databaseService;
        private readonly Guid _userId;
        public ImageSource? BackgroundImage { get; set; }


        public MainPage(DatabaseService databaseService)
        {
            InitializeComponent(); // This now correctly loads the UI from MainPage.xaml
            ResourceAccess ra = new ResourceAccess();
            BindingContext = this;
           
            OnPropertyChanged(nameof(BackgroundImage));
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

            //try
            //{
            //    // Run the diagnostic for the specific id
            //    var ra = new NumberArtistView.Services.ResourceAccess();
            //    await ra.DiagnoseReferenceDrawingLookupAsync(1037929613);
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine($"Diagnose call threw: {ex.Message}");
            //}

            //   Task.Run(async () => await _databaseService.CopyFilesFromServerToLocalDb);


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

            Debug.Write(@"here at {0}", DateTime.Now.ToString());

            var dxfFiles = await _databaseService.GetDxfFilesAsync(_userId);


            var entryFiles = dxfFiles.DistinctBy<DxfFileEntry, string>(x => x.ResourceName).ToList();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DxfFilePicker.ItemsSource = entryFiles;
                DxfFilePicker.ItemDisplayBinding = new Binding("Name");

                if (dxfFiles.Any())
                {
                    DxfFilePicker.SelectedIndex = 0;
                }
            });
        }

        // Extracted method to address S1199
        private async Task ProcessDxfFileEntryAsync(DxfFileEntry dxfFileEntry)
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
                      
                        LayerColour = new NumberArtistView.Models.LayerColorObject() { R = 255, G = 0, B = 0 }, // You might want to set actual color values here
                      
                        
                        Vertices = pline.Vertexes.Select(v => new VertexModel
                        {
                            X = v.Position.X,
                            Y = v.Position.Y,
                            Bulge = v.Bulge
                        }).ToList()
                    }).ToList();

                    var layers = polylineDrawable.Layers.OrderBy(l => l).ToList();
                    var layers2 = layers.Select((ln, idx) => new LayerItem
                    {
                        LayerName = ln,
                        LayerIndex = idx + 1,
                        color = Microsoft.Maui.Graphics.Colors.Black,
                        IsVisible = true
                    }).ToList();

                    foreach (var li in layers2)
                        li.PropertyChanged += LayerItem_PropertyChanged;

                    LayersListView.ItemsSource = layers2;

                    if (layers2.Any())
                    {
                        LayersListView.SelectedItem = layers2[0];
                    }
                    else
                    {
                        polylineDrawable.SelectedLayer = null;
                    }

                    // initialize drawable visible layers from list
                    polylineDrawable.VisibleLayers = layers2.Where(x => x.IsVisible).Select(x => x.LayerName).ToHashSet();

                    GraphicsView.Invalidate();
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
        // Changed to return Task so callers can await and avoid blocking UI thread
        private async Task LoadBackgroundResourceData(DxfFileEntry dxfFile)
        {

            if (dxfFile == null)
            {
                await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Error", "Invalid resource Id.", "OK"));
                return;
            }

            ResourceAccess resourceaccess = new ResourceAccess();


            var backgroundFile = await resourceaccess.GetBackgroundResourceAsync(dxfFile.ReferenceDrawingId);
            //hhhh
            //
            if (string.IsNullOrEmpty(backgroundFile))
            {
                await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Error", "Failed to load background resource.", "OK"));
                return;
            }
            if(backgroundFile != null)
            { 
           
}
         


            byte[] array = Encoding.ASCII.GetBytes(backgroundFile);

            using (var stream = new MemoryStream(array))
            {
                // Load the background image from the stream
                BackgroundImage = ImageSource.FromStream(() => stream);
         
              
                // ImageBrush is not accessible; set background color as a fallback
                GraphicsView.BackgroundColor = Colors.Transparent;
                // If you want to display the image, use an Image control in your layout.
            }
        }
        // Plan / Pseudocode (detailed):
        // 1. Validate input 'resourceName' and request bytes from ResourceAccess.
        // 2. If returned byte[] is null or empty, show an alert on main thread and return.
        // 3. Copy byte[] into a local buffer to avoid accidental mutation and to be safe across threads.
        // 4. Offload DXF parsing to a background thread with Task.Run:
        //    - Create a MemoryStream from the buffer on that background thread.
        //    - Call DxfDocument.Load(stream) inside the Task.Run so UI thread is not blocked.
        //    - Return the loaded DxfDocument (or null if loading failed).
        private async Task LoadDxfData(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Error", "Invalid resource name.", "OK"));
                return;
            }

            var resourceaccess = new ResourceAccess();

            try
            {
                var dxfFile = await resourceaccess.GetResourceAsync(resourceName);

                // Validate buffer
                if (dxfFile == null || dxfFile.Length == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Error", "DXF resource is empty or could not be loaded.", "OK"));
                    return;
                }

                // Make a local copy of the bytes to be safe across threads
                var buffer = new byte[dxfFile.Length];
                Array.Copy(dxfFile, buffer, dxfFile.Length);

                // Load the DXF document on a background thread to avoid blocking the UI
                DxfDocument loaded = null!;
                try
                {
                    loaded = await Task.Run(() =>
                    {
                        using (var ms = new MemoryStream(buffer, writable: false))
                        {
                            // DxfDocument.Load can be CPU bound; run on background thread
                            return DxfDocument.Load(ms);
                        }
                    });
                }
                catch (Exception bgEx)
                {
                    // If parsing fails on background thread, surface error to UI
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Error", $"Error parsing DXF file: {bgEx.Message}", "OK"));
                    return;
                }

                if (loaded == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Error", "Error loading DXF file (DxfDocument.Load returned null).", "OK"));
                    return;
                }

                // Prepare polylines and layers on background thread to minimize UI work
                var plines = loaded.Entities.Polylines2D.Select(pline => new Pline2DModel
                {
                    IsClosed = pline.IsClosed,
                    Layer = pline.Layer?.Name ?? string.Empty,
                    LayerColour = GetColourSafe(pline.Layer?.Color.Index ?? 0),
                    Vertices = pline.Vertexes.Select(v => new VertexModel
                    {
                        X = v.Position.X,
                        Y = v.Position.Y,
                        Bulge = v.Bulge,
                    }).ToList()
                }).ToList();

                var layers = plines.Select(p => p.Layer ?? string.Empty).Distinct().OrderBy(l => l).ToList();

                // Build LayerItem list (no UI assignment yet)
                var layers2 = new List<LayerItem>();
                var colourSelectionList = new ColourSelectionList();
                var fallback = Microsoft.Maui.Graphics.Colors.Black;

                for (int i = 0; i < layers.Count; i++)
                {
                    var layerName = layers[i];

                    var selectedColor = (colourSelectionList.Selection != null && colourSelectionList.Selection.Count > i)
                        ? colourSelectionList.Selection[i]
                        : fallback;

                    var layerColourObj = new NumberArtistView.Models.LayerColorObject
                    {
                        R = (int)(selectedColor.Red * 255),
                        G = (int)(selectedColor.Green * 255),
                        B = (int)(selectedColor.Blue * 255),
                        A = (int)(selectedColor.Alpha * 255)
                    };

                    layers2.Add(new LayerItem
                    {
                        color = Color.FromRgb(layerColourObj.R, layerColourObj.G, layerColourObj.B),
                        LayerIndex = i + 1,
                        LayerName = layerName,
                        IsVisible = true,
                        LayerColour = layerColourObj
                    });
                }

                // Marshal final UI updates to main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Assign polylines
                    polylineDrawable.Polylines = plines;

                    // Ensure SelectedLayer is never set to null (use empty string when none)
                    polylineDrawable.SelectedLayer = layers2.Any() ? layers2[0].LayerName : string.Empty;

                    // Wire up PropertyChanged handlers and set ItemsSource
                    foreach (var li in layers2)
                    {
                        li.PropertyChanged += LayerItem_PropertyChanged;
                    }

                    LayersListView.ItemsSource = layers2;

                    // initialize drawable visible layers from list
                    polylineDrawable.VisibleLayers = layers2.Where(x => x.IsVisible).Select(x => x.LayerName).ToHashSet();

                    if (layers2.Any())
                    {
                        LayersListView.SelectedItem = layers2[0];
                    }
                    else
                    {
                        polylineDrawable.SelectedLayer = string.Empty;
                    }

                    // Request redraw on UI thread
                    GraphicsView.Invalidate();
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Error", $"An error occurred while loading the DXF file: {ex.Message}", "OK"));
            }

            // Local helper to safely return a Color-like object used in the project (keeps parity with GetColour)
            static Color GetColourSafe(short index)
            {
                // Reuse existing GetColour logic if available on instance; this static fallback mirrors GetColour behavior.
                var colourSelectionList = new ColourSelectionList();
                var fallback = Microsoft.Maui.Graphics.Colors.Black;
                var selectedColor = (colourSelectionList.Selection != null && colourSelectionList.Selection.Count > index)
                    ? colourSelectionList.Selection[index]
                    : fallback;

                var layerColourObj = new NumberArtistView.Models.LayerColorObject
                {
                    R = (int)(selectedColor.Red * 255),
                    G = (int)(selectedColor.Green * 255),
                    B = (int)(selectedColor.Blue * 255),
                    A = (int)(selectedColor.Alpha * 255)
                };

                return Color.FromRgb(layerColourObj.R, layerColourObj.G, layerColourObj.B);
            }
        }
        // Add this method to fix XC0002
        private double currentScale = 1;
        private double startScale = 1;
        private double xOffset;
        private double yOffset;

        private void PinchGestureRecognizer_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            // Handle pinch gesture here
            // Example: You can use e.Scale, e.Status, etc.
            if (e.Status == GestureStatus.Started)
            {
                startScale = Content.Scale;
                Content.AnchorX = 0;
                Content.AnchorY = 0;
            }

            if (e.Status == GestureStatus.Running)
            {
                // Calculate the scale factor to be applied.
                currentScale += (e.Scale - 1) * startScale;
                currentScale = Math.Max(1, currentScale);

                // The ScaleOrigin is in relative coordinates to the wrapped user interface element,
                // so get the X pixel coordinate.
                var renderedX = Content.X + xOffset;
                var deltaX = renderedX / Width;
                var deltaWidth = Width / (Content.Width * startScale);
                var originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

                // The ScaleOrigin is in relative coordinates to the wrapped user interface element,
                // so get the Y pixel coordinate.
                var renderedY = Content.Y + yOffset;
                var deltaY = renderedY / Height;
                var deltaHeight = Height / (Content.Height * startScale);
                var originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

                // Calculate the transformed element pixel coordinates.
                var targetX = xOffset - ((originX * Content.Width) * (currentScale - startScale));
                var targetY = yOffset - ((originY * Content.Height) * (currentScale - startScale));

                // Apply translation based on the change in origin.
                Content.TranslationX = Math.Clamp(targetX, -Content.Width * (currentScale - 1), 0);
                Content.TranslationY = Math.Clamp(targetY, -Content.Height * (currentScale - 1), 0);

                // Apply scale factor
                Content.Scale = currentScale;
            }

            if (e.Status == GestureStatus.Completed)
            {
                // Store the translation delta's of the wrapped user interface element.
                xOffset = Content.TranslationX;
                yOffset = Content.TranslationY;
            }
        }
        
        private void ColorBox_Tapped(object sender, TappedEventArgs e)
        {
            // Event handling logic goes here
        }

        // Make event handler async so we can await database calls instead of blocking
        private async void DxfFilePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DxfFilePicker.SelectedItem is DxfFileEntry selectedFile)
            {
                _ = LoadDxfData(selectedFile.ResourceName);

                // Use the injected database service and ensure it's initialized
                await _databaseService.InitializeAsync();

                var dxffile = await _databaseService.GetDxfFileByNameAsync(selectedFile.ResourceName);
                if (dxffile != null)
                {
                    await LoadBackgroundResourceData(dxffile);
                }

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

        private void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
        {

        }

        // common handler that updates drawable visible set when a layer's IsVisible changes
        private void LayerItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerItem.IsVisible))
            {
                var items = LayersListView.ItemsSource as IEnumerable<LayerItem>;
                if (items != null)
                {
                    polylineDrawable.VisibleLayers = items.Where(x => x.IsVisible).Select(x => x.LayerName).ToHashSet();
                    GraphicsView.Invalidate();
                }
            }
        }
      
        private void AllLayersCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            LoadList( e.Value);
             
        }

        private void LoadList(bool IsVisible)
        {
            var layers = polylineDrawable.Layers.OrderBy(l => l).ToList();
            var layers2 = new List<LayerItem>();

            var colourSelectionList = new ColourSelectionList();
            // Ensure there's at least a fallback color (black) if selection is empty
            var fallback = Microsoft.Maui.Graphics.Colors.Black;

            for (int i = 0; i < layers.Count; i++)
            {
                var layerName = layers[i];

                // Determine colour by ordinal position (index)
                var selectedColor = (colourSelectionList.Selection != null && colourSelectionList.Selection.Count > i)
                    ? colourSelectionList.Selection[i]
                    : fallback;

                // Convert MAUI Color (0..1) to 0..255 ints for LayerColorObject
                var layerColourObj = new NumberArtistView.Models.LayerColorObject
                {
                    R = (int)(selectedColor.Red * 255),
                    G = (int)(selectedColor.Green * 255),
                    B = (int)(selectedColor.Blue * 255),
                    A = (int)(selectedColor.Alpha * 255)
                };

                layers2.Add(new LayerItem
                {
                    color = Color.FromRgb(layerColourObj.R, layerColourObj.G, layerColourObj.B),
                    LayerIndex = i + 1,
                    LayerName = layerName,
                    IsVisible = IsVisible
                });
            }

            // Wire up property changed so toggling boxes updates drawable and view
            foreach (var li in layers2)
            {
                li.PropertyChanged += LayerItem_PropertyChanged;
            }

            LayersListView.ItemsSource = layers2;
        }

        private async void DiagnoticsButton_Clicked(object sender, EventArgs e)
        {

            try
            {
                var ra = new ResourceAccess();
                await ra.DiagnoseReferenceDrawingLookupAsync(1037929613);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Optional UI feedback that the diagnostic ran.
                    DisplayAlert("Diagnostics", "DiagnoseReferenceDrawingLookupAsync completed. See Debug output.", "OK");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Diagnostics button error: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DisplayAlert("Error", $"Diagnostics failed: {ex.Message}", "OK");
                });
            }
        }
    }
}