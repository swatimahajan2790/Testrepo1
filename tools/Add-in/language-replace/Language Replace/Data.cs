using Autodesk.Revit.DB;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Language_Replace
{
    public class TranslationEntry
    {
        public bool Selected { get; set; } 
        public string Original { get; set; }
        public string Translation { get; set; }
        public List<ElementId> ElementIds { get; set; } = new List<ElementId>();
    }
    public class Data : INotifyPropertyChanged
    {
        public enum ReplacementOption
        {
            ReplaceToSecondLanguage,
            ReplaceToExistingOrSecond
        }

        public enum ReplacementScope
        {
            AllInModel,
            SelectedOnly,         
            PresentInExcelOnly    
        }
        private string _excelFilePath = "";
        private string _selectedLanguage = "";

        private ReplacementScope _selectedScope = ReplacementScope.SelectedOnly;
        public ReplacementScope SelectedScope
        {
            get => _selectedScope;
            set { _selectedScope = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> AvailableLanguages { get; } = new ObservableCollection<string>();
        public ObservableCollection<TranslationEntry> Translations { get; } = new ObservableCollection<TranslationEntry>();
        public ReplacementOption SelectedOption { get; set; } = ReplacementOption.ReplaceToSecondLanguage;

        public string ExcelFilePath
        {
            get => _excelFilePath;
            set { _excelFilePath = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
