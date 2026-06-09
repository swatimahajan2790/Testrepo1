using Autodesk.Revit.UI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Language_Replace
{
    public partial class Main : Window
    {
        private readonly UIDocument _uidoc;
        private readonly Data _data;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Main(Data data, UIDocument uidoc)
        {
            InitializeComponent();
            _uidoc = uidoc;
            _data = data;

            DataContext = _data;

            Option1Radio.IsChecked = true;

            ScopeSelectedRadio.IsChecked = true;

        }


        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Translation File",
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var filePath = openFileDialog.FileName;
                    _data.ExcelFilePath = filePath;

                    try
                    {

                        var translations = ExcelTranslationReader.ReadTranslationsFromExcel(filePath);

                        var filtered = FilterToNotesPresentInModel(translations);

                        var keyMap = TextReplaceHelper.MapKeysToElementIds(_uidoc, filtered.Select(f => f.Original));

                        _data.Translations.Clear();
                        foreach (var t in filtered)
                        {
                            if (keyMap.TryGetValue(t.Original, out var ids) && ids != null)
                                t.ElementIds = ids;
                            else
                                t.ElementIds = new List<Autodesk.Revit.DB.ElementId>();

                            t.Selected = false;
                            _data.Translations.Add(t);
                        }

                        StatusTextBlock.Text = $"Loaded {filtered.Count} translations present in model (of {translations.Count})";
                        StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    }
                    catch (Exception ex)
                    {
                        StatusTextBlock.Text = $"Error loading file: {ex.Message}";
                        StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
        }
        private List<TranslationEntry> FilterToNotesPresentInModel(IEnumerable<TranslationEntry> source)
        {
            try
            {
                if (_uidoc == null) return source.Where(t => !string.IsNullOrWhiteSpace(t.Original)).ToList();

                var keys = source
                    .Select(t => t.Original ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                var presentKeys = TextReplaceHelper.GetPresentKeys(_uidoc, keys);
                var presentSet = new HashSet<string>(presentKeys, StringComparer.OrdinalIgnoreCase);

                return source
                    .Where(t => !string.IsNullOrWhiteSpace(t.Original) && presentSet.Contains(t.Original))
                    .ToList();
            }
            catch
            {
                return source.Where(t => !string.IsNullOrWhiteSpace(t.Original)).ToList();
            }
        }
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               
                    if (_data.Translations.Count == 0)
                    {
                        StatusTextBlock.Text = "No translations loaded.";
                        StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                        return;
                    }

                if (Option1Radio.IsChecked == true)
                    _data.SelectedOption = Data.ReplacementOption.ReplaceToSecondLanguage;
                else if (Option2Radio.IsChecked == true)
                    _data.SelectedOption = Data.ReplacementOption.ReplaceToExistingOrSecond;


                if (ScopeAllRadio.IsChecked == true)
                    _data.SelectedScope = Data.ReplacementScope.AllInModel;
                else if (ScopeSelectedRadio.IsChecked == true)
                    _data.SelectedScope = Data.ReplacementScope.SelectedOnly;
                //else if (ScopePresentInExcelRadio.IsChecked == true)
                //    _data.SelectedScope = Data.ReplacementScope.PresentInExcelOnly;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                ApplyButton.IsEnabled = true;
                BrowseButton.IsEnabled = true;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;  
            Close();
        }
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}