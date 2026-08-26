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

internal static class ExportRackV04Preview
{
    private const string OpenAssemblyFileName = "Rack4Modules_OpenCase_V04.SLDASM";
    private const string Tilt60AssemblyFileName = "Rack4Modules_DesktopTilt60_V04.SLDASM";
    private const int PreviewWidth = 2400;
    private const int PreviewHeight = 1600;

    [STAThread]
    private static int Main(string[] arguments)
    {
        SldWorks application = null;
        ModelDoc2 openCaseDocument = null;
        ModelDoc2 tilt60Document = null;
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
            tilt60AssemblyPath = ProjectAssemblyPath(root, Tilt60AssemblyFileName);
            RequireFile(openAssemblyPath, "V0.4 open assembly");
            RequireFile(tilt60AssemblyPath, "V0.4 60-degree desktop assembly");

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

            openCaseDocument = OpenExactAssembly(application, openAssemblyPath, "V0.4 open assembly");
            tilt60Document = OpenExactAssembly(application, tilt60AssemblyPath, "V0.4 60-degree desktop assembly");

            application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
            application.Visible = true;
            application.UserControl = true;

            Log("ATTACHED_SOLIDWORKS_REVISION=" + revision);
            Log("OPEN_SOURCE_ASSEMBLY=" + Path.GetFullPath(openCaseDocument.GetPathName()));
            Log("TILT60_SOURCE_ASSEMBLY=" + Path.GetFullPath(tilt60Document.GetPathName()));

            ActivateExactAssembly(application, openCaseDocument, openAssemblyPath);

            // The project coordinate convention places the module face at z=0 and the broad VESA back at z=110.
            // Therefore Back looks toward the module face, Front shows the VESA back, and Top exposes the upper I/O edge.
            ExportView(
                openCaseDocument,
                exportsDirectory,
                "Rack4Modules_OpenCase_V04_01_ModuleFront.png",
                swStandardViews_e.swBackView,
                "open-case-module-front");

            ExportView(
                openCaseDocument,
                exportsDirectory,
                "Rack4Modules_OpenCase_V04_02_Isometric.png",
                swStandardViews_e.swIsometricView,
                "open-case-isometric");

            ExportView(
                openCaseDocument,
                exportsDirectory,
                "Rack4Modules_OpenCase_V04_03_UpperEdge_IO.png",
                swStandardViews_e.swTopView,
                "upper-edge-interface");

            ExportView(
                openCaseDocument,
                exportsDirectory,
                "Rack4Modules_OpenCase_V04_04_Back_VESA.png",
                swStandardViews_e.swFrontView,
                "broad-back-vesa");

            ActivateExactAssembly(application, tilt60Document, tilt60AssemblyPath);
            ExportView(
                tilt60Document,
                exportsDirectory,
                "Rack4Modules_DesktopTilt60_V04_05_Isometric.png",
                swStandardViews_e.swIsometricView,
                "desktop-tilt-60-isometric");

            Log("PREVIEW_EXPORT_COMPLETE=true");
        }
        catch (Exception exception)
        {
            exportFailure = exception;
        }
        finally
        {
            if (application != null && !string.IsNullOrEmpty(tilt60AssemblyPath))
            {
                try
                {
                    if (tilt60Document == null && File.Exists(tilt60AssemblyPath))
                    {
                        tilt60Document = OpenExactAssembly(
                            application,
                            tilt60AssemblyPath,
                            "V0.4 60-degree desktop assembly");
                    }

                    if (tilt60Document != null)
                    {
                        ShowFinalTilt60Assembly(application, tilt60Document, tilt60AssemblyPath);
                    }
                }
                catch (Exception restoreException)
                {
                    if (exportFailure == null)
                    {
                        exportFailure = new InvalidOperationException(
                            "Preview files were exported, but the DesktopTilt60 V0.4 interface " +
                            "could not be restored.",
                            restoreException);
                    }
                    else
                    {
                        Console.Error.WriteLine("RESTORE_WARNING=" + restoreException.ToString());
                        Console.Error.Flush();
                    }
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

    private static ModelDoc2 OpenExactAssembly(
        SldWorks application,
        string expectedPath,
        string description)
    {
        string expected = Path.GetFullPath(expectedPath);
        RequireFile(expected, description);

        ModelDoc2 document = application.GetOpenDocumentByName(expected) as ModelDoc2;
        if (document == null)
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

            if (document == null || errors != 0)
            {
                throw new InvalidOperationException(
                    "SOLIDWORKS could not open the exact " + description + "; errors=" +
                    errors.ToString(CultureInfo.InvariantCulture) + "; warnings=" +
                    warnings.ToString(CultureInfo.InvariantCulture) + "; path=" + expected);
            }
        }

        string actual = Path.GetFullPath(document.GetPathName());
        if (!(document is AssemblyDoc) ||
            !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SOLIDWORKS returned the wrong document for " + description + ": " + actual);
        }

        return document;
    }

    private static void ActivateExactAssembly(
        SldWorks application,
        ModelDoc2 document,
        string expectedPath)
    {
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
        string ownershipToken = ".rack4modules-v04-preview-" +
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

    private static void ShowFinalTilt60Assembly(
        SldWorks application,
        ModelDoc2 tilt60Document,
        string tilt60AssemblyPath)
    {
        application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
        application.Visible = true;
        application.UserControl = true;
        ActivateExactAssembly(application, tilt60Document, tilt60AssemblyPath);
        SetView(tilt60Document, swStandardViews_e.swIsometricView);
        application.FrameState = (int)swWindowState_e.swWindowMaximized;
        Log("FINAL_DOCUMENT=" + Path.GetFullPath(tilt60Document.GetPathName()));
        Log("FINAL_VIEW=desktop-tilt-60-isometric");
    }

    private static void DeleteOwnedTemporaryBitmap(
        string exportsDirectory,
        string bitmapPath,
        string ownershipToken)
    {
        string safeBitmapPath = SafePathInside(exportsDirectory, bitmapPath);
        string fileName = Path.GetFileName(safeBitmapPath);
        if (!string.Equals(fileName, ownershipToken + ".bmp", StringComparison.Ordinal) ||
            !ownershipToken.StartsWith(".rack4modules-v04-preview-", StringComparison.Ordinal))
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
        Console.WriteLine("[RackV04Preview] " + message);
        Console.Out.Flush();
    }
}
