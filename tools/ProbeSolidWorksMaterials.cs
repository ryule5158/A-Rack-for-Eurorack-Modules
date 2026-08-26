using System;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Readback-only material database probe.  It creates one unsaved temporary
// part, tests exact library names, and closes the temporary document unsaved.
internal static class ProbeSolidWorksMaterials
{
    private const string PartTemplate =
        @"C:\ProgramData\SolidWorks\SOLIDWORKS 2025\templates\gb_part.prtdot";
    private const string MaterialDatabase =
        @"E:\SW2025\SOLIDWORKS\lang\english\sldmaterials\solidworks materials.sldmat";

    [STAThread]
    private static int Main(string[] arguments)
    {
        ModelDoc2 document = null;
        SldWorks application = null;
        try
        {
            if (arguments.Length != 1 || !Directory.Exists(arguments[0]))
            {
                throw new ArgumentException("Pass exactly one existing Rack4Modules root.");
            }
            if (!File.Exists(PartTemplate) || !File.Exists(MaterialDatabase))
            {
                throw new FileNotFoundException("SOLIDWORKS template or material database is missing.");
            }

            application = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            document = application.NewDocument(PartTemplate, 0, 0.0, 0.0) as ModelDoc2;
            PartDoc part = document as PartDoc;
            if (part == null)
            {
                throw new InvalidOperationException("SOLIDWORKS did not create the temporary part.");
            }

            string[] candidates =
            {
                "Natural Rubber", "SBR", "NEOPRENE", "Silicon Rubber"
            };
            foreach (string candidate in candidates)
            {
                part.SetMaterialPropertyName2(string.Empty, MaterialDatabase, candidate);
                string database;
                string actual = part.GetMaterialPropertyName2(string.Empty, out database);
                Console.WriteLine("MATERIAL_PROBE_REQUEST=" + candidate +
                    ";READBACK=" + (actual ?? string.Empty) +
                    ";DATABASE=" + (database ?? string.Empty));
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("MATERIAL_PROBE_FAILED=" + exception);
            return 1;
        }
        finally
        {
            if (application != null && document != null)
            {
                try { application.CloseDoc(document.GetTitle()); }
                catch { }
            }
        }
    }
}
