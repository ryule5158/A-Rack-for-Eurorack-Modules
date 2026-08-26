using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class ExportRackV06Preview
{
    private const string OpenAssemblyFileName = "Rack4Modules_OpenCase_V06.SLDASM";
    private const string TransportAssemblyFileName = "Rack4Modules_TransportClosed_V06.SLDASM";
    private const string ClearanceAssemblyFileName = "Rack4Modules_ClearanceCheck_V06.SLDASM";
    private const string Tilt60AssemblyFileName = "Rack4Modules_DesktopTilt60_V06.SLDASM";
    private const int PreviewWidth = 2400;
    private const int PreviewHeight = 1600;

    [STAThread]
    private static int Main(string[] arguments)
    {
        SldWorks application = null;
        ManagedDocument openCase = null;
        ManagedDocument tilt60 = null;
        string tilt60AssemblyPath = null;
        Exception exportFailure = null;

        try
        {
            string root = arguments.Length > 0
                ? Path.GetFullPath(arguments[0])
                : @"C:\Users\LENOVO\Desktop\Rack4Modules";

            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException("Rack4Modules project root does not exist: " + root);
            }

            string openAssemblyPath = ProjectAssemblyPath(root, OpenAssemblyFileName);
            string transportAssemblyPath = ProjectAssemblyPath(root, TransportAssemblyFileName);
            string clearanceAssemblyPath = ProjectAssemblyPath(root, ClearanceAssemblyFileName);
            tilt60AssemblyPath = ProjectAssemblyPath(root, Tilt60AssemblyFileName);

            RequireFile(openAssemblyPath, "V0.6 open assembly");
            RequireFile(transportAssemblyPath, "V0.6 transport assembly");
            RequireFile(clearanceAssemblyPath, "V0.6 clearance-check assembly");
            RequireFile(tilt60AssemblyPath, "V0.6 60-degree desktop assembly");

            Log("VERIFIED_NATIVE_ASSEMBLY=" + openAssemblyPath);
            Log("VERIFIED_NATIVE_ASSEMBLY=" + transportAssemblyPath);
            Log("VERIFIED_NATIVE_ASSEMBLY=" + clearanceAssemblyPath);
            Log("VERIFIED_NATIVE_ASSEMBLY=" + tilt60AssemblyPath);

            string exportsDirectory = Path.GetFullPath(Path.Combine(root, "exports"));
            Directory.CreateDirectory(exportsDirectory);

            application = AttachToRunningSolidWorks2025();
            string revision = application.RevisionNumber();
            if (string.IsNullOrEmpty(revision) ||
                !revision.StartsWith("33.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The attached automation server is not SOLIDWORKS 2025; revision=" + revision);
            }

            // Refuse before changing any view if one of the four exact V0.6 targets is
            // already open and dirty. Unrelated documents are never activated, saved,
            // rebuilt or closed.
            RequireExistingTargetIsSafe(application, openAssemblyPath, "V0.6 open assembly");
            RequireExistingTargetIsSafe(application, transportAssemblyPath, "V0.6 transport assembly");
            RequireExistingTargetIsSafe(application, clearanceAssemblyPath, "V0.6 clearance-check assembly");
            RequireExistingTargetIsSafe(application, tilt60AssemblyPath, "V0.6 60-degree desktop assembly");

            // Only the open and 60-degree variants are render sources. The transport and
            // clearance variants are exact-file prerequisites and remain untouched.
            openCase = OpenExactAssembly(application, openAssemblyPath, "V0.6 open assembly");
            tilt60 = OpenExactAssembly(application, tilt60AssemblyPath, "V0.6 60-degree desktop assembly");

            application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
            application.Visible = true;
            application.UserControl = true;

            Log("ATTACHED_SOLIDWORKS_REVISION=" + revision);
            Log("OPEN_SOURCE_ASSEMBLY=" + Path.GetFullPath(openCase.Document.GetPathName()));
            Log("TILT60_SOURCE_ASSEMBLY=" + Path.GetFullPath(tilt60.Document.GetPathName()));

            ActivateExactAssembly(application, openCase.Document, openAssemblyPath);

            // The project coordinate convention places the module face at z=0 and the broad
            // VESA back at z=110. Back therefore looks toward the module face, Front shows
            // the VESA back, and Top exposes the upper interface edge.
            ExportView(
                openCase.Document,
                exportsDirectory,
                "Rack4Modules_OpenCase_01_ModuleFront_V06.png",
                swStandardViews_e.swBackView,
                "open-case-module-front");

            ExportView(
                openCase.Document,
                exportsDirectory,
                "Rack4Modules_OpenCase_02_Isometric_V06.png",
                swStandardViews_e.swIsometricView,
                "open-case-isometric");

            ExportView(
                openCase.Document,
                exportsDirectory,
                "Rack4Modules_OpenCase_03_UpperEdge_IO_V06.png",
                swStandardViews_e.swTopView,
                "upper-edge-interface");

            ExportView(
                openCase.Document,
                exportsDirectory,
                "Rack4Modules_OpenCase_04_Back_VESA_V06.png",
                swStandardViews_e.swFrontView,
                "broad-back-vesa");

            // Export the complete standard right-side view so that the folded leg, outer
            // structural cheek and storage lock pin can be judged together at true fit scale.
            ExportView(
                openCase.Document,
                exportsDirectory,
                "Rack4Modules_OpenCase_05_RightSide_StandStorage_V06.png",
                swStandardViews_e.swRightView,
                "open-case-right-side-full-standard-view-folded-leg-outer-cheek-storage-pin");
            Log(
                "OPEN_RIGHT_SIDE_VIEW=full-right-standard-view; features=" +
                "folded-leg,outer-structural-cheek,storage-lock-pin");

            ActivateExactAssembly(application, tilt60.Document, tilt60AssemblyPath);
            ExportView(
                tilt60.Document,
                exportsDirectory,
                "Rack4Modules_DesktopTilt60_06_Isometric_V06.png",
                swStandardViews_e.swIsometricView,
                "desktop-tilt-60-isometric");

            // This complete standard side view exposes the deployed support stack,
            // positive hard stop and desk footpad together in one full-frame view.
            ExportView(
                tilt60.Document,
                exportsDirectory,
                "Rack4Modules_DesktopTilt60_07_RightSide_HardStop_Footpad_V06.png",
                swStandardViews_e.swRightView,
                "desktop-tilt-60-right-side-full-standard-view-hard-stop-footpad");
            Log(
                "TILT60_RIGHT_SIDE_VIEW=full-right-standard-view; features=" +
                "deployed-leg,positive-hard-stop,desk-footpad");

            Log("PREVIEW_EXPORT_COMPLETE=true");
        }
        catch (Exception exception)
        {
            exportFailure = exception;
        }
        finally
        {
            // Close only the V0.6 open-case document if this exporter opened it. Never
            // close a document that was already open, and never discard a document that
            // became dirty while the exporter was running. Tilt60 intentionally remains
            // open because it is the requested final operator view.
            CloseOwnedOtherDocument(application, openCase, "V0.6 open assembly", ref exportFailure);

            if (application != null && !string.IsNullOrEmpty(tilt60AssemblyPath))
            {
                try
                {
                    // If preflight refused a dirty target, tilt60 remains null and cleanup
                    // must not open or activate another target as a side effect of refusal.
                    if (tilt60 != null)
                    {
                        ShowFinalTilt60Assembly(application, tilt60.Document, tilt60AssemblyPath);
                    }
                }
                catch (Exception restoreException)
                {
                    RecordSecondaryFailure(
                        ref exportFailure,
                        "Preview cleanup completed, but the DesktopTilt60 V0.6 interface could not be restored.",
                        restoreException);
                }
            }
        }

        if (exportFailure != null)
        {
            Console.Error.WriteLine("PREVIEW_EXPORT_FAILED=" + exportFailure.ToString());
            Console.Error.Flush();
            return 1;
        }

        return 0;
    }

    private static string ProjectAssemblyPath(string root, string fileName)
    {
        return Path.GetFullPath(Path.Combine(root, "cad", "assemblies", fileName));
    }

    private static void RequireFile(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The exact " + description + " does not exist.", path);
        }
    }

    private static SldWorks AttachToRunningSolidWorks2025()
    {
        string[] programIds = new string[] { "SldWorks.Application.33", "SldWorks.Application" };
        Exception lastFailure = null;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            for (int index = 0; index < programIds.Length; index++)
            {
                try
                {
                    SldWorks application = Marshal.GetActiveObject(programIds[index]) as SldWorks;
                    if (application != null)
                    {
                        return application;
                    }
                }
                catch (COMException exception)
                {
                    lastFailure = exception;
                }
            }

            Thread.Sleep(250);
        }

        throw new InvalidOperationException(
            "No running SOLIDWORKS 2025 automation object is available. " +
            "The preview exporter intentionally does not start a new SOLIDWORKS process.",
            lastFailure);
    }

    private static void RequireExistingTargetIsSafe(
        SldWorks application,
        string expectedPath,
        string description)
    {
        ModelDoc2 existing = FindOpenDocumentByExactPath(application, expectedPath);
        if (existing == null)
        {
            return;
        }

        ValidateExactAssembly(existing, expectedPath, description);
        if (existing.GetSaveFlag())
        {
            throw new InvalidOperationException(
                "The exact " + description + " was already open with unsaved changes. " +
                "Refusing to activate it, change its view, save it, rebuild it or close it: " +
                Path.GetFullPath(expectedPath));
        }
    }

    private static ManagedDocument OpenExactAssembly(
        SldWorks application,
        string expectedPath,
        string description)
    {
        string expected = Path.GetFullPath(expectedPath);
        RequireFile(expected, description);

        ModelDoc2 document = FindOpenDocumentByExactPath(application, expected);
        bool openedByThisRun = false;

        try
        {
            if (document != null)
            {
                ValidateExactAssembly(document, expected, description);
                if (document.GetSaveFlag())
                {
                    throw new InvalidOperationException(
                        "The exact " + description + " is open with unsaved changes. " +
                        "Refusing to use or alter it: " + expected);
                }
            }
            else
            {
                int errors = 0;
                int warnings = 0;
                document = application.OpenDoc6(
                    expected,
                    (int)swDocumentTypes_e.swDocASSEMBLY,
                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                    string.Empty,
                    ref errors,
                    ref warnings) as ModelDoc2;
                openedByThisRun = document != null;

                if (document == null || errors != 0)
                {
                    throw new InvalidOperationException(
                        "SOLIDWORKS could not open the exact " + description + "; errors=" +
                        errors.ToString(CultureInfo.InvariantCulture) + "; warnings=" +
                        warnings.ToString(CultureInfo.InvariantCulture) + "; path=" + expected);
                }
            }

            ValidateExactAssembly(document, expected, description);
            return new ManagedDocument(document, expected, openedByThisRun);
        }
        catch
        {
            if (openedByThisRun && document != null)
            {
                try
                {
                    if (!document.GetSaveFlag())
                    {
                        application.CloseDoc(document.GetTitle());
                    }
                }
                catch (COMException)
                {
                    // Preserve the original open or validation failure.
                }
            }

            throw;
        }
    }

    private static ModelDoc2 FindOpenDocumentByExactPath(SldWorks application, string expectedPath)
    {
        string expected = Path.GetFullPath(expectedPath);
        ModelDoc2 current = application.GetFirstDocument() as ModelDoc2;
        while (current != null)
        {
            string currentPath = current.GetPathName();
            if (!string.IsNullOrWhiteSpace(currentPath) &&
                string.Equals(
                    Path.GetFullPath(currentPath),
                    expected,
                    StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            current = current.GetNext() as ModelDoc2;
        }

        return null;
    }

    private static void ValidateExactAssembly(
        ModelDoc2 document,
        string expectedPath,
        string description)
    {
        if (document == null)
        {
            throw new InvalidOperationException("SOLIDWORKS returned no document for " + description + ".");
        }

        string actualPath = document.GetPathName();
        if (string.IsNullOrWhiteSpace(actualPath) ||
            !(document is AssemblyDoc) ||
            !string.Equals(
                Path.GetFullPath(actualPath),
                Path.GetFullPath(expectedPath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SOLIDWORKS returned the wrong document for " + description + ": " + actualPath);
        }
    }

    private static void ActivateExactAssembly(
        SldWorks application,
        ModelDoc2 document,
        string expectedPath)
    {
        ValidateExactAssembly(document, expectedPath, "requested preview assembly");
        if (document.GetSaveFlag())
        {
            throw new InvalidOperationException(
                "The requested preview assembly has unsaved changes. Refusing to change its view: " +
                Path.GetFullPath(expectedPath));
        }

        int activationError = 0;
        ModelDoc2 activated = application.ActivateDoc3(
            document.GetTitle(),
            false,
            (int)swRebuildOnActivation_e.swDontRebuildActiveDoc,
            ref activationError) as ModelDoc2;

        if (activated == null ||
            !string.Equals(
                Path.GetFullPath(activated.GetPathName()),
                Path.GetFullPath(expectedPath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SOLIDWORKS could not activate the exact assembly; status=" +
                activationError.ToString(CultureInfo.InvariantCulture) + "; expected=" + expectedPath);
        }

        activated.Visible = true;
    }

    private static void ExportView(
        ModelDoc2 document,
        string exportsDirectory,
        string pngFileName,
        swStandardViews_e view,
        string viewLabel)
    {
        string pngPath = SafePathInside(exportsDirectory, Path.Combine(exportsDirectory, pngFileName));
        string ownershipToken = ".rack4modules-v06-preview-" +
            Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + "-" +
            Guid.NewGuid().ToString("N");
        string bitmapPath = SafePathInside(
            exportsDirectory,
            Path.Combine(exportsDirectory, ownershipToken + ".bmp"));

        if (File.Exists(bitmapPath))
        {
            throw new IOException("A supposedly unique preview bitmap already exists: " + bitmapPath);
        }

        bool createdByThisExport = false;
        try
        {
            if (document.GetSaveFlag())
            {
                throw new InvalidOperationException(
                    "The preview source became dirty before rendering " + viewLabel +
                    "; refusing to change its view or save it.");
            }

            SetView(document, view);
            Thread.Sleep(250);

            bool saved = document.SaveBMP(bitmapPath, PreviewWidth, PreviewHeight);
            createdByThisExport = File.Exists(bitmapPath);
            if (!saved || !createdByThisExport || new FileInfo(bitmapPath).Length <= 0)
            {
                throw new InvalidOperationException(
                    "SOLIDWORKS could not render the " + viewLabel + " bitmap; success=" +
                    saved.ToString());
            }

            using (Bitmap bitmap = new Bitmap(bitmapPath))
            using (FileStream pngStream = new FileStream(
                pngPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                bitmap.Save(pngStream, ImageFormat.Png);
            }

            FileInfo png = new FileInfo(pngPath);
            if (!png.Exists || png.Length <= 0)
            {
                throw new InvalidOperationException("The converted PNG is missing or empty: " + pngPath);
            }

            using (Image verification = Image.FromFile(pngPath))
            {
                if (verification.Width != PreviewWidth || verification.Height != PreviewHeight)
                {
                    throw new InvalidOperationException(
                        "The preview PNG dimensions are incorrect: " + pngPath + "; " +
                        verification.Width.ToString(CultureInfo.InvariantCulture) + "x" +
                        verification.Height.ToString(CultureInfo.InvariantCulture));
                }
            }

            Log("PNG=" + pngPath + "; view=" + viewLabel + "; bytes=" +
                png.Length.ToString(CultureInfo.InvariantCulture));
        }
        finally
        {
            if (createdByThisExport && File.Exists(bitmapPath))
            {
                DeleteOwnedTemporaryBitmap(exportsDirectory, bitmapPath, ownershipToken);
            }
        }
    }

    private static void SetView(ModelDoc2 document, swStandardViews_e view)
    {
        document.ShowNamedView2(string.Empty, (int)view);
        document.ViewDisplayShaded();
        document.ViewZoomtofit2();
        document.GraphicsRedraw2();
    }

    private static void CloseOwnedOtherDocument(
        SldWorks application,
        ManagedDocument managed,
        string description,
        ref Exception primaryFailure)
    {
        if (application == null || managed == null || !managed.OpenedByThisRun)
        {
            return;
        }

        try
        {
            ModelDoc2 openDocument = FindOpenDocumentByExactPath(application, managed.ExpectedPath);
            if (openDocument == null)
            {
                return;
            }

            ValidateExactAssembly(openDocument, managed.ExpectedPath, description);
            if (openDocument.GetSaveFlag())
            {
                throw new InvalidOperationException(
                    "The exporter-opened " + description + " became dirty while previews were running. " +
                    "Refusing to discard it by closing: " + managed.ExpectedPath);
            }

            application.CloseDoc(openDocument.GetTitle());
            if (FindOpenDocumentByExactPath(application, managed.ExpectedPath) != null)
            {
                throw new InvalidOperationException(
                    "SOLIDWORKS did not close the exporter-owned " + description + ": " +
                    managed.ExpectedPath);
            }

            Log("CLOSED_EXPORTER_OWNED_DOCUMENT=" + managed.ExpectedPath);
        }
        catch (Exception cleanupException)
        {
            RecordSecondaryFailure(
                ref primaryFailure,
                "The exporter could not safely close its own " + description + ".",
                cleanupException);
        }
    }

    private static void ShowFinalTilt60Assembly(
        SldWorks application,
        ModelDoc2 tilt60Document,
        string tilt60AssemblyPath)
    {
        if (tilt60Document.GetSaveFlag())
        {
            throw new InvalidOperationException(
                "DesktopTilt60 V0.6 has unsaved changes. Refusing to alter its final view.");
        }

        application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
        application.Visible = true;
        application.UserControl = true;
        ActivateExactAssembly(application, tilt60Document, tilt60AssemblyPath);
        SetView(tilt60Document, swStandardViews_e.swIsometricView);
        application.FrameState = (int)swWindowState_e.swWindowMaximized;
        Log("FINAL_DOCUMENT=" + Path.GetFullPath(tilt60Document.GetPathName()));
        Log("FINAL_VIEW=desktop-tilt-60-isometric");
    }

    private static void RecordSecondaryFailure(
        ref Exception primaryFailure,
        string message,
        Exception secondaryFailure)
    {
        if (primaryFailure == null)
        {
            primaryFailure = new InvalidOperationException(message, secondaryFailure);
            return;
        }

        Console.Error.WriteLine("SECONDARY_WARNING=" + message + " " + secondaryFailure.ToString());
        Console.Error.Flush();
    }

    private static void DeleteOwnedTemporaryBitmap(
        string exportsDirectory,
        string bitmapPath,
        string ownershipToken)
    {
        string safeBitmapPath = SafePathInside(exportsDirectory, bitmapPath);
        string fileName = Path.GetFileName(safeBitmapPath);
        if (!string.Equals(fileName, ownershipToken + ".bmp", StringComparison.Ordinal) ||
            !ownershipToken.StartsWith(".rack4modules-v06-preview-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Refusing to delete a bitmap that was not created by this preview export: " +
                safeBitmapPath);
        }

        File.Delete(safeBitmapPath);
        if (File.Exists(safeBitmapPath))
        {
            throw new IOException("The owned temporary bitmap could not be removed: " + safeBitmapPath);
        }
    }

    private static string SafePathInside(string directory, string candidate)
    {
        string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(candidate);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Path escapes the Rack4Modules exports directory: " + fullPath);
        }

        return fullPath;
    }

    private static void Log(string message)
    {
        Console.WriteLine("[RackV06Preview] " + message);
        Console.Out.Flush();
    }

    private sealed class ManagedDocument
    {
        internal readonly ModelDoc2 Document;
        internal readonly string ExpectedPath;
        internal readonly bool OpenedByThisRun;

        internal ManagedDocument(ModelDoc2 document, string expectedPath, bool openedByThisRun)
        {
            Document = document;
            ExpectedPath = Path.GetFullPath(expectedPath);
            OpenedByThisRun = openedByThisRun;
        }
    }
}
