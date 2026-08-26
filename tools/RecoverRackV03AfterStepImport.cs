using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class RecoverRackV03AfterStepImport
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

            string root = Path.GetFullPath(arguments[0]);
            string partsRoot = Path.Combine(root, "cad", "parts") + Path.DirectorySeparatorChar;
            string assembliesRoot = Path.Combine(root, "cad", "assemblies") + Path.DirectorySeparatorChar;
            SldWorks application = Attach();

            List<ModelDoc2> documents = Documents(application);
            foreach (ModelDoc2 document in documents)
            {
                ValidateProjectOrOwnedImport(document, partsRoot, assembliesRoot);
            }

            int importedParts = 0;
            int importedAssemblies = 0;
            foreach (ModelDoc2 document in documents)
            {
                string path = document.GetPathName();
                if (document.GetType() == (int)swDocumentTypes_e.swDocPART &&
                    !string.IsNullOrWhiteSpace(path) &&
                    Path.GetFullPath(path).StartsWith(assembliesRoot, StringComparison.OrdinalIgnoreCase) &&
                    !File.Exists(path))
                {
                    string realPart = Path.Combine(partsRoot, document.GetTitle());
                    if (!File.Exists(realPart))
                    {
                        throw new InvalidOperationException("Cannot prove transient STEP ownership for " + path);
                    }

                    string title = document.GetTitle();
                    string temporaryTitle = "__RackV03ValidationImportPart_" +
                        importedParts.ToString(CultureInfo.InvariantCulture) + ".SLDPRT";
                    if (!document.SetTitle2(temporaryTitle) ||
                        !string.Equals(document.GetTitle(), temporaryTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Cannot assign a unique title to the owned STEP child: " + title);
                    }
                    application.CloseDoc(temporaryTitle);
                    importedParts++;
                    Console.WriteLine("CLOSED_OWNED_STEP_CHILD=" + title + ";nonexistent_path=" + path);
                }
            }

            foreach (ModelDoc2 document in documents)
            {
                string path = document.GetPathName();
                if (document.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY &&
                    string.IsNullOrWhiteSpace(path) &&
                    IsKnownAssemblyTitle(document.GetTitle()))
                {
                    string realAssembly = Path.Combine(assembliesRoot, document.GetTitle());
                    if (!File.Exists(realAssembly))
                    {
                        throw new InvalidOperationException("Cannot prove imported assembly ownership: " + document.GetTitle());
                    }

                    string title = document.GetTitle();
                    string temporaryTitle = "__RackV03ValidationImportAssembly_" +
                        importedAssemblies.ToString(CultureInfo.InvariantCulture) + ".SLDASM";
                    if (!document.SetTitle2(temporaryTitle) ||
                        !string.Equals(document.GetTitle(), temporaryTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Cannot assign a unique title to the owned STEP assembly: " + title);
                    }
                    application.CloseDoc(temporaryTitle);
                    importedAssemblies++;
                    Console.WriteLine("CLOSED_OWNED_STEP_ASSEMBLY=" + title);
                }
            }

            string openCasePath = Path.Combine(assembliesRoot, "Rack4Modules_OpenCase_V03.SLDASM");
            int errors = 0;
            int warnings = 0;
            ModelDoc2 openCase = application.OpenDoc6(openCasePath, (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty,
                ref errors, ref warnings) as ModelDoc2;
            if (openCase == null || errors != 0 ||
                !string.Equals(Path.GetFullPath(openCase.GetPathName()), openCasePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot restore the exact native V0.3 open case; errors=" + errors);
            }

            openCase.ShowNamedView2(string.Empty, (int)swStandardViews_e.swIsometricView);
            openCase.ViewDisplayShaded();
            openCase.ViewZoomtofit2();
            openCase.GraphicsRedraw2();
            application.Visible = true;
            application.UserControl = true;
            application.FrameState = (int)swWindowState_e.swWindowMaximized;

            foreach (ModelDoc2 remaining in Documents(application))
            {
                string path = remaining.GetPathName();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    throw new InvalidOperationException("A transient validation document remains: " + remaining.GetTitle());
                }
            }

            Console.WriteLine("CLOSED_OWNED_STEP_CHILD_COUNT=" + importedParts.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("CLOSED_OWNED_STEP_ASSEMBLY_COUNT=" + importedAssemblies.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("RESTORED_NATIVE_OPEN_CASE=" + openCasePath + ";warnings=" + warnings);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V03_STEP_IMPORT_RECOVERY_FAILED=" + exception);
            return 1;
        }
    }

    private static void ValidateProjectOrOwnedImport(ModelDoc2 document, string partsRoot, string assembliesRoot)
    {
        string path = document.GetPathName();
        if (string.IsNullOrWhiteSpace(path))
        {
            if (document.GetType() != (int)swDocumentTypes_e.swDocASSEMBLY ||
                !IsKnownAssemblyTitle(document.GetTitle()))
            {
                throw new InvalidOperationException("Refusing an unrelated unnamed document: " + document.GetTitle());
            }
            return;
        }

        string fullPath = Path.GetFullPath(path);
        if (fullPath.StartsWith(partsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            return;
        }
        if (fullPath.StartsWith(assembliesRoot, StringComparison.OrdinalIgnoreCase) &&
            document.GetType() == (int)swDocumentTypes_e.swDocPART && !File.Exists(fullPath) &&
            File.Exists(Path.Combine(partsRoot, document.GetTitle())))
        {
            return;
        }

        throw new InvalidOperationException("Refusing an unrelated or ambiguous document: " + fullPath);
    }

    private static bool IsKnownAssemblyTitle(string title)
    {
        return string.Equals(title, "Rack4Modules_OpenCase_V03.SLDASM", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(title, "Rack4Modules_TransportClosed_V03.SLDASM", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(title, "Rack4Modules_ClearanceCheck_V03.SLDASM", StringComparison.OrdinalIgnoreCase);
    }

    private static List<ModelDoc2> Documents(SldWorks application)
    {
        List<ModelDoc2> result = new List<ModelDoc2>();
        ModelDoc2 document = application.GetFirstDocument() as ModelDoc2;
        while (document != null)
        {
            result.Add(document);
            document = document.GetNext() as ModelDoc2;
        }
        return result;
    }

    private static SldWorks Attach()
    {
        foreach (string identifier in new string[] { "SldWorks.Application.33", "SldWorks.Application" })
        {
            try
            {
                SldWorks application = Marshal.GetActiveObject(identifier) as SldWorks;
                if (application != null) return application;
            }
            catch (COMException) { }
        }
        throw new InvalidOperationException("No running SOLIDWORKS 2025 session was found.");
    }
}
