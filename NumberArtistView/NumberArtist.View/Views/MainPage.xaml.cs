using Core.Business.Objects;
using Core.Business.Objects.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using netDxf;
using Newtonsoft.Json;
using NumberArtist.View.Views;
using NumberArtistView.Models;
using NumberArtistView.NumberArtist.View.ViewModels;
using NumberArtistView.Services;
using System.Collections.ObjectModel;
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

        // Add the Vertices property as an ObservableCollection
        public ObservableCollection<Pline2DModel> Shapes { get; set; } = new ObservableCollection<Pline2DModel>();

        // Collection that can be bound to UI: each item contains the Layer name and the list of closed polylines for that layer.
        public ObservableCollection<LayerClosedGroup> VisibleClosedGroups { get; set; } = new ObservableCollection<LayerClosedGroup>();


        public ObservableCollection<LayerGroup> LayerGroup { get; set; } = new ObservableCollection<LayerGroup>();
        public LayerClosedGroup SelectedClosedGroup { get; set; }
        private string _currentDxfFileName;

        private double currentScale = 1;
        private double startScale = 1;
        private double xOffset;
        private double yOffset;
        private Array bytes;
        public MainPage(DatabaseService databaseService)
        {
            InitializeComponent();
            ResourceAccess ra = new ResourceAccess();
            BindingContext = this;

            OnPropertyChanged(nameof(BackgroundImage));
            _databaseService = databaseService;

            var userIdString = Preferences.Get("userId", string.Empty);
            if (!Guid.TryParse(userIdString, out _userId))
            {
                _userId = Guid.Empty;
            }

            polylineDrawable = new PolylineDrawable();
            this.GraphicsView.Drawable = (IDrawable)polylineDrawable;
            LayersListView.SelectionChanged += LayerPicker_SelectionChanged;

            // Remove fire-and-forget Task.Run
        }

        private void LayerPicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        // Rebuilds VisibleClosedGroups from the current polylineDrawable state and VisibleLayers set.
        // Runs updates on the main/UI thread to safely update the ObservableCollection.
        private void UpdateVisibleClosedGroups()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                VisibleClosedGroups.Clear();

                if (polylineDrawable == null || polylineDrawable.Polylines == null || !polylineDrawable.Polylines.Any())
                {
                    // Ensure the ClosedGroupsListView is bound (empty) so the view shows nothing instead of stale data.
                    ClosedGroupsListView.ItemsSource = VisibleClosedGroups;
                    OnPropertyChanged(nameof(VisibleClosedGroups));
                    return;
                }

                var visibleSet = polylineDrawable.VisibleLayers;

                var groups = polylineDrawable.Polylines
                    .Where(p => p != null && p.IsClosed && (visibleSet == null || visibleSet.Contains(p.Layer)))
                    .GroupBy(p => p.Layer ?? string.Empty)
                    .OrderBy(g => g.Key, StringComparer.Ordinal);

                foreach (var g in groups)
                {
                    var list = g.ToList();

                    VisibleClosedGroups.Add(new LayerClosedGroup
                    {
                        LayerName = g.Key,
                        ClosedPlines = list,
                        ClosedPlinesCount = list.Count
                    });
                }

                // Bind the ClosedGroupsListView to the VisibleClosedGroups collection so the view shows the groups.
                ClosedGroupsListView.ItemsSource = VisibleClosedGroups;

                OnPropertyChanged(nameof(VisibleClosedGroups));
            });
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


            await Task.Run(async () => await LoadDxfFilesIntoPicker());

            // You can also await the other tasks here if needed
            var recordCount = await _databaseService.GetRecordCount();
            var files = await _databaseService.GetDxfFilesAsync(_userId);
            // Now 'files' will contain the list of DxfFileEntry objects
            Debug.WriteLine($"Record count: {recordCount}");   
        }

        private async Task LoadDxfFilesIntoPicker()
        {
            if (_userId == Guid.Empty)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => await DisplayAlertAsync("Error", "Could not determine user.", "OK"));
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
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlertAsync("Error", "DXF file data is empty.", "OK");
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
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await DisplayAlertAsync("Error", "Error loading DXF file.", "OK");
                        });
                        return;
                    }

                    polylineDrawable.Polylines = loaded.Entities.Polylines2D.Select(pline => new Pline2DModel
                    {
                        IsClosed = pline.IsClosed,
                        Layer = pline.Layer.Name,
                        LayerColour = new NumberArtistView.Models.LayerColorObject() { R = 255, G = 0, B = 0 },
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
                        polylineDrawable.SelectedLayer = layers2[0].LayerName ?? string.Empty;
                    }
                    else
                    {
                        polylineDrawable.SelectedLayer = string.Empty;
                    }

                    // initialize drawable visible layers from list
                    polylineDrawable.VisibleLayers = layers2.Where(x => x.IsVisible).Select(x => x.LayerName).ToHashSet();

                    // Populate the Vertices collection for the selected layer
                    await UpdateVerticesForSelectedLayer(polylineDrawable.SelectedLayer);

                    GraphicsView.Invalidate();
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlertAsync("Error", $"An error occurred while loading the DXF file: {ex.Message}", "OK");
                });
            }

            UpdateVisibleClosedGroups();
        }



        /// <summary>
        /// Update the ObservableCollection `Vertices` with VertexViewModel entries for the specified layer.
        /// If layerName is null/empty, the collection will be cleared.
        /// This method marshals updates to the UI thread.
        /// </summary>
        private async Task UpdateVerticesForSelectedLayer(string? layerName)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Shapes.Clear();

                if (string.IsNullOrWhiteSpace(layerName) || polylineDrawable?.Polylines == null)
                {
                    // Do not change ClosedGroupsListView.ItemsSource here; leave it bound to VisibleClosedGroups.
                    return;
                }

                // Find polylines that belong to the selected layer (case sensitive match as existing code)
                var matchedPlines = polylineDrawable.Polylines
                    .Where(p => string.Equals(p.Layer, layerName, StringComparison.Ordinal))
                    .ToList();

                foreach (var pline in matchedPlines)
                {
                    Shapes.Add(pline);
                }

                // Keep Shapes updated for other UI bindings. ClosedGroupsListView remains bound to VisibleClosedGroups.
            });
        }

        // Changed to return Task so callers can await and avoid blocking UI thread
        private async Task LoadBackgroundResourceData(DxfFileEntry dxfFile)
        {
            if (dxfFile == null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => await DisplayAlertAsync("Error", "Invalid resource Id.", "OK"));
                return;
            }

            var resourceaccess = new ResourceAccess();

            // Get raw image bytes (preferred over string/base64)
            var bytes = await resourceaccess.GetReferenceImageBytesAsync(dxfFile.ReferenceDrawingId);
            if (bytes == null || bytes.Length == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => await DisplayAlertAsync("Error", "Failed to load background resource bytes.", "OK"));
                return;
            }

            // Make a defensive copy that the FromStream delegate can use later (fresh MemoryStream each call)
            var bytesCopy = new byte[bytes.Length];
            Array.Copy(bytes, bytesCopy, bytes.Length);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // ImageSource.FromStream requires the delegate to return a new, valid stream each time.
                BackgroundImage = ImageSource.FromStream(() => new MemoryStream(bytesCopy));

                // Notify bindings that BackgroundImage changed
                OnPropertyChanged(nameof(BackgroundImage));

                // Ensure GraphicsView remains transparent so the background image shows
                GraphicsView.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;

                // Request a redraw
                GraphicsView.Invalidate();
            });
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
                await MainThread.InvokeOnMainThreadAsync(async () => await DisplayAlertAsync("Error", "Invalid resource name.", "OK"));
                return;
            }

            _currentDxfFileName = resourceName;

            var resourceaccess = new ResourceAccess();

            try
            {
                var dxfFile = await resourceaccess.GetResourceAsync(resourceName);

                // Validate buffer
                if (dxfFile == null || dxfFile.Length == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () => await DisplayAlertAsync("Error", "DXF resource is empty or could not be loaded.", "OK"));
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
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                        await DisplayAlertAsync("Error", $"Error parsing DXF file: {bgEx.Message}", "OK"));
                    return;
                }

                if (loaded == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                        await DisplayAlertAsync("Error", "Error loading DXF file (DxfDocument.Load returned null).", "OK"));
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

                // Restore saved layer states
                await RestoreLayerStatesAsync(layers2);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    polylineDrawable.Polylines = plines;
                    polylineDrawable.SelectedLayer = layers2.Any() ? layers2[0].LayerName : string.Empty;

                    foreach (var li in layers2)
                    {
                        li.PropertyChanged += LayerItem_PropertyChanged;
                    }

                    LayersListView.ItemsSource = layers2;
                    polylineDrawable.VisibleLayers = layers2.Where(x => x.IsVisible).Select(x => x.LayerName).ToHashSet();

                    if (layers2.Any())
                    {
                        LayersListView.SelectedItem = layers2[0];
                    }
                    else
                    {
                        polylineDrawable.SelectedLayer = string.Empty;
                    }

                    GraphicsView.Invalidate();
                });

                UpdateVisibleClosedGroups();
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await DisplayAlertAsync("Error", $"An error occurred while loading the DXF file: {ex.Message}", "OK"));
            }

            static Color GetColourSafe(short index)
            {
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
        // Add this method to save layer states when they change
        private async Task SaveLayerStatesAsync()
        {
            if (_userId == Guid.Empty || string.IsNullOrWhiteSpace(_currentDxfFileName))
                return;

            var layers = LayersListView.ItemsSource as IEnumerable<LayerItem>;
            if (layers == null || !layers.Any())
                return;

            try
            {
                await _databaseService.SaveLayerGroupStateAsync(_userId, _currentDxfFileName, layers);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving layer states: {ex.Message}");
            }
        }

        // Add this method to save polyline states when they change
        private async Task SavePolylineStatesAsync(string layerName, IEnumerable<Pline2DModel> polylines)
        {
            if (_userId == Guid.Empty || string.IsNullOrWhiteSpace(_currentDxfFileName) || string.IsNullOrWhiteSpace(layerName))
                return;

            try
            {
                await _databaseService.SavePolylineStatesAsync(_userId, _currentDxfFileName, layerName, polylines);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving polyline states: {ex.Message}");
            }
        }

        // Add this method to restore layer states
        private async Task RestoreLayerStatesAsync(List<LayerItem> layers)
        {
            if (_userId == Guid.Empty || string.IsNullOrWhiteSpace(_currentDxfFileName) || layers == null || !layers.Any())
                return;

            try
            {
                var savedStates = await _databaseService.LoadLayerGroupStateAsync(_userId, _currentDxfFileName);
                if (!savedStates.Any())
                    return;

                var stateDict = savedStates.ToDictionary(s => s.LayerName, s => s);

                foreach (var layer in layers)
                {
                    if (stateDict.TryGetValue(layer.LayerName, out var state))
                    {
                        layer.IsVisible = state.IsVisible;
                        layer.color = Color.FromRgba(state.ColorR, state.ColorG, state.ColorB, state.ColorA);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restoring layer states: {ex.Message}");
            }
        }

        // Add this method to restore polyline states
        private async Task RestorePolylineStatesAsync(string layerName, List<Pline2DModel> polylines)
        {
            if (_userId == Guid.Empty || string.IsNullOrWhiteSpace(_currentDxfFileName) || string.IsNullOrWhiteSpace(layerName) || polylines == null || !polylines.Any())
                return;

            try
            {
                var savedStates = await _databaseService.LoadPolylineStatesAsync(_userId, _currentDxfFileName, layerName);
                if (!savedStates.Any())
                    return;

                for (int i = 0; i < Math.Min(polylines.Count, savedStates.Count); i++)
                {
                    polylines[i].IsPainted = savedStates[i].IsPainted;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restoring polyline states: {ex.Message}");
            }
        }

        // Modify LayerItem_PropertyChanged to save state when visibility changes
        private void LayerItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerItem.IsVisible))
            {
                var items = LayersListView.ItemsSource as IEnumerable<LayerItem>;
                if (items != null)
                {
                    polylineDrawable.VisibleLayers = items.Where(x => x.IsVisible).Select(x => x.LayerName).ToHashSet();

                    // Update grouped visible closed polylines
                    UpdateVisibleClosedGroups();

                    GraphicsView.Invalidate();

                    // Save state to database
                    _ = SaveLayerStatesAsync();
                }
            }

            if (e.PropertyName == nameof(LayerItem.IsVisible))
            {
                GraphicsView.Invalidate();
            }
        }



        // common handler that updates drawable visible set when a layer's IsVisible changes
        private void PlineItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Pline2DModel.IsPainted))
            {
                // Clear the list to prevent stale data from persisting
                SelectedPlinesListView.ItemsSource = null;
                GraphicsView.Invalidate();
                var items = SelectedPlinesListView.ItemsSource as IEnumerable<Pline2DModel>;
                if (items != null)
                {
                    GraphicsView.Invalidate();

                    // Save polyline states
                    if (sender is Pline2DModel pline && !string.IsNullOrWhiteSpace(pline.Layer))
                    {
                        _ = SavePolylineStatesAsync(pline.Layer, items);
                    }
                }
            }

            if (e.PropertyName == nameof(Pline2DModel.IsPainted))
            {
                GraphicsView.Invalidate();
            }
        }

        private void AllLayersCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            LoadList(e.Value);

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
        // Add this method to handle the ItemSelected event for LayersListView
        private void LayerPicker_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is LayerItem selectedLayer)
            {
                polylineDrawable.SelectedLayer = selectedLayer.LayerName ?? string.Empty;
                _ = UpdateVerticesForSelectedLayer(polylineDrawable.SelectedLayer);
                GraphicsView.Invalidate();
            }
        }
        // Add this method to fix XC0002
        private void DxfFilePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Add your logic for when the selected index changes


            if (DxfFilePicker.SelectedItem is DxfFileEntry selectedDxfFile)
            {
                // Load the DXF data for the selected file
                _ = LoadDxfData(selectedDxfFile.ResourceName);
                // Load the background resource data
                _ = LoadBackgroundResourceData(selectedDxfFile);
            }
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
                await DisplayAlertAsync("Error", $"An error occurred while selecting the DXF file: {ex.Message}", "OK");
            }
        }
        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {

            Color chColor = null;


            var box = (BoxView)sender;

            switch (e.Direction)
            {
                case SwipeDirection.Right:
                    chColor = Colors.Red;
                    break;
            }

        }

        private void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (e.StatusType == GestureStatus.Running)
            {
                // Update the position of the content based on the pan gesture
                Content.TranslationX += e.TotalX;
                Content.TranslationY += e.TotalY;
            }
        }

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

        private async void ClosedGroupsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var groupsListView = sender as CollectionView;
            if (groupsListView?.SelectedItem is LayerClosedGroup selectedGroup)
            {
                // Restore polyline states before binding
                await RestorePolylineStatesAsync(selectedGroup.LayerName, selectedGroup.ClosedPlines);
                
                SelectedPlinesListView.ItemsSource = selectedGroup.ClosedPlines;
                
                // Wire up property changed for each polyline to track IsPainted changes
                foreach (var pline in selectedGroup.ClosedPlines)
                {
                    pline.PropertyChanged -= PlineItem_PropertyChanged; // Remove first to avoid duplicates
                    pline.PropertyChanged += PlineItem_PropertyChanged;
                }
                
                // Force the ListView to refresh
                SelectedPlinesListView.SelectedItem = null;
            }
        }

        private void LogoutButton_Clicked(object sender, EventArgs e)
        {
            // Clear user session data
            Preferences.Clear("UserId");

            // Navigate back to the LoginPage using the recommended approach
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new LoginPage();
            }
        }
        //private async void DiagnoticsButton_Clicked(object sender, EventArgs e)
        //{

        //    try
        //    {
        //        var ra = new ResourceAccess();
        //        await ra.DiagnoseReferenceDrawingLookupAsync(1037929613);
        //        await MainThread.InvokeOnMainThreadAsync(() =>
        //        {
        //            // Optional UI feedback that the diagnostic ran.
        //            DisplayAlert("Diagnostics", "DiagnoseReferenceDrawingLookupAsync completed. See Debug output.", "OK");
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Diagnostics button error: {ex.Message}");
        //        await MainThread.InvokeOnMainThreadAsync(() =>
        //        {
        //            DisplayAlert("Error", $"Diagnostics failed: {ex.Message}", "OK");
        //        });
        //    }
        //}

        private async void PlinePainted_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.BindingContext is Pline2DModel pline)
            {
                // The IsPainted property should already be updated via binding
                // Now save the state to the database
                if (!string.IsNullOrWhiteSpace(pline.Layer))
                {
                    var items = SelectedPlinesListView.ItemsSource as IEnumerable<Pline2DModel>;
                    if (items != null && items.Any())
                    {
                        await SavePolylineStatesAsync(pline.Layer, items);
                        GraphicsView.Invalidate();
                    }
                }
            }
        }

   
    }


}