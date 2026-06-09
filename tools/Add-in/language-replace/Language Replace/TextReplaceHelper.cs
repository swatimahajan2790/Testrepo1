using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Language_Replace
{
    public static class TextReplaceHelper
    {
        private static (Dictionary<string, List<ElementId>> index, int total) BuildNormalizedIndex(UIDocument uiDoc)
        {
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            var doc = uiDoc.Document;

            var index = new Dictionary<string, List<ElementId>>(StringComparer.OrdinalIgnoreCase);
            int total = 0;

            var notes = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNote))
                .Cast<TextNote>();

            foreach (var tn in notes)
            {
                total++;
                var txt = tn.Text;
                if (string.IsNullOrEmpty(txt)) continue;

                var norm = NormalizeKey(txt);
                if (string.IsNullOrWhiteSpace(norm)) continue;

                if (!index.TryGetValue(norm, out var list))
                {
                    list = new List<ElementId>(capacity: 2);
                    index[norm] = list;
                }
                list.Add(tn.Id);
            }

            return (index, total);
        }
        private static string? NormalizeKey(string? s)
        {
            if (s == null) return null;
            s = s.Normalize(System.Text.NormalizationForm.FormKC);
            s = s.Replace('\u00A0', ' ');
            s = s.Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            s = s.Trim();
            return s;
        }
        public static (int replaced, int inspected) ReplaceAllTextNotes(
            UIDocument uiDoc,
            Dictionary<string, string> translations,
            bool combineWithOriginal)
        {
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            if (translations == null) throw new ArgumentNullException(nameof(translations));

            var doc = uiDoc.Document;

            var normDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in translations)
            {
                var k = NormalizeKey(kv.Key);
                if (!string.IsNullOrWhiteSpace(k))
                    normDict[k] = kv.Value ?? "";
            }

            var textNotes = new FilteredElementCollector(doc).OfClass(typeof(TextNote)).Cast<TextNote>().ToList();
            int replaced = 0, inspected = textNotes.Count;

            using (var tx = new Transaction(doc, "Replace TextNotes"))
            {
                tx.Start();
                foreach (var tn in textNotes)
                {
                    var current = tn.Text;
                    if (string.IsNullOrEmpty(current)) continue;

                    var norm = NormalizeKey(current);
                    if (string.IsNullOrWhiteSpace(norm)) continue;

                    if (normDict.TryGetValue(norm, out var translationValue))
                    {
                        if (string.IsNullOrWhiteSpace(translationValue)) continue;

                        string newText = combineWithOriginal
                            ? $"{current}/{translationValue}"
                            : translationValue;

                        if (!string.Equals(newText, current, StringComparison.Ordinal))
                        {
                            tn.Text = newText;
                            replaced++;
                        }
                    }
                }
                tx.Commit();
            }

            return (replaced, inspected);
        }
        public static (int replaced, int inspected) ReplaceSelectedTextNotes(
            UIDocument uiDoc,
            Dictionary<string, string> translations,
            bool combineWithOriginal)
        {
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            if (translations == null) throw new ArgumentNullException(nameof(translations));

            var sel = uiDoc.Selection;
            var ids = sel.GetElementIds();
            var doc = uiDoc.Document;

            var normDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in translations)
            {
                var k = NormalizeKey(kv.Key);
                if (!string.IsNullOrWhiteSpace(k))
                    normDict[k] = kv.Value ?? "";
            }

            var textNotes = ids.Select(id => doc.GetElement(id)).OfType<TextNote>().ToList();
            int replaced = 0, inspected = textNotes.Count;

            using (var tx = new Transaction(doc, "Replace Selected TextNotes"))
            {
                tx.Start();
                foreach (var tn in textNotes)
                {
                    var current = tn.Text;
                    if (string.IsNullOrEmpty(current)) continue;

                    var norm = NormalizeKey(current);
                    if (string.IsNullOrWhiteSpace(norm)) continue;

                    if (normDict.TryGetValue(norm, out var translationValue))
                    {
                        if (string.IsNullOrWhiteSpace(translationValue)) continue;

                        string newText = combineWithOriginal
                            ? $"{current}/{translationValue}"
                            : translationValue;

                        if (!string.Equals(newText, current, StringComparison.Ordinal))
                        {
                            tn.Text = newText;
                            replaced++;
                        }
                    }
                }
                tx.Commit();
            }

            return (replaced, inspected);
        }
        public static (int replaced, int inspected) ReplaceNotesPresentInExcel(
            UIDocument uiDoc,
            Dictionary<string, string> translations,
            bool combineWithOriginal)
        {
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            if (translations == null) throw new ArgumentNullException(nameof(translations));

            var doc = uiDoc.Document;

            var normDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in translations)
            {
                var k = NormalizeKey(kv.Key);
                if (!string.IsNullOrWhiteSpace(k))
                    normDict[k] = kv.Value ?? "";
            }
            var keySet = new HashSet<string>(normDict.Keys, StringComparer.OrdinalIgnoreCase);

            var textNotes = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNote))
                .Cast<TextNote>()
                .Where(tn =>
                {
                    if (string.IsNullOrEmpty(tn.Text)) return false;
                    var norm = NormalizeKey(tn.Text);
                    return !string.IsNullOrWhiteSpace(norm) && keySet.Contains(norm);
                })
                .ToList();

            int replaced = 0, inspected = textNotes.Count;

            using (var tx = new Transaction(doc, "Replace Notes Present In Excel"))
            {
                tx.Start();
                foreach (var tn in textNotes)
                {
                    var current = tn.Text;
                    if (string.IsNullOrEmpty(current)) continue;

                    var norm = NormalizeKey(current);
                    if (string.IsNullOrWhiteSpace(norm)) continue;

                    if (normDict.TryGetValue(norm, out var translationValue))
                    {
                        if (string.IsNullOrWhiteSpace(translationValue)) continue;

                        string newText = combineWithOriginal
                            ? $"{current}/{translationValue}"
                            : translationValue;

                        if (!string.Equals(newText, current, StringComparison.Ordinal))
                        {
                            tn.Text = newText;
                            replaced++;
                        }
                    }
                }
                tx.Commit();
            }

            return (replaced, inspected);
        }
        public static List<string> GetPresentKeys(UIDocument uiDoc, IEnumerable<string> keys)
        {
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            static string? NormalizeKey(string? s)
            {
                if (s == null) return null;

                s = s.Normalize(System.Text.NormalizationForm.FormKC);

                s = s.Replace('\u00A0', ' ');

                s = s.Replace("\u200B", "")
                     .Replace("\u200C", "")
                     .Replace("\u200D", "")
                     .Replace("\uFEFF", "");
                s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");

                s = s.Trim();

                return s;
            }

            var doc = uiDoc.Document;

            var presentNorm = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var textNotes = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNote))
                .Cast<TextNote>();

            foreach (var tn in textNotes)
            {
                var txt = tn.Text;
                if (string.IsNullOrEmpty(txt)) continue;

                var norm = NormalizeKey(txt);
                if (!string.IsNullOrWhiteSpace(norm))
                    presentNorm.Add(norm);
            }

            var result = new List<string>();
            foreach (var k in keys)
            {
                var normKey = NormalizeKey(k);
                if (!string.IsNullOrWhiteSpace(normKey) && presentNorm.Contains(normKey))
                    result.Add(k);
            }

            return result;
        }
        public static Dictionary<string, List<ElementId>> MapKeysToElementIds(UIDocument uiDoc, IEnumerable<string> keys)
        {
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            var (index, _) = BuildNormalizedIndex(uiDoc);

            var result = new Dictionary<string, List<ElementId>>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in keys)
            {
                var normKey = NormalizeKey(k);
                if (string.IsNullOrWhiteSpace(normKey)) continue;

                if (index.TryGetValue(normKey, out var ids) && ids != null && ids.Count > 0)
                {
                    result[k] = new List<ElementId>(ids);
                }
            }
            return result;
        }
        public static (int replaced, int inspected) ReplaceByIds(
            UIDocument uiDoc,
            IEnumerable<ElementId> ids,
            Dictionary<string, string> translations)
        {
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc));
            if (translations == null) throw new ArgumentNullException(nameof(translations));

            var doc = uiDoc.Document;

            var normDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in translations)
            {
                var k = NormalizeKey(kv.Key);
                if (!string.IsNullOrWhiteSpace(k))
                    normDict[k] = kv.Value ?? "";
            }

            var idList = ids?.Distinct().ToList() ?? new List<ElementId>();
            int inspected = idList.Count;
            int replaced = 0;

            using (var tx = new Transaction(doc, "Replace Selected (Grid) TextNotes"))
            {
                tx.Start();
                foreach (var id in idList)
                {
                    if (doc.GetElement(id) is TextNote tn)
                    {
                        var current = tn.Text;
                        if (string.IsNullOrEmpty(current)) continue;

                        var norm = NormalizeKey(current);
                        if (string.IsNullOrWhiteSpace(norm)) continue;

                        if (normDict.TryGetValue(norm, out var newText) && !string.IsNullOrEmpty(newText))
                        {
                            if (!string.Equals(current, newText, StringComparison.Ordinal))
                            {
                                tn.Text = newText;
                                replaced++;
                            }
                        }
                    }
                }
                tx.Commit();
            }

            return (replaced, inspected);
        }
    }
}