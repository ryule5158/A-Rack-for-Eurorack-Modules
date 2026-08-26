using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class RebuildRackV03SecondaryAssemblies
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Exactly one Rack4Modules root is required.");
            }

            RackCadSession cad = new RackCadSession(Path.GetFullPath(arguments[0]));
            PreserveOnlyProjectDocuments(cad);

            foreach (string stem in new string[]
            {
                "Rack4Modules_TransportClosed_V03",
                "Rack4Modules_ClearanceCheck_V03"
            })
            {
                RebuildOne(cad, stem);
            }

            RestoreOpenCase(cad);
            cad.Log("V03_SECONDARY_REBUILD_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V03_SECONDARY_REBUILD_FAILED=" + exception);
            return 1;
        }
    }

    private static void PreserveOnlyProjectDocuments(RackCadSession cad)
    {
        string prefix = cad.Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        ModelDoc2 document = cad.Application.GetFirstDocument() as ModelDoc2;
        while (document != null)
        {
            string path = document.GetPathName();
            if (string.IsNullOrWhiteSpace(path) ||
                !Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Refusing a non-project or unnamed open document: " + document.GetTitle());
            }

            if (document.GetSaveFlag())
            {
                int errors = 0;
                int warnings = 0;
                if (!document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errors, ref warnings) || errors != 0)
                {
                    throw new InvalidOperationException("Cannot preserve project document " + path + "; errors=" + errors);
                }

                cad.Log("PRESERVED_PROJECT_DOCUMENT=" + path + ";warnings=" + warnings);
            }

            document = document.GetNext() as ModelDoc2;
        }
    }

    private static void RebuildOne(RackCadSession cad, string stem)
    {
        string path = Path.Combine(cad.AssembliesDirectory, stem + ".SLDASM");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Required V0.3 secondary assembly is missing.", path);
        }

        int errors = 0;
        int warnings = 0;
        ModelDoc2 document = cad.Application.OpenDoc6(path, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty, ref errors, ref warnings) as ModelDoc2;
        if (document == null || errors != 0)
        {
            throw new InvalidOperationException("Cannot open " + path + "; errors=" + errors + "; warnings=" + warnings);
        }

        cad.Log("PRE_REBUILD_WARNING=" + stem + ":" + warnings.ToString(CultureInfo.InvariantCulture));
        AssemblyDoc assembly = document as AssemblyDoc;
        if (assembly == null)
        {
            throw new InvalidOperationException(path + " is not an assembly.");
        }

        int unresolved = assembly.ResolveAllLightWeightComponents(true);
        Array components = assembly.GetComponents(false) as Array;
        if (components != null)
        {
            foreach (object item in components)
            {
                Component2 component = item as Component2;
                ModelDoc2 child = component == null ? null : component.GetModelDoc2() as ModelDoc2;
                if (child == null)
                {
                    continue;
                }

                child.Extension.ForceRebuildAll();
                child.ForceRebuild3(true);
                if (child.GetSaveFlag())
                {
                    int childErrors = 0;
                    int childWarnings = 0;
                    if (!child.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                        ref childErrors, ref childWarnings) || childErrors != 0)
                    {
                        throw new InvalidOperationException("Referenced project component rebuild failed: " +
                            component.GetPathName() + "; errors=" + childErrors);
                    }
                }
            }
        }

        document.Extension.ForceRebuildAll();
        document.ForceRebuild3(true);
        document.Extension.EditRebuildAll();
        document.EditRebuild3();
        int saveErrors = 0;
        int saveWarnings = 0;
        if (!document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent,
            ref saveErrors, ref saveWarnings) || saveErrors != 0)
        {
            throw new InvalidOperationException("Native rebuild save failed for " + path + "; errors=" + saveErrors);
        }

        cad.SaveAssembly(document, stem, true);
        cad.Log("REBUILT=" + stem + ";resolve_status=" + unresolved + ";save_warnings=" + saveWarnings);
        cad.Application.CloseDoc(document.GetTitle());

        int verifyErrors = 0;
        int verifyWarnings = 0;
        ModelDoc2 verified = cad.Application.OpenDoc6(path, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty,
            ref verifyErrors, ref verifyWarnings) as ModelDoc2;
        if (verified == null || verifyErrors != 0)
        {
            throw new InvalidOperationException("Rebuilt assembly cannot reopen: " + path);
        }

        int remaining = verifyWarnings &
            ((int)swFileLoadWarning_e.swFileLoadWarning_IdMismatch |
             (int)swFileLoadWarning_e.swFileLoadWarning_NeedsRegen);
        if (remaining != 0)
        {
            cad.Log("WARNING: Persistent SOLIDWORKS generated-body assembly status for " + stem +
                "; bitmask=" + remaining.ToString(CultureInfo.InvariantCulture) +
                ". Full recursive rebuild and native save completed without save errors.");
        }

        cad.Log("POST_REBUILD_WARNING=" + stem + ":" + verifyWarnings.ToString(CultureInfo.InvariantCulture));
        cad.Application.CloseDoc(verified.GetTitle());
    }

    private static void RestoreOpenCase(RackCadSession cad)
    {
        string path = Path.Combine(cad.AssembliesDirectory, "Rack4Modules_OpenCase_V03.SLDASM");
        int errors = 0;
        int warnings = 0;
        ModelDoc2 document = cad.Application.GetOpenDocumentByName(path) as ModelDoc2;
        if (document == null)
        {
            document = cad.Application.OpenDoc6(path, (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty,
                ref errors, ref warnings) as ModelDoc2;
        }

        if (document == null || errors != 0)
        {
            throw new InvalidOperationException("Cannot restore the V0.3 open-case assembly.");
        }

        cad.Show(document);
        cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
    }
}
