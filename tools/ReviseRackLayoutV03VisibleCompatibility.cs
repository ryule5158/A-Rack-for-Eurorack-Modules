using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class ReviseRackLayoutV03VisibleCompatibility
{
    private static readonly HashSet<string> GeneratedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BackPanel_V03_VESAOnly",
        "SideFrame_V03_RecessedLeg",
        "RearEdge_V03_IO_Handle_Power",
        "LowerEdge_V03_HiddenVent",
        "RearEdgeAudio_V03_8xTRS635",
        "RearEdgeMidiUsb_V03_3xDIN_USB_C",
        "RearEdgePowerBlank_V03",
        "RearCarryHandle_V03_Reference",
        "SideRecessedLeg_V03_TwoPosition",
        "InternalLidCatch_V03",
        "FourBackFeet_V03"
    };

    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Exactly one existing Rack4Modules project root is required.");
            }

            string root = Path.GetFullPath(arguments[0]);
            RackCadSession cad = new RackCadSession(root);
            SaveProjectDocumentsAndRecoverOwnScratch(cad);

            EdgeReferencedLayoutBuilder builder = new EdgeReferencedLayoutBuilder(cad);
            Invoke(builder, "VerifyExistingSessionIsProjectOnly");

            // SOLIDWORKS 2025 requires a visible part to activate it for native SaveAs.
            // Every generated part closes immediately after saving; the open-case
            // assembly is therefore the only lasting project page shown to the user.
            cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
            cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
            cad.Application.Visible = true;
            cad.Application.UserControl = true;

            cad.Log("V03_REFERENCE=Intellijel Gen-2 primary; Befaco 7U secondary");
            cad.Log("V03_LAYOUT=Rear-facing narrow edge I/O; broad back reserved for VESA");

            string[] partStages = new string[]
            {
                "BuildBroadBackV03",
                "BuildSideFrameV03",
                "BuildRearIoEdge",
                "BuildLowerVentEdge",
                "BuildAudioEdgeCassette",
                "BuildMidiUsbEdgeCassette",
                "BuildPowerBlankEdgeCassette",
                "BuildRearCarryHandle",
                "BuildSideLeg",
                "BuildInternalLidCatch",
                "BuildFourBackFeet"
            };

            foreach (string stage in partStages)
            {
                cad.Log("V03_STAGE=" + stage);
                Invoke(builder, stage);
            }

            Invoke(builder, "RegisterExistingParts");
            Invoke(builder, "ConfigurePlacements");

            if (!cad.Application.CloseAllDocuments(true))
            {
                throw new InvalidOperationException("SOLIDWORKS would not close the already-saved project documents.");
            }

            cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
            Invoke(builder, "BuildVisibleOpenAssembly");
            Invoke(builder, "WriteReport");
            cad.Log("V03_VISIBLE_BUILD_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V03_VISIBLE_BUILD_FAILED=" + exception);
            return 1;
        }
    }

    private static void SaveProjectDocumentsAndRecoverOwnScratch(RackCadSession cad)
    {
        List<ModelDoc2> documents = new List<ModelDoc2>();
        ModelDoc2 current = cad.Application.GetFirstDocument() as ModelDoc2;
        while (current != null)
        {
            documents.Add(current);
            current = current.GetNext() as ModelDoc2;
        }

        string projectPrefix = cad.Root.TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        foreach (ModelDoc2 document in documents)
        {
            string path = document.GetPathName();
            string title = document.GetTitle();

            if (string.IsNullOrWhiteSpace(path))
            {
                string property = null;
                string resolved = null;
                document.Extension.CustomPropertyManager[string.Empty].Get2("Project", out property, out resolved);

                if (!GeneratedTitles.Contains(title) ||
                    (!string.Equals(property, "Rack4Modules V0.3 reference-edge layout", StringComparison.Ordinal) &&
                     !string.Equals(resolved, "Rack4Modules V0.3 reference-edge layout", StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("Refusing an unrelated unnamed SOLIDWORKS document: " + title);
                }

                cad.Application.CloseDoc(title);
                cad.Log("RECOVERED_OWN_GENERATED_SCRATCH=" + title);
                continue;
            }

            string fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Refusing unrelated open SOLIDWORKS document: " + fullPath);
            }

            if (document.GetSaveFlag())
            {
                int errors = 0;
                int warnings = 0;
                if (!document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errors, ref warnings) || errors != 0)
                {
                    throw new InvalidOperationException("Cannot preserve project document " + fullPath + "; errors=" +
                        errors.ToString(CultureInfo.InvariantCulture));
                }

                cad.Log("PRESERVED_PROJECT_DOCUMENT=" + fullPath);
            }
        }
    }

    private static void Invoke(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new MissingMethodException(target.GetType().FullName, name);
        }

        try
        {
            method.Invoke(target, null);
        }
        catch (TargetInvocationException exception)
        {
            if (exception.InnerException != null)
            {
                throw new InvalidOperationException("V0.3 stage failed: " + name, exception.InnerException);
            }

            throw;
        }
    }
}
