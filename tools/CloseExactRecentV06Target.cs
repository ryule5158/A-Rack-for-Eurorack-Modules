using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// One-purpose recovery helper for an exact, recently generated V0.6 target.
// It never saves.  It closes only the exact Tilt60 V0.6 path after proving
// assembly type, revision marker, component count and V0.6 hardware inventory.
internal static class CloseExactRecentV06Target
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Pass exactly one Rack4Modules project root.");
            }

            string root = Path.GetFullPath(arguments[0]);
            string target = Path.GetFullPath(Path.Combine(root, "cad", "assemblies",
                "Rack4Modules_DesktopTilt60_V06.SLDASM"));
            FileInfo file = new FileInfo(target);
            if (!file.Exists || file.Length <= 0)
            {
                throw new FileNotFoundException("Exact recent V0.6 target is missing.", target);
            }

            TimeSpan age = DateTime.UtcNow - file.LastWriteTimeUtc;
            if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(30.0))
            {
                throw new InvalidOperationException(
                    "Refusing to close a V0.6 target that was not written in the last 30 minutes: " + target);
            }

            SldWorks application = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            ModelDoc2 document = FindExact(application, target);
            if (document == null)
            {
                Console.WriteLine("V06_EXACT_TARGET_ALREADY_CLOSED=" + target);
                return 0;
            }

            if (document.GetType() != (int)swDocumentTypes_e.swDocASSEMBLY)
            {
                throw new InvalidOperationException("Exact V0.6 target is not an assembly.");
            }

            string revision = ReadProperty(document, "Desktop support revision");
            if (revision.IndexOf("V0.6 captured", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException(
                    "Exact target does not carry the expected V0.6 generated revision marker: " + revision);
            }

            AssemblyDoc assembly = document as AssemblyDoc;
            object raw = assembly == null ? null : assembly.GetComponents(true);
            object[] components = raw as object[];
            if (components == null || components.Length != 67)
            {
                throw new InvalidOperationException(
                    "Exact V0.6 target does not have the frozen 67 top-level components.");
            }

            Dictionary<string, int> required = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase)
            {
                { "SideFrame_V06_StableDoubleShearInner", 2 },
                { "SideKickstand_V06_170mm_6mm", 2 },
                { "KickstandOuterCheek_V06_Stable", 2 },
                { "KickstandPivotPin_V06_8mm", 2 },
                { "KickstandSpacer_V06_6p8mm", 8 },
                { "KickstandLoadStopPin_V06_8mm", 2 },
                { "KickstandLockPin_V06_5mm", 2 },
                { "KickstandHeelInsert_V06", 2 },
                { "KickstandFootPad_V06_Rubber", 2 },
                { "DeepTravelLid_V06_StandRelief", 0 }
            };
            Dictionary<string, int> actual = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
            foreach (object item in components)
            {
                Component2 component = item as Component2;
                if (component == null)
                {
                    continue;
                }

                string path = component.GetPathName();
                string stem = string.IsNullOrWhiteSpace(path)
                    ? Path.GetFileNameWithoutExtension(component.Name2)
                    : Path.GetFileNameWithoutExtension(path);
                if (!actual.ContainsKey(stem))
                {
                    actual[stem] = 0;
                }
                actual[stem]++;
            }

            foreach (KeyValuePair<string, int> pair in required)
            {
                int count = actual.ContainsKey(pair.Key) ? actual[pair.Key] : 0;
                if (count != pair.Value)
                {
                    throw new InvalidOperationException(
                        "Exact target hardware inventory mismatch for " + pair.Key +
                        "; expected " + pair.Value.ToString(CultureInfo.InvariantCulture) +
                        ", actual " + count.ToString(CultureInfo.InvariantCulture));
                }
            }

            long lengthBefore = file.Length;
            DateTime writeBefore = file.LastWriteTimeUtc;
            bool wasDirty = document.GetSaveFlag();
            string title = document.GetTitle();
            application.CloseDoc(title);
            if (FindExact(application, target) != null)
            {
                throw new InvalidOperationException("SOLIDWORKS did not close the exact V0.6 target.");
            }

            file.Refresh();
            if (!file.Exists || file.Length != lengthBefore || file.LastWriteTimeUtc != writeBefore)
            {
                throw new InvalidOperationException(
                    "Closing without save unexpectedly changed the exact V0.6 file metadata.");
            }

            Console.WriteLine("V06_EXACT_TARGET_CLOSED_WITHOUT_SAVE=" + target);
            Console.WriteLine("V06_EXACT_TARGET_WAS_DIRTY=" +
                wasDirty.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("V06_EXACT_TARGET_BYTES_UNCHANGED=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V06_EXACT_TARGET_CLOSE_FAILED=" + exception);
            return 1;
        }
    }

    private static ModelDoc2 FindExact(SldWorks application, string target)
    {
        ModelDoc2 current = application.GetFirstDocument() as ModelDoc2;
        while (current != null)
        {
            string path = current.GetPathName();
            if (!string.IsNullOrWhiteSpace(path) && string.Equals(
                    Path.GetFullPath(path), target, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }
            current = current.GetNext() as ModelDoc2;
        }
        return null;
    }

    private static string ReadProperty(ModelDoc2 document, string name)
    {
        CustomPropertyManager manager =
            document.Extension.CustomPropertyManager[string.Empty];
        if (manager == null)
        {
            return string.Empty;
        }

        string value;
        string resolved;
        bool wasResolved;
        bool linked;
        manager.Get6(name, false, out value, out resolved, out wasResolved, out linked);
        return string.IsNullOrWhiteSpace(resolved) ? value : resolved;
    }
}
