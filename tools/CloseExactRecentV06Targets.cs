using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Recovery helper for the exact generated V0.6 assembly targets that a
// preview/validator may leave dirty after SOLIDWORKS adds NeedsRegen state.
// It never saves and never touches V0.4/V0.5 or any non-whitelisted path.
internal static class CloseExactRecentV06Targets
{
    private sealed class Target
    {
        internal readonly string Stem;
        internal readonly int Count;
        internal Target(string stem, int count) { Stem = stem; Count = count; }
    }

    private static readonly Target[] Targets =
    {
        new Target("Rack4Modules_OpenCase_V06", 66),
        new Target("Rack4Modules_TransportClosed_V06", 67),
        new Target("Rack4Modules_ClearanceCheck_V06", 74),
        new Target("Rack4Modules_DesktopTilt60_V06", 67)
    };

    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length != 1) throw new ArgumentException("Pass one Rack4Modules root.");
            string root = Path.GetFullPath(arguments[0]);
            string assemblyRoot = Path.GetFullPath(Path.Combine(root, "cad", "assemblies")) +
                Path.DirectorySeparatorChar;
            SldWorks application = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            int closed = 0;

            foreach (Target targetInfo in Targets)
            {
                string target = Path.GetFullPath(Path.Combine(assemblyRoot, targetInfo.Stem + ".SLDASM"));
                if (!target.StartsWith(assemblyRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Whitelist escaped the V0.6 assembly directory.");
                FileInfo file = new FileInfo(target);
                if (!file.Exists || file.Length <= 0)
                    throw new FileNotFoundException("Whitelisted V0.6 assembly is missing or empty.", target);
                TimeSpan age = DateTime.UtcNow - file.LastWriteTimeUtc;
                if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(45.0))
                    throw new InvalidOperationException("Refusing an assembly not written in the last 45 minutes: " + target);

                ModelDoc2 document = FindExact(application, target);
                if (document == null)
                {
                    Console.WriteLine("V06_TARGET_ALREADY_CLOSED=" + target);
                    continue;
                }
                if (!(document is AssemblyDoc)) throw new InvalidOperationException("Whitelisted target is not an assembly: " + target);
                string revision = ReadProperty(document, "Desktop support revision");
                if (revision.IndexOf("V0.6 captured", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Revision marker mismatch: " + target);

                Array raw = ((AssemblyDoc)document).GetComponents(true) as Array;
                if (raw == null || raw.Length != targetInfo.Count)
                    throw new InvalidOperationException("Component count mismatch for " + target + ": expected " + targetInfo.Count + ", actual " + (raw == null ? 0 : raw.Length));

                long lengthBefore = file.Length;
                DateTime writeBefore = file.LastWriteTimeUtc;
                bool wasDirty = document.GetSaveFlag();
                string title = document.GetTitle();
                application.CloseDoc(title);
                if (FindExact(application, target) != null)
                    throw new InvalidOperationException("SOLIDWORKS did not close exact target: " + target);
                file.Refresh();
                if (!file.Exists || file.Length != lengthBefore || file.LastWriteTimeUtc != writeBefore)
                    throw new InvalidOperationException("Closing without save changed target metadata: " + target);
                Console.WriteLine("V06_TARGET_CLOSED_WITHOUT_SAVE=" + target);
                Console.WriteLine("V06_TARGET_WAS_DIRTY=" + wasDirty.ToString(CultureInfo.InvariantCulture));
                closed++;
            }

            Console.WriteLine("V06_EXACT_TARGETS_CLOSED=" + closed.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("V06_TARGET_BYTES_UNCHANGED=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V06_TARGET_CLOSE_FAILED=" + exception);
            return 1;
        }
    }

    private static ModelDoc2 FindExact(SldWorks application, string expected)
    {
        ModelDoc2 current = application.GetFirstDocument() as ModelDoc2;
        while (current != null)
        {
            string path = current.GetPathName();
            if (!string.IsNullOrWhiteSpace(path) &&
                string.Equals(Path.GetFullPath(path), expected, StringComparison.OrdinalIgnoreCase))
                return current;
            current = current.GetNext() as ModelDoc2;
        }
        return null;
    }

    private static string ReadProperty(ModelDoc2 document, string name)
    {
        CustomPropertyManager manager = document.Extension.CustomPropertyManager[string.Empty];
        if (manager == null) return string.Empty;
        string value, resolved; bool wasResolved, linked;
        manager.Get6(name, false, out value, out resolved, out wasResolved, out linked);
        return string.IsNullOrWhiteSpace(resolved) ? value : resolved;
    }
}
