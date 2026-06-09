using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace Language_Replace
{
    public class ApplyHandler
    {
        public UIDocument? UiDoc { get; set; }
        public Data? Data { get; set; }
        public void Execute(UIApplication app)
        {
            try
            {
                if (UiDoc == null || Data == null) return;

                var entries = Data.Translations.Select(t => new { t.Original, t.Translation }).ToList();
                if (entries.Count == 0)
                {
                    TaskDialog.Show("Language Replace", "No translations available.");
                    return;
                }

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in entries)
                {
                    var original = e.Original ?? "";
                    var translation = e.Translation ?? "";
                    dict[original] = translation;
                }
                bool combine = Data.SelectedOption == Data.ReplacementOption.ReplaceToExistingOrSecond;

                (int replaced, int inspected) result = (0, 0);

                switch (Data.SelectedScope)
                {
                    case Data.ReplacementScope.AllInModel:
                        result = TextReplaceHelper.ReplaceAllTextNotes(UiDoc, dict, combine);
                        break;

                    case Data.ReplacementScope.SelectedOnly:
                        {
                            var chosenIds = Data.Translations
                                .Where(t => t.Selected && t.ElementIds.Count > 0)
                                .SelectMany(t => t.ElementIds)
                                .Distinct()
                                .ToList();

                            if (chosenIds.Count == 0)
                            {
                                TaskDialog.Show("Language Replace", "No rows selected in the grid.");
                                return;
                            }

                            result = TextReplaceHelper.ReplaceByIds(UiDoc, chosenIds, dict);
                            break;
                        }

                    case Data.ReplacementScope.PresentInExcelOnly:
                        result = TextReplaceHelper.ReplaceNotesPresentInExcel(UiDoc, dict, combine);
                        break;
                }

                var msg = $"Replaced {result.replaced} of {result.inspected} text notes.";
                TaskDialog.Show("Language Replace", msg);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Language Replace", ex.Message);
            }
        }

        public string GetName()
        {
            return "ApplyHandler";
        }
    }
}