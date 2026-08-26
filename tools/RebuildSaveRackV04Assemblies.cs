using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class RebuildSaveRackV04Assemblies
{
    private static readonly string[] ExactAssemblyFileNames = new string[]
    {
        "Rack4Modules_OpenCase_V04.SLDASM",
        "Rack4Modules_TransportClosed_V04.SLDASM",
        "Rack4Modules_ClearanceCheck_V04.SLDASM",
        "Rack4Modules_DesktopTilt60_V04.SLDASM",
        "Rack4Modules_DesktopTilt75_V04.SLDASM"
    };

    private const string FinalAssemblyFileName = "Rack4Modules_DesktopTilt60_V04.SLDASM";

    [STAThread]
    private static int Main(string[] arguments)
    {
        SldWorks application = null;
        List<AssemblyTarget> targets = null;
        List<string> failures = new List<string>();
        Exception fatalFailure = null;

        try
        {
            if (arguments == null || arguments.Length != 1 ||
                string.IsNullOrWhiteSpace(arguments[0]))
            {
                throw new ArgumentException(
                    "Exactly one Rack4Modules project-root argument is required.");
            }

            string root = Path.GetFullPath(arguments[0]);
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException(
                    "The supplied Rack4Modules project root does not exist: " + root);
            }

            string assembliesDirectory = Path.GetFullPath(
                Path.Combine(root, "cad", "assemblies"));
            if (!Directory.Exists(assembliesDirectory))
            {
                throw new DirectoryNotFoundException(
                    "The supplied project has no cad\\assemblies directory: " + assembliesDirectory);
            }

            targets = BuildAndValidateTargets(assembliesDirectory);
            application = AttachToRunningSolidWorks2025();

            string revision = application.RevisionNumber();
            if (string.IsNullOrEmpty(revision) ||
                !revision.StartsWith("33.", StringComparison.Ordinal))
            {
                application = null;
                throw new InvalidOperationException(
                    "The attached automation server is not SOLIDWORKS 2025; revision=" + revision);
            }

            Log("ATTACHED_SOLIDWORKS_REVISION=" + revision);
            Log("ASSEMBLY_WHITELIST_DIRECTORY=" + assembliesDirectory);
            Log("ASSEMBLY_WHITELIST_COUNT=" +
                targets.Count.ToString(CultureInfo.InvariantCulture));

            for (int index = 0; index < targets.Count; index++)
            {
                AssemblyTarget target = targets[index];
                try
                {
                    RebuildAndSaveOne(application, target, index + 1, targets.Count);
                }
                catch (Exception exception)
                {
                    string failure = target.FileName + ": " + exception.ToString();
                    failures.Add(failure);
                    Console.Error.WriteLine("ASSEMBLY_REBUILD_SAVE_FAILURE=" + failure);
                    Console.Error.Flush();
                }
            }
        }
        catch (Exception exception)
        {
            fatalFailure = exception;
        }
        finally
        {
            if (application != null && targets != null)
            {
                try
                {
                    AssemblyTarget finalTarget = FindTarget(targets, FinalAssemblyFileName);
                    ModelDoc2 finalDocument = OpenExactAssembly(application, finalTarget);
                    ShowFinalDesktopTilt60(application, finalDocument, finalTarget.Path);
                }
                catch (Exception exception)
                {
                    string failure = "FINAL_DESKTOP_TILT60: " + exception.ToString();
                    failures.Add(failure);
                    Console.Error.WriteLine("FINAL_DISPLAY_FAILURE=" + failure);
                    Console.Error.Flush();
                }
            }
        }

        if (fatalFailure != null)
        {
            Console.Error.WriteLine("V04_REBUILD_SAVE_FATAL=" + fatalFailure.ToString());
            Console.Error.Flush();
            return 1;
        }

        if (failures.Count != 0)
        {
            Console.Error.WriteLine("V04_REBUILD_SAVE_FAILED_COUNT=" +
                failures.Count.ToString(CultureInfo.InvariantCulture));
            Console.Error.Flush();
            return 1;
        }

        Log("V04_ASSEMBLY_REBUILD_SAVE_COMPLETE=true");
        return 0;
    }

    private static List<AssemblyTarget> BuildAndValidateTargets(string assembliesDirectory)
    {
        string exactDirectory = Path.GetFullPath(assembliesDirectory);
        string safePrefix = exactDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        List<AssemblyTarget> targets = new List<AssemblyTarget>();

        for (int index = 0; index < ExactAssemblyFileNames.Length; index++)
        {
            string exactFileName = ExactAssemblyFileNames[index];
            string candidate = Path.GetFullPath(Path.Combine(exactDirectory, exactFileName));

            if (!candidate.StartsWith(safePrefix, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    Path.GetDirectoryName(candidate),
                    exactDirectory,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    Path.GetFileName(candidate),
                    exactFileName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A V0.4 assembly whitelist path escaped cad\\assemblies: " + candidate);
            }

            if (!File.Exists(candidate))
            {
                throw new FileNotFoundException(
                    "A required exact V0.4 assembly is missing.",
                    candidate);
            }

            targets.Add(new AssemblyTarget(exactFileName, candidate));
        }

        return targets;
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
            "This tool intentionally does not start SOLIDWORKS.",
            lastFailure);
    }

    private static void RebuildAndSaveOne(
        SldWorks application,
        AssemblyTarget target,
        int ordinal,
        int total)
    {
        Log("BEGIN_ASSEMBLY=" + ordinal.ToString(CultureInfo.InvariantCulture) + "/" +
            total.ToString(CultureInfo.InvariantCulture) + "; path=" + target.Path);

        ModelDoc2 document = OpenExactAssembly(application, target);
        ActivateExactAssembly(application, document, target.Path);

        bool dirtyBeforeRebuild = document.GetSaveFlag();
        bool rebuildSucceeded = document.ForceRebuild3(false);
        bool dirtyAfterRebuild = document.GetSaveFlag();

        Log("REBUILD_RESULT=" + target.FileName +
            "; success=" + rebuildSucceeded.ToString() +
            "; dirty_before=" + dirtyBeforeRebuild.ToString() +
            "; dirty_after=" + dirtyAfterRebuild.ToString() +
            "; warning_channel=not_exposed_by_ForceRebuild3");

        int saveErrors = 0;
        int saveWarnings = 0;
        bool saveSucceeded = document.Save3(
            (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
            ref saveErrors,
            ref saveWarnings);
        bool dirtyAfterSave = document.GetSaveFlag();

        Log("SAVE_RESULT=" + target.FileName +
            "; success=" + saveSucceeded.ToString() +
            "; errors=" + saveErrors.ToString(CultureInfo.InvariantCulture) +
            "; warnings=" + saveWarnings.ToString(CultureInfo.InvariantCulture) +
            "; dirty_after=" + dirtyAfterSave.ToString());

        if (!rebuildSucceeded)
        {
            throw new InvalidOperationException(
                "ForceRebuild3(false) reported failure for " + target.Path +
                "; Save3 was still attempted and recorded.");
        }

        if (!saveSucceeded || saveErrors != 0 || dirtyAfterSave)
        {
            throw new InvalidOperationException(
                "Silent native save did not complete cleanly for " + target.Path +
                "; success=" + saveSucceeded.ToString() +
                "; errors=" + saveErrors.ToString(CultureInfo.InvariantCulture) +
                "; warnings=" + saveWarnings.ToString(CultureInfo.InvariantCulture) +
                "; dirty_after=" + dirtyAfterSave.ToString());
        }

        Log("END_ASSEMBLY=" + target.FileName);
    }

    private static ModelDoc2 OpenExactAssembly(SldWorks application, AssemblyTarget target)
    {
        ModelDoc2 document = application.GetOpenDocumentByName(target.Path) as ModelDoc2;
        int openErrors = 0;
        int openWarnings = 0;
        bool wasAlreadyOpen = document != null;

        if (document == null)
        {
            document = application.OpenDoc6(
                target.Path,
                (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                string.Empty,
                ref openErrors,
                ref openWarnings) as ModelDoc2;
        }

        Log("OPEN_RESULT=" + target.FileName +
            "; already_open=" + wasAlreadyOpen.ToString() +
            "; errors=" + openErrors.ToString(CultureInfo.InvariantCulture) +
            "; warnings=" + openWarnings.ToString(CultureInfo.InvariantCulture));

        if (document == null || openErrors != 0)
        {
            throw new InvalidOperationException(
                "SOLIDWORKS could not open the exact whitelisted assembly; errors=" +
                openErrors.ToString(CultureInfo.InvariantCulture) + "; warnings=" +
                openWarnings.ToString(CultureInfo.InvariantCulture) + "; path=" + target.Path);
        }

        string actualPath = document.GetPathName();
        if (string.IsNullOrWhiteSpace(actualPath))
        {
            throw new InvalidOperationException(
                "SOLIDWORKS returned an unnamed document for " + target.Path);
        }

        actualPath = Path.GetFullPath(actualPath);
        if (!(document is AssemblyDoc) ||
            !string.Equals(actualPath, target.Path, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SOLIDWORKS returned a document outside the exact V0.4 whitelist; expected=" +
                target.Path + "; actual=" + actualPath);
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

        string actualPath = activated == null || string.IsNullOrWhiteSpace(activated.GetPathName())
            ? string.Empty
            : Path.GetFullPath(activated.GetPathName());
        if (activated == null ||
            !string.Equals(actualPath, expectedPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SOLIDWORKS could not activate the exact whitelisted assembly; status=" +
                activationError.ToString(CultureInfo.InvariantCulture) + "; expected=" +
                expectedPath + "; actual=" + actualPath);
        }

        activated.Visible = true;
        Log("ACTIVATED=" + actualPath + "; status=" +
            activationError.ToString(CultureInfo.InvariantCulture));
    }

    private static AssemblyTarget FindTarget(List<AssemblyTarget> targets, string exactFileName)
    {
        for (int index = 0; index < targets.Count; index++)
        {
            if (string.Equals(
                targets[index].FileName,
                exactFileName,
                StringComparison.Ordinal))
            {
                return targets[index];
            }
        }

        throw new InvalidOperationException(
            "The final display assembly is absent from the exact whitelist: " + exactFileName);
    }

    private static void ShowFinalDesktopTilt60(
        SldWorks application,
        ModelDoc2 document,
        string expectedPath)
    {
        application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
        application.Visible = true;
        application.UserControl = true;
        ActivateExactAssembly(application, document, expectedPath);
        document.ShowNamedView2(string.Empty, (int)swStandardViews_e.swIsometricView);
        document.ViewDisplayShaded();
        document.ViewZoomtofit2();
        document.GraphicsRedraw2();
        application.FrameState = (int)swWindowState_e.swWindowMaximized;
        Log("FINAL_DOCUMENT=" + Path.GetFullPath(document.GetPathName()));
        Log("FINAL_VIEW=desktop-tilt-60-isometric");
    }

    private static void Log(string message)
    {
        Console.WriteLine("[RackV04RebuildSave] " + message);
        Console.Out.Flush();
    }

    private sealed class AssemblyTarget
    {
        internal readonly string FileName;
        internal readonly string Path;

        internal AssemblyTarget(string fileName, string path)
        {
            FileName = fileName;
            Path = path;
        }
    }
}
