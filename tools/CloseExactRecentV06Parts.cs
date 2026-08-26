using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Recovery helper for the exact ten V0.6 generated part documents only.
// It never saves and refuses files outside the explicit whitelist or older
// than 45 minutes.  V0.4/V0.5 documents are deliberately out of scope.
internal static class CloseExactRecentV06Parts
{
    private static readonly string[] PartStems =
    {
        "SideFrame_V06_StableDoubleShearInner",
        "SideKickstand_V06_170mm_6mm",
        "KickstandOuterCheek_V06_Stable",
        "KickstandPivotPin_V06_8mm",
        "KickstandSpacer_V06_6p8mm",
        "KickstandLoadStopPin_V06_8mm",
        "KickstandLockPin_V06_5mm",
        "KickstandHeelInsert_V06",
        "KickstandFootPad_V06_Rubber",
        "DeepTravelLid_V06_StandRelief"
    };

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
            string partsRoot = Path.GetFullPath(Path.Combine(root, "cad", "parts")) +
                Path.DirectorySeparatorChar;
            SldWorks application = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            int closed = 0;

            foreach (string stem in PartStems)
            {
                string target = Path.GetFullPath(Path.Combine(partsRoot, stem + ".SLDPRT"));
                if (!target.StartsWith(partsRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Whitelist path escaped the V0.6 parts directory.");
                }

                FileInfo file = new FileInfo(target);
                if (!file.Exists || file.Length <= 0)
                {
                    throw new FileNotFoundException("Whitelisted V0.6 part is missing.", target);
                }

                TimeSpan age = DateTime.UtcNow - file.LastWriteTimeUtc;
                if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(45.0))
                {
                    throw new InvalidOperationException(
                        "Refusing to close a V0.6 part not written in the last 45 minutes: " + target);
                }

                ModelDoc2 document = FindExact(application, target);
                if (document == null)
                {
                    Console.WriteLine("V06_PART_ALREADY_CLOSED=" + target);
                    continue;
                }

                if (document.GetType() != (int)swDocumentTypes_e.swDocPART)
                {
                    throw new InvalidOperationException("Whitelisted V0.6 target is not a part: " + target);
                }

                long lengthBefore = file.Length;
                DateTime writeBefore = file.LastWriteTimeUtc;
                bool wasDirty = document.GetSaveFlag();
                application.CloseDoc(document.GetTitle());
                if (FindExact(application, target) != null)
                {
                    throw new InvalidOperationException("SOLIDWORKS did not close exact V0.6 part: " + target);
                }

                file.Refresh();
                if (!file.Exists || file.Length != lengthBefore || file.LastWriteTimeUtc != writeBefore)
                {
                    throw new InvalidOperationException(
                        "Closing without save changed exact V0.6 part metadata: " + target);
                }

                Console.WriteLine("V06_PART_CLOSED_WITHOUT_SAVE=" + target);
                Console.WriteLine("V06_PART_WAS_DIRTY=" +
                    wasDirty.ToString(CultureInfo.InvariantCulture));
                closed++;
            }

            Console.WriteLine("V06_EXACT_RECENT_PARTS_CLOSED=" +
                closed.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("V06_PART_BYTES_UNCHANGED=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V06_EXACT_PART_CLOSE_FAILED=" + exception);
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
}
