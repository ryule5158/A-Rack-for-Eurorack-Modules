using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class SaveRackProjectDocuments
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Exactly one existing project root is required.");
            }

            string root = Path.GetFullPath(arguments[0]);
            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException(root);
            }

            SldWorks application = Attach();
            int saved = 0;
            int inspected = 0;
            ModelDoc2 document = application.GetFirstDocument() as ModelDoc2;
            while (document != null)
            {
                inspected++;
                string fullPath = document.GetPathName();
                string title = document.GetTitle();

                if (string.IsNullOrWhiteSpace(fullPath))
                {
                    throw new InvalidOperationException("Refusing unnamed open document: " + title);
                }

                fullPath = Path.GetFullPath(fullPath);
                if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Refusing non-project open document: " + fullPath);
                }

                if (document.GetSaveFlag())
                {
                    int errors = 0;
                    int warnings = 0;
                    bool savedOk = document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                        ref errors, ref warnings);
                    if (!savedOk || errors != 0)
                    {
                        throw new InvalidOperationException("Save failed for " + fullPath + "; errors=" +
                            errors.ToString(CultureInfo.InvariantCulture));
                    }

                    saved++;
                    Console.WriteLine("SAVED_PROJECT_DOCUMENT=" + fullPath + ";warnings=" +
                        warnings.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    Console.WriteLine("CLEAN_PROJECT_DOCUMENT=" + fullPath);
                }

                document = document.GetNext() as ModelDoc2;
            }

            Console.WriteLine("INSPECTED=" + inspected.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("SAVED=" + saved.ToString(CultureInfo.InvariantCulture));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("SAVE_PROJECT_DOCUMENTS_FAILED=" + exception);
            return 1;
        }
    }

    private static SldWorks Attach()
    {
        foreach (string identifier in new string[] { "SldWorks.Application.33", "SldWorks.Application" })
        {
            try
            {
                SldWorks application = Marshal.GetActiveObject(identifier) as SldWorks;
                if (application != null)
                {
                    return application;
                }
            }
            catch (COMException)
            {
                // Keep searching for the already running project session.
            }
        }

        throw new InvalidOperationException("No already running SOLIDWORKS 2025 session was found.");
    }
}
