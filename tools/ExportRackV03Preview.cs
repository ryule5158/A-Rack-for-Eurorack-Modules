using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class ExportRackV03Preview
{
    private const string AssemblyFileName = "Rack4Modules_OpenCase_V03.SLDASM";
    private const int PreviewWidth = 2400;
    private const int PreviewHeight = 1600;

    [STAThread]
    private static int Main(string[] arguments)
    {
        ModelDoc2 assemblyDocument = null;
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

            string expectedAssemblyPath = Path.GetFullPath(
                Path.Combine(root, "cad", "assemblies", AssemblyFileName));
            if (!File.Exists(expectedAssemblyPath))
            {
                throw new FileNotFoundException("The exact V0.3 open assembly does not exist.", expectedAssemblyPath);
            }

            string exportsDirectory = Path.GetFullPath(Path.Combine(root, "exports"));
            Directory.CreateDirectory(exportsDirectory);

            SldWorks application = AttachToRunningSolidWorks2025();
            string revision = application.RevisionNumber();
            if (string.IsNullOrEmpty(revision) || !revision.StartsWith("33.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The attached automation server is not SOLIDWORKS 2025; revision=" + revision);
            }

            assemblyDocument = application.GetOpenDocumentByName(expectedAssemblyPath) as ModelDoc2;
            if (assemblyDocument == null)
            {
                throw new InvalidOperationException(
                    "The V0.3 open assembly is not already open in the current SOLIDWORKS 2025 session. " +
                    "This exporter will not open or modify another assembly.");
            }

            string actualAssemblyPath = Path.GetFullPath(assemblyDocument.GetPathName());
            if (!string.Equals(actualAssemblyPath, expectedAssemblyPath, StringComparison.OrdinalIgnoreCase) ||
                !(assemblyDocument is AssemblyDoc))
            {
                throw new InvalidOperationException(
                    "The attached document is not the exact Rack4Modules V0.3 open assembly: " + actualAssemblyPath);
            }

            int activationError = 0;
            ModelDoc2 activated = application.ActivateDoc3(
                assemblyDocument.GetTitle(),
                false,
                (int)swRebuildOnActivation_e.swDontRebuildActiveDoc,
                ref activationError) as ModelDoc2;

            if (activated == null ||
                !string.Equals(Path.GetFullPath(activated.GetPathName()), expectedAssemblyPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "SOLIDWORKS could not activate the exact V0.3 assembly; status=" +
                    activationError.ToString(CultureInfo.InvariantCulture));
            }

            application.Visible = true;
            application.UserControl = true;
            assemblyDocument.Visible = true;

            Log("ATTACHED_SOLIDWORKS_REVISION=" + revision);
            Log("SOURCE_ASSEMBLY=" + actualAssemblyPath);

            // The project coordinate convention places the module face at z=0 and the broad back at z=110.
            // Therefore the standard Back view looks toward the module face, while Front shows the VESA back.
            ExportView(
                assemblyDocument,
                exportsDirectory,
                "Rack4Modules_OpenCase_V03_01_ModuleFront.png",
                swStandardViews_e.swBackView,
                "module-front");

            ExportView(
                assemblyDocument,
                exportsDirectory,
                "Rack4Modules_OpenCase_V03_02_Isometric.png",
                swStandardViews_e.swIsometricView,
                "isometric");

            // Looking down the +Y side exposes the x-z rear-edge plane and its TRS/handle/DIN/USB/power zones.
            ExportView(
                assemblyDocument,
                exportsDirectory,
                "Rack4Modules_OpenCase_V03_03_RearEdge_IO.png",
                swStandardViews_e.swTopView,
                "rear-edge-interface");

            ExportView(
                assemblyDocument,
                exportsDirectory,
                "Rack4Modules_OpenCase_V03_04_Back_VESA.png",
                swStandardViews_e.swFrontView,
                "broad-back-vesa");

            Log("PREVIEW_EXPORT_COMPLETE=true");
        }
        catch (Exception exception)
        {
            exportFailure = exception;
        }
        finally
        {
            if (assemblyDocument != null)
            {
                try
                {
                    SetView(assemblyDocument, swStandardViews_e.swIsometricView);
                    Log("FINAL_VIEW=isometric");
                }
                catch (Exception restoreException)
                {
                    if (exportFailure == null)
                    {
                        exportFailure = new InvalidOperationException(
                            "Preview files were exported, but the final isometric view could not be restored.",
                            restoreException);
                    }
                    else
                    {
                        Console.Error.WriteLine("RESTORE_WARNING=" + restoreException.Message);
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

    private static void ExportView(
        ModelDoc2 document,
        string exportsDirectory,
        string pngFileName,
        swStandardViews_e view,
        string viewLabel)
    {
        string pngPath = SafePathInside(exportsDirectory, Path.Combine(exportsDirectory, pngFileName));
        string ownershipToken = ".rack4modules-v03-preview-" +
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
                    "SOLIDWORKS could not render the " + viewLabel + " bitmap; success=" + saved.ToString());
            }

            using (Bitmap bitmap = new Bitmap(bitmapPath))
            {
                bitmap.Save(pngPath, ImageFormat.Png);
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

            Log("PNG=" + pngPath + "; bytes=" + png.Length.ToString(CultureInfo.InvariantCulture));
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

    private static void DeleteOwnedTemporaryBitmap(
        string exportsDirectory,
        string bitmapPath,
        string ownershipToken)
    {
        string safeBitmapPath = SafePathInside(exportsDirectory, bitmapPath);
        string fileName = Path.GetFileName(safeBitmapPath);
        if (!string.Equals(fileName, ownershipToken + ".bmp", StringComparison.Ordinal) ||
            !ownershipToken.StartsWith(".rack4modules-v03-preview-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Refusing to delete a bitmap that was not created by this preview export: " + safeBitmapPath);
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
            throw new InvalidOperationException("Path escapes the Rack4Modules exports directory: " + fullPath);
        }

        return fullPath;
    }

    private static void Log(string message)
    {
        Console.WriteLine("[RackV03Preview] " + message);
        Console.Out.Flush();
    }
}
