using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Language_Replace;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
namespace Core
{
    [Transaction(TransactionMode.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                string domainName = Environment.UserDomainName;
                if (!domainName.ToUpper().Contains("DAR") &&
                    !domainName.ToUpper().Contains("DMSD"))
                {
                    MessageBox.Show("THIS APPLICATION IS NOT VALID OUTSIDE DAR GROUP!!",
                                    "DAR GROUP",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Stop);
                    return Result.Failed;
                }

                var uiapp = commandData.Application;
                var uidoc = uiapp.ActiveUIDocument;
                var data = new Data();

                var handler = new ApplyHandler { UiDoc = uidoc, Data = data };
                var main = new Main(data, uidoc);

                bool? dlgRes = main.ShowDialog();

                if (dlgRes != true)
                    return Result.Cancelled;

                handler.Execute(uiapp);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Language Replace", MessageBoxButton.OK, MessageBoxImage.Error);
                return Result.Failed;
            }
        }
    }
}