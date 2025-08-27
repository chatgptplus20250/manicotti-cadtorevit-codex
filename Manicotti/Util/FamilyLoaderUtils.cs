using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;

namespace Manicotti.Util
{
    public static class FamilyLoaderUtils
    {
        /// <summary>
        /// A robust method to find a family in the project or load it.
        /// 1. Tries to find the family by name in the document.
        /// 2. If not found, tries to load it from a default path.
        /// 3. If that fails, prompts the user to locate the .rfa file.
        /// </summary>
        /// <param name="doc">The active Revit document.</param>
        /// <param name="familyName">The name of the family to find or load.</param>
        /// <param name="defaultRfaPath">The default full path to the .rfa file.</param>
        /// <returns>The loaded Family, or null if all attempts fail.</returns>
        public static Family GetAndLoadFamily(Document doc, string familyName, string defaultRfaPath)
        {
            // 1. Try to find the family in the project first.
            var family = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .FirstOrDefault(f => f.Name == familyName);

            if (family != null)
            {
                return family;
            }

            // 2. If not found, try to load from the default path.
            if (!string.IsNullOrEmpty(defaultRfaPath) && System.IO.File.Exists(defaultRfaPath))
            {
                using (var tx = new Transaction(doc, $"Load Family: {familyName}"))
                {
                    tx.Start();
                    if (doc.LoadFamily(defaultRfaPath, out family))
                    {
                        tx.Commit();
                        return family;
                    }
                    tx.RollBack();
                }
            }

            // 3. If that fails, prompt the user.
            TaskDialog.Show("Family Not Found",
                $"The family '{familyName}' could not be found at the default path. Please locate the required .rfa file.");

            FileOpenDialog dialog = new FileOpenDialog("Revit Families (*.rfa)|*.rfa");
            dialog.Title = $"Please locate {familyName}.rfa";

            if (dialog.Show() == ItemSelectionDialogResult.Confirmed)
            {
                var path = ModelPathUtils.ConvertModelPathToUserVisiblePath(dialog.SelectedModelPath);
                using (var tx = new Transaction(doc, $"Load Family: {familyName}"))
                {
                    tx.Start();
                    if (doc.LoadFamily(path, out family))
                    {
                        tx.Commit();
                        // After loading, let's update the settings for next time if possible.
                        // (This part would require a settings manager service)
                        return family;
                    }
                    tx.RollBack();
                }
            }

            return null; // All attempts failed.
        }
    }
}
