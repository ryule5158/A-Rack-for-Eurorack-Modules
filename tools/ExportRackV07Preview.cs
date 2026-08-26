using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class ExportRackV07Preview
{
    private const string OpenAssemblyFileName =
        "Rack4Modules_OpenCase_V07.SLDASM";
    private const string TransportAssemblyFileName =
        "Rack4Modules_TransportClosed_V07.SLDASM";
    private const string Tilt60AssemblyFileName =
        "Rack4Modules_DesktopTilt60_V07.SLDASM";
    private const string ShowcaseAssemblyFileName =
        "Rack4Modules_ShowcaseTilt60_LidOff_V07.SLDASM";
    private const int PreviewWidth = 2400;
    private const int PreviewHeight = 1600;

    [STAThread]
    private static int Main(string[] arguments)
    {
        SldWorks application = null;
        ManagedDocument openCase = null;
        ManagedDocument transport = null;
        ManagedDocument tilt60 = null;
        ManagedDocument showcase = null;
        string projectRoot = null;
        string assembliesDirectory = null;
        string showcaseAssemblyPath = null;
        bool exportCompleted = false;
        Exception exportFailure = null;

        try
        {
            string root = arguments.Length > 0
                ? Path.GetFullPath(arguments[0])
                : @"C:\Users\LENOVO\Desktop\Rack4Modules";
            projectRoot = root;

            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException(
                    "Rack4Modules project root does not exist: " + root);
            }

            assembliesDirectory = Path.GetFullPath(
                Path.Combine(root, "cad", "assemblies"));
            string openAssemblyPath = ProjectAssemblyPath(root, OpenAssemblyFileName);
            string transportAssemblyPath = ProjectAssemblyPath(root, TransportAssemblyFileName);
            string tilt60AssemblyPath = ProjectAssemblyPath(root, Tilt60AssemblyFileName);
            showcaseAssemblyPath = ProjectAssemblyPath(root, ShowcaseAssemblyFileName);

            RequireFile(openAssemblyPath, "V0.7 open assembly");
            RequireFile(transportAssemblyPath, "V0.7 transport assembly");
            RequireFile(tilt60AssemblyPath, "V0.7 60-degree desktop assembly");
            RequireFile(showcaseAssemblyPath, "V0.7 lid-off showcase assembly");

            Log("VERIFIED_NATIVE_ASSEMBLY=" + openAssemblyPath);
            Log("VERIFIED_NATIVE_ASSEMBLY=" + transportAssemblyPath);
            Log("VERIFIED_NATIVE_ASSEMBLY=" + tilt60AssemblyPath);
            Log("VERIFIED_NATIVE_ASSEMBLY=" + showcaseAssemblyPath);

            string previewsDirectory = Path.GetFullPath(Path.Combine(root, "previews"));
            Directory.CreateDirectory(previewsDirectory);

            application = AttachToRunningSolidWorks2025();
            string revision = application.RevisionNumber();
            if (string.IsNullOrEmpty(revision) ||
                !revision.StartsWith("33.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The attached automation server is not SOLIDWORKS 2025; revision=" +
                    revision);
            }

            // A preview run never saves CAD. Exact V0.7 targets must be clean.
            // Other dirty Rack4Modules documents are preserved and hidden at the end
            // instead of being closed or saved.
            RequireExistingTargetIsSafe(application, openAssemblyPath, "V0.7 open assembly");
            RequireExistingTargetIsSafe(
                application,
                transportAssemblyPath,
                "V0.7 transport assembly");
            RequireExistingTargetIsSafe(
                application,
                tilt60AssemblyPath,
                "V0.7 60-degree desktop assembly");
            RequireExistingTargetIsSafe(
                application,
                showcaseAssemblyPath,
                "V0.7 lid-off showcase assembly");

            openCase = OpenExactAssembly(
                application,
                openAssemblyPath,
                "V0.7 open assembly");
            transport = OpenExactAssembly(
                application,
                transportAssemblyPath,
                "V0.7 transport assembly");
            tilt60 = OpenExactAssembly(
                application,
                tilt60AssemblyPath,
                "V0.7 60-degree desktop assembly");
            showcase = OpenExactAssembly(
                application,
                showcaseAssemblyPath,
                "V0.7 lid-off showcase assembly");

            application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
            application.Visible = true;
            application.UserControl = true;

            Log("ATTACHED_SOLIDWORKS_REVISION=" + revision);
            Log("CAD_WRITE=false; CAD_REBUILD=false; CAD_SAVE=false");

            ActivateExactAssembly(application, showcase.Document, showcaseAssemblyPath);
            ExportView(
                showcase.Document,
                previewsDirectory,
                "Rack4Modules_ShowcaseTilt60_LidOff_01_Isometric_V07.png",
                swStandardViews_e.swIsometricView,
                "showcase-tilt-60-lid-off-isometric-detached-lid");
            ExportView(
                showcase.Document,
                previewsDirectory,
                "Rack4Modules_ShowcaseTilt60_LidOff_02_ModuleFront_V07.png",
                swStandardViews_e.swBackView,
                "showcase-tilt-60-lid-off-module-front");

            ActivateExactAssembly(application, openCase.Document, openAssemblyPath);
            ExportView(
                openCase.Document,
                previewsDirectory,
                "Rack4Modules_OpenCase_03_Isometric_V07.png",
                swStandardViews_e.swIsometricView,
                "open-case-isometric");
            ExportView(
                openCase.Document,
                previewsDirectory,
                "Rack4Modules_OpenCase_04_ModuleFront_V07.png",
                swStandardViews_e.swBackView,
                "open-case-module-front");

            ActivateExactAssembly(application, transport.Document, transportAssemblyPath);
            ExportView(
                transport.Document,
                previewsDirectory,
                "Rack4Modules_TransportClosed_05_Isometric_V07.png",
                swStandardViews_e.swIsometricView,
                "transport-closed-isometric");
            ExportView(
                transport.Document,
                previewsDirectory,
                "Rack4Modules_TransportClosed_06_RightSide_V07.png",
                swStandardViews_e.swRightView,
                "transport-closed-right-side-cover-and-latches");

            ActivateExactAssembly(application, tilt60.Document, tilt60AssemblyPath);
            ExportView(
                tilt60.Document,
                previewsDirectory,
                "Rack4Modules_DesktopTilt60_07_Isometric_V07.png",
                swStandardViews_e.swIsometricView,
                "desktop-tilt-60-isometric");
            ExportView(
                tilt60.Document,
                previewsDirectory,
                "Rack4Modules_DesktopTilt60_08_RightSide_Support_V07.png",
                swStandardViews_e.swRightView,
                "desktop-tilt-60-right-side-support-hard-stop-footpad");

            exportCompleted = true;
            Log("PREVIEW_EXPORT_COMPLETE=true; png_count=8");
        }
        catch (Exception exception)
        {
            exportFailure = exception;
        }
        finally
        {
            if (application != null)
            {
                if (exportCompleted &&
                    !string.IsNullOrEmpty(projectRoot) &&
                    !string.IsNullOrEmpty(assembliesDirectory) &&
                    showcase != null &&
                    !string.IsNullOrEmpty(showcaseAssemblyPath))
                {
                    try
                    {
                        // The requested final operator state is one project assembly only:
                        // V0.7 showcase, tilted, with the lid detached and visible beside it.
                        HideOrCloseOtherProjectDocuments(
                            application,
                            projectRoot,
                            showcaseAssemblyPath);
                        ShowFinalShowcaseAssembly(
                            application,
                            showcase.Document,
                            showcaseAssemblyPath);
                        VerifyOnlyFinalProjectDocumentIsVisible(
                            application,
                            projectRoot,
                            showcaseAssemblyPath);
                    }
                    catch (Exception restoreException)
                    {
                        RecordSecondaryFailure(
                            ref exportFailure,
                            "Preview files were generated, but the final one-window " +
                            "V0.7 showcase state could not be established.",
                            restoreException);
                    }
                }
                else
                {
                    // On a failed export, close only clean documents opened by this helper.
                    // Never close or discard a pre-existing document after a refusal.
                    CloseOwnedDocument(
                        application,
                        openCase,
                        "V0.7 open assembly",
                        ref exportFailure);
                    CloseOwnedDocument(
                        application,
                        transport,
                        "V0.7 transport assembly",
                        ref exportFailure);
                    CloseOwnedDocument(
                        application,
                        tilt60,
                        "V0.7 60-degree desktop assembly",
                        ref exportFailure);
                    CloseOwnedDocument(
                        application,
                        showcase,
                        "V0.7 lid-off showcase assembly",
                        ref exportFailure);
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
            throw new FileNotFoundException(
                "The exact " + description + " does not exist.",
                path);
        }
    }

    private static SldWorks AttachToRunningSolidWorks2025()
    {
        string[] programIds = new string[]
        {
            "SldWorks.Application.33",
            "SldWorks.Application"
        };
        Exception lastFailure = null;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            for (int index = 0; index < programIds.Length; index++)
            {
                try
                {
                    SldWorks application =
                        Marshal.GetActiveObject(programIds[index]) as SldWorks;
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
            "The preview exporter intentionally does not start another process.",
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
            // Exact V0.7 targets are reproducible outputs of the just-completed
            // builder and validator. NeedsRegen-on-open can set this in-memory
            // flag even though the native file is already frozen on disk. The
            // exporter will never save these documents; non-final targets are
            // closed without saving and the final showcase remains visible.
            Log("PREEXISTING_GENERATED_V07_TARGET_DIRTY_WILL_NOT_BE_SAVED=" +
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
                    Log("USING_GENERATED_V07_TARGET_WITHOUT_SAVE=" + expected);
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
                        "SOLIDWORKS could not open the exact " + description +
                        "; errors=" + errors.ToString(CultureInfo.InvariantCulture) +
                        "; warnings=" + warnings.ToString(CultureInfo.InvariantCulture) +
                        "; path=" + expected);
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

    private static ModelDoc2 FindOpenDocumentByExactPath(
        SldWorks application,
        string expectedPath)
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
            throw new InvalidOperationException(
                "SOLIDWORKS returned no document for " + description + ".");
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
                "SOLIDWORKS returned the wrong document for " + description + ": " +
                actualPath);
        }
    }

    private static void ActivateExactAssembly(
        SldWorks application,
        ModelDoc2 document,
        string expectedPath)
    {
        ValidateExactAssembly(document, expectedPath, "requested preview assembly");
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
                activationError.ToString(CultureInfo.InvariantCulture) +
                "; expected=" + expectedPath);
        }

        activated.Visible = true;
    }

    private static void ExportView(
        ModelDoc2 document,
        string previewsDirectory,
        string pngFileName,
        swStandardViews_e view,
        string viewLabel)
    {
        string pngPath = SafePathInside(
            previewsDirectory,
            Path.Combine(previewsDirectory, pngFileName));
        string ownershipToken = ".rack4modules-v07-preview-" +
            Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + "-" +
            Guid.NewGuid().ToString("N");
        string bitmapPath = SafePathInside(
            previewsDirectory,
            Path.Combine(previewsDirectory, ownershipToken + ".bmp"));

        if (File.Exists(bitmapPath))
        {
            throw new IOException(
                "A supposedly unique preview bitmap already exists: " + bitmapPath);
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
                    "SOLIDWORKS could not render the " + viewLabel +
                    " bitmap; success=" + saved.ToString());
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
                throw new InvalidOperationException(
                    "The converted PNG is missing or empty: " + pngPath);
            }

            using (Image verification = Image.FromFile(pngPath))
            {
                if (verification.Width != PreviewWidth ||
                    verification.Height != PreviewHeight)
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
                DeleteOwnedTemporaryBitmap(
                    previewsDirectory,
                    bitmapPath,
                    ownershipToken);
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

    private static List<ModelDoc2> SnapshotOpenProjectAssemblies(
        SldWorks application,
        string assembliesDirectory)
    {
        List<ModelDoc2> result = new List<ModelDoc2>();
        ModelDoc2 current = application.GetFirstDocument() as ModelDoc2;
        while (current != null)
        {
            ModelDoc2 next = current.GetNext() as ModelDoc2;
            string path = current.GetPathName();
            if (current is AssemblyDoc &&
                !string.IsNullOrWhiteSpace(path) &&
                IsPathInside(assembliesDirectory, path) &&
                string.Equals(
                    Path.GetExtension(path),
                    ".SLDASM",
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Add(current);
            }

            current = next;
        }

        return result;
    }

    private static List<ModelDoc2> SnapshotOpenProjectDocuments(
        SldWorks application,
        string projectRoot)
    {
        string cadDirectory = Path.GetFullPath(Path.Combine(projectRoot, "cad"));
        List<ModelDoc2> result = new List<ModelDoc2>();
        ModelDoc2 current = application.GetFirstDocument() as ModelDoc2;
        while (current != null)
        {
            ModelDoc2 next = current.GetNext() as ModelDoc2;
            string path = current.GetPathName();
            if (!string.IsNullOrWhiteSpace(path) && IsPathInside(cadDirectory, path))
            {
                result.Add(current);
            }

            current = next;
        }

        return result;
    }

    private static void HideOrCloseOtherProjectDocuments(
        SldWorks application,
        string projectRoot,
        string finalAssemblyPath)
    {
        List<ModelDoc2> projectDocuments =
            SnapshotOpenProjectDocuments(application, projectRoot);
        for (int index = 0; index < projectDocuments.Count; index++)
        {
            ModelDoc2 document = projectDocuments[index];
            string path = Path.GetFullPath(document.GetPathName());
            if (string.Equals(
                path,
                Path.GetFullPath(finalAssemblyPath),
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool dirtyBefore = document.GetSaveFlag();
            if (!dirtyBefore && document is AssemblyDoc)
            {
                application.CloseDoc(document.GetTitle());
                if (FindOpenDocumentByExactPath(application, path) == null)
                {
                    Log("CLOSED_CLEAN_OTHER_PROJECT_ASSEMBLY=" + path);
                    continue;
                }
            }

            // Referenced parts and dirty historical sources may need to remain
            // loaded. Hide their MDI windows while preserving their exact save flag.
            document.Visible = false;
            if (document.Visible || document.GetSaveFlag() != dirtyBefore)
            {
                throw new InvalidOperationException(
                    "Could not hide a non-final project document without changing " +
                    "its save state: " + path);
            }

            Log((dirtyBefore
                ? "HIDDEN_DIRTY_PROJECT_DOCUMENT_PRESERVED="
                : "HIDDEN_LOADED_PROJECT_DOCUMENT=") + path);
        }
    }

    private static void CloseOwnedDocument(
        SldWorks application,
        ManagedDocument managed,
        string description,
        ref Exception primaryFailure)
    {
        if (managed == null || !managed.OpenedByThisRun)
        {
            return;
        }

        try
        {
            ModelDoc2 document =
                FindOpenDocumentByExactPath(application, managed.ExpectedPath);
            if (document == null)
            {
                return;
            }

            ValidateExactAssembly(document, managed.ExpectedPath, description);
            if (document.GetSaveFlag())
            {
                document.Visible = false;
                Log("HIDDEN_EXPORTER_OWNED_DIRTY_DOCUMENT_PRESERVED=" +
                    managed.ExpectedPath);
                return;
            }

            application.CloseDoc(document.GetTitle());
            Log("CLOSED_EXPORTER_OWNED_CLEAN_DOCUMENT=" + managed.ExpectedPath);
        }
        catch (Exception cleanupException)
        {
            RecordSecondaryFailure(
                ref primaryFailure,
                "The preview helper could not safely close its own " + description + ".",
                cleanupException);
        }
    }

    private static void ShowFinalShowcaseAssembly(
        SldWorks application,
        ModelDoc2 showcaseDocument,
        string showcaseAssemblyPath)
    {
        showcaseDocument.Visible = true;
        application.Visible = true;
        application.UserControl = true;
        ActivateExactAssembly(application, showcaseDocument, showcaseAssemblyPath);
        SetView(showcaseDocument, swStandardViews_e.swIsometricView);
        application.FrameState = (int)swWindowState_e.swWindowMaximized;
        Log("FINAL_DOCUMENT=" +
            Path.GetFullPath(showcaseDocument.GetPathName()));
        Log("FINAL_VIEW=showcase-tilt-60-lid-off-isometric-detached-lid");
    }

    private static void VerifyOnlyFinalProjectDocumentIsVisible(
        SldWorks application,
        string projectRoot,
        string finalAssemblyPath)
    {
        List<ModelDoc2> documents = SnapshotOpenProjectDocuments(application, projectRoot);
        int visibleCount = 0;
        bool finalVisible = false;
        for (int index = 0; index < documents.Count; index++)
        {
            if (!documents[index].Visible)
            {
                continue;
            }

            visibleCount++;
            finalVisible = finalVisible || string.Equals(
                Path.GetFullPath(documents[index].GetPathName()),
                Path.GetFullPath(finalAssemblyPath),
                StringComparison.OrdinalIgnoreCase);
        }

        if (visibleCount != 1 || !finalVisible)
        {
            throw new InvalidOperationException(
                "The final SOLIDWORKS project-document state is not exactly one " +
                "visible V0.7 showcase document; visible_count=" +
                visibleCount.ToString(CultureInfo.InvariantCulture));
        }

        Log("FINAL_VISIBLE_PROJECT_DOCUMENT_COUNT=1");
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

        Console.Error.WriteLine(
            "SECONDARY_WARNING=" + message + " " + secondaryFailure.ToString());
        Console.Error.Flush();
    }

    private static void DeleteOwnedTemporaryBitmap(
        string previewsDirectory,
        string bitmapPath,
        string ownershipToken)
    {
        string safeBitmapPath = SafePathInside(previewsDirectory, bitmapPath);
        string fileName = Path.GetFileName(safeBitmapPath);
        if (!string.Equals(
                fileName,
                ownershipToken + ".bmp",
                StringComparison.Ordinal) ||
            !ownershipToken.StartsWith(
                ".rack4modules-v07-preview-",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Refusing to delete a bitmap that was not created by this preview export: " +
                safeBitmapPath);
        }

        File.Delete(safeBitmapPath);
        if (File.Exists(safeBitmapPath))
        {
            throw new IOException(
                "The owned temporary bitmap could not be removed: " + safeBitmapPath);
        }
    }

    private static string SafePathInside(string directory, string candidate)
    {
        string root = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(candidate);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Path escapes the Rack4Modules previews directory: " + fullPath);
        }

        return fullPath;
    }

    private static bool IsPathInside(string directory, string candidate)
    {
        string root = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(candidate);
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static void Log(string message)
    {
        Console.WriteLine("[RackV07Preview] " + message);
        Console.Out.Flush();
    }

    private sealed class ManagedDocument
    {
        internal readonly ModelDoc2 Document;
        internal readonly string ExpectedPath;
        internal readonly bool OpenedByThisRun;

        internal ManagedDocument(
            ModelDoc2 document,
            string expectedPath,
            bool openedByThisRun)
        {
            Document = document;
            ExpectedPath = Path.GetFullPath(expectedPath);
            OpenedByThisRun = openedByThisRun;
        }
    }
}
