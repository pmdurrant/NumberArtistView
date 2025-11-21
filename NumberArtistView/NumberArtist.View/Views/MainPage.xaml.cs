using Core.Business.Objects;
using Microsoft.Maui.Controls;
using netDxf;
using Newtonsoft.Json;
using NumberArtist.View.Views;
using NumberArtistView.Models;
using NumberArtistView.Services;
using System.ComponentModel;
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

         //   Task.Run(async () => await _databaseService.CopyFilesFromServerToLocalDb());


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


            var fred = dxfFiles.DistinctBy<DxfFileEntry, string>(x => x.ResourceName).ToList();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DxfFilePicker.ItemsSource = fred;
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
                      
                        LayerColour = new LayerColorObject() { R = 255, G = 0, B = 0 }, // You might want to set actual color values here
                      
                        
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

        private async Task LoadDxfData(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                await DisplayAlert("Error", "Invalid resource name.", "OK");
                return;
            }
    
            ResourceAccess resourceaccess = new ResourceAccess();

            try
            { 
                var dxfFile = await resourceaccess.GetResourceAsync(resourceName);
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
                        LayerColour = GetColour( pline.Layer.Color.Index),
                        Vertices = pline.Vertexes.Select(v => new VertexModel
                        {
                            X = v.Position.X,
                            Y = v.Position.Y,
                            Bulge = v.Bulge,
                        }).ToList()
                    }).ToList();

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
                        var layerColourObj = new LayerColorObject
                        {
                            R = (int)(selectedColor.Red * 255),
                            G = (int)(selectedColor.Green * 255),
                            B = (int)(selectedColor.Blue * 255),
                            A = (int)(selectedColor.Alpha * 255)
                        };

                        layers2.Add(new LayerItem
                        {
                            color = Color.FromRgb(layerColourObj.R,layerColourObj.G,layerColourObj.B),
                            LayerIndex = i + 1,
                            LayerName = layerName,
                            IsVisible = true
                        });
                    }

                    // Wire up property changed so toggling boxes updates drawable and view
                    foreach (var li in layers2)
                    {
                        li.PropertyChanged += LayerItem_PropertyChanged;
                    }

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
                await DisplayAlert("Error", $"An error occurred while loading the DXF file: {ex.Message}", "OK");
            }
        }

        private Color GetColour(short index)
        {
            var fallback = Microsoft.Maui.Graphics.Colors.Black;

            var colourSelectionList = new ColourSelectionList();
            var selectedColor = (colourSelectionList.Selection != null && colourSelectionList.Selection.Count > index)
                            ? colourSelectionList.Selection[index]
                            : fallback;

            // Convert MAUI Color (0..1) to 0..255 ints for LayerColorObject
            var layerColourObj = new LayerColorObject
            {
                R = (int)(selectedColor.Red * 255),
                G = (int)(selectedColor.Green * 255),
                B = (int)(selectedColor.Blue * 255),
                A = (int)(selectedColor.Alpha * 255)
            };


            return Color.FromRgb(layerColourObj.R, layerColourObj.G, layerColourObj.B);


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
                var layerColourObj = new LayerColorObject
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
    }
}