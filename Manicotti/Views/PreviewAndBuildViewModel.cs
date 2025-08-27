using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Manicotti.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Manicotti.Views
{
    public class PreviewAndBuildViewModel : INotifyPropertyChanged, IDisposable
    {
        // ... (Properties as before) ...
        private UIApplication _uiapp;
        private Document _doc;
        private Action _closeAction;
        private RevitTaskHandler _revitTaskHandler;
        private ImportInstance _import;
        public DiagnosticsReport Report { get; private set; }
        private bool _isProcessing;
        public bool IsProcessing { get => _isProcessing; set { _isProcessing = value; OnPropertyChanged(nameof(IsProcessing)); } }
        private string _statusText;
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(nameof(StatusText)); } }
        public ICommand BuildCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveReportCommand { get; }


        public PreviewAndBuildViewModel(UIApplication uiapp, ImportInstance import, Action closeAction)
        {
            _uiapp = uiapp;
            _doc = uiapp.ActiveUIDocument.Document;
            _import = import;
            _closeAction = closeAction;
            _revitTaskHandler = new RevitTaskHandler();
            Report = new DiagnosticsReport();

            BuildCommand = new RelayCommand(p => !IsProcessing, p => BuildModel());
            CancelCommand = new RelayCommand(p => true, p => Cancel());
            SaveReportCommand = new RelayCommand(p => !IsProcessing, p => SaveReport());

            Task.Run(() => AnalyzeAndQueuePreview());
        }

        private void AnalyzeAndQueuePreview()
        {
            // ... (Analysis logic as before) ...
        }

        private bool IsLayerMatch(string layerName, string pattern)
        {
            if (pattern.EndsWith("*"))
            {
                return layerName.StartsWith(pattern.TrimEnd('*'));
            }
            return layerName == pattern;
        }

        private void SaveReport()
        {
            try
            {
                var dialog = new FileSaveDialog("JSON Files (*.json)|*.json|CSV Files (*.csv)|*.csv");
                dialog.Title = "Save Diagnostics Report";
                dialog.InitialFileName = "dwg-diagnostics-report";

                if (dialog.Show() == ItemSelectionDialogResult.Confirmed)
                {
                    string path = ModelPathUtils.ConvertModelPathToUserVisiblePath(dialog.GetSelectedModelPath());

                    // Simple JSON serialization for the report
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(Report, options);

                    File.WriteAllText(path, jsonString);
                    TaskDialog.Show("Success", "Report saved successfully to:\n" + path);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Could not save report: " + ex.Message);
            }
        }

        // ... (Rest of the methods: DrawPreviewGraphics, BuildModel, Cancel, etc.)
        private void DrawPreviewGraphics(UIApplication app, LayerMap layerMap) { /* ... */ }
        private void BuildModel() { /* ... */ }
        private void Cancel() { /* ... */ }
        public void Dispose() { }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
