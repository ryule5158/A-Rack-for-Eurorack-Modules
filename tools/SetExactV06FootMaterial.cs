using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Applies the verified-readable NEOPRENE database entry to the exact V0.6
// foot-pad part.  Geometry and every other project document are read-only.
internal static class SetExactV06FootMaterial
{
    private const string MaterialDatabase =
        @"E:\SW2025\SOLIDWORKS\lang\english\sldmaterials\solidworks materials.sldmat";

    [STAThread]
    private static int Main(string[] arguments)
    {
        SldWorks application = null;
        ModelDoc2 document = null;
        bool openedByThisRun = false;
        try
        {
            if (arguments.Length != 1 || !Directory.Exists(arguments[0]))
            {
                throw new ArgumentException("Pass exactly one existing Rack4Modules root.");
            }

            string target = Path.GetFullPath(Path.Combine(arguments[0], "cad", "parts",
                "KickstandFootPad_V06_Rubber.SLDPRT"));
            FileInfo file = new FileInfo(target);
            if (!file.Exists || file.Length <= 0)
            {
                throw new FileNotFoundException("Exact V0.6 foot-pad part is missing.", target);
            }
            if (!File.Exists(MaterialDatabase))
            {
                throw new FileNotFoundException("SOLIDWORKS material database is missing.", MaterialDatabase);
            }

            application = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            document = FindExact(application, target);
            if (document == null)
            {
                int errors = 0;
                int warnings = 0;
                document = application.OpenDoc6(target,
                    (int)swDocumentTypes_e.swDocPART,
                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                    string.Empty, ref errors, ref warnings) as ModelDoc2;
                if (document == null || errors != 0)
                {
                    throw new InvalidOperationException(
                        "Cannot open exact V0.6 foot-pad part; errors=" + errors +
                        ", warnings=" + warnings);
                }
                openedByThisRun = true;
            }

            if (document.GetType() != (int)swDocumentTypes_e.swDocPART ||
                !string.Equals(Path.GetFullPath(document.GetPathName()), target,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Opened document is not the exact V0.6 foot-pad part.");
            }

            PartDoc part = document as PartDoc;
            object rawBodies = part.GetBodies2((int)swBodyType_e.swSolidBody, true);
            object[] bodies = rawBodies as object[];
            if (bodies == null || bodies.Length != 1)
            {
                throw new InvalidOperationException("V0.6 foot pad must contain exactly one solid body.");
            }

            double[] box = part.GetPartBox(true) as double[];
            if (box == null || box.Length < 6 ||
                Math.Abs((box[3] - box[0]) * 1000.0 - 6.0) > 0.05 ||
                Math.Abs((box[4] - box[1]) * 1000.0 - 16.0) > 0.05 ||
                Math.Abs((box[5] - box[2]) * 1000.0 - 16.0) > 0.05)
            {
                throw new InvalidOperationException("Exact foot-pad geometry is not 6 x 16 x 16 mm.");
            }

            part.SetMaterialPropertyName2(string.Empty, MaterialDatabase, "NEOPRENE");
            string database;
            string material = part.GetMaterialPropertyName2(string.Empty, out database);
            if (!string.Equals(material, "NEOPRENE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "NEOPRENE material readback failed; actual='" + material + "'.");
            }

            int saveErrors = 0;
            int saveWarnings = 0;
            bool saved = document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                ref saveErrors, ref saveWarnings);
            if (!saved || saveErrors != 0 || document.GetSaveFlag())
            {
                throw new InvalidOperationException(
                    "Exact foot-pad material save failed; errors=" + saveErrors +
                    ", warnings=" + saveWarnings + ", dirty=" + document.GetSaveFlag());
            }

            Console.WriteLine("V06_FOOT_MATERIAL=NEOPRENE");
            Console.WriteLine("V06_FOOT_MATERIAL_DATABASE=" + database);
            Console.WriteLine("V06_FOOT_MATERIAL_READBACK=PASS");
            Console.WriteLine("V06_FOOT_GEOMETRY_UNCHANGED=6x16x16_mm");
            Console.WriteLine("V06_FOOT_NATIVE_CLEAN=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V06_FOOT_MATERIAL_SET_FAILED=" + exception);
            return 1;
        }
        finally
        {
            if (openedByThisRun && application != null && document != null)
            {
                try
                {
                    if (!document.GetSaveFlag())
                    {
                        application.CloseDoc(document.GetTitle());
                    }
                }
                catch { }
            }
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
