using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class FinalizeAndValidateRack
{
    private const string MaterialName = "6061-T6 (SS)";
    private const string MaterialDatabase = @"E:\SW2025\SOLIDWORKS\lang\english\sldmaterials\solidworks materials.sldmat";

    private static readonly string[] Aluminium6061Parts =
    {
        "SideFrame_6061_3mm",
        "Rail_104HP_104xM3",
        "ThreadStrip_104HP_M3Pilot",
        "RailEndBlock_M3",
        "VesaReinforcement_100x100_M4",
        "RearCrossBeam_6061",
        "VesaStile_6061",
        "VesaBridge_6061",
        "TopFoldFlatHandle_Concept",
        "LidLatch_Concept",
        "FoldOutLeg_Concept",
        "FitGauge_104HP_3U"
    };

    private static SldWorks application;
    private static string root;
    private static readonly StringBuilder report = new StringBuilder();

    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Pass exactly one existing Rack4Modules project root.");
            }

            root = Path.GetFullPath(arguments[0]);
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException(root);
            }

            Type applicationType = Type.GetTypeFromProgID("SldWorks.Application.33", true);
            application = (SldWorks)Activator.CreateInstance(applicationType);
            application.Visible = false;
            application.UserControl = false;
            application.DocumentVisible(false, (int)swDocumentTypes_e.swDocPART);
            application.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);

            report.AppendLine("# Rack4Modules SolidWorks verification");
            report.AppendLine();
            report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            report.AppendLine("SOLIDWORKS revision: " + application.RevisionNumber());
            report.AppendLine();

            Log("STAGE=Correcting real 6061-T6 material assignments in the background");
            AssignAll6061Materials();

            Log("STAGE=Checking native part bodies and critical dimensions");
            VerifyPartInventoryAndBoxes();

            Log("STAGE=Checking assemblies, physical mass and component interference");
            VerifyAssembly("Rack4Modules_OpenCase", 49);
            VerifyAssembly("Rack4Modules_TransportClosed", 50);
            VerifyAssembly("Rack4Modules_ClearanceCheck", 54);

            string reportPath = Path.Combine(root, "reports", "solidworks-verification.md");
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
            Log("VERIFICATION_REPORT=" + reportPath);

            application.CloseAllDocuments(true);
            application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
            application.DocumentVisible(false, (int)swDocumentTypes_e.swDocPART);
            application.Visible = true;
            application.UserControl = true;

            ModelDoc2 visible = OpenDocument(Path.Combine(root, "cad", "assemblies", "Rack4Modules_OpenCase.SLDASM"),
                swDocumentTypes_e.swDocASSEMBLY);
            ShowDocument(visible);
            SavePreview(visible);

            Log("VISIBLE_DOCUMENT=Rack4Modules_OpenCase.SLDASM");
            Log("FINAL_DOCUMENT_COUNT=" + application.GetDocumentCount());
            Log("VERIFICATION_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FINALIZATION_FAILED=" + exception);
            return 1;
        }
    }

    private static void AssignAll6061Materials()
    {
        report.AppendLine("## Physical material assignments");
        report.AppendLine();
        report.AppendLine("The following structural parts were assigned and read back as `6061-T6 (SS)`:");
        report.AppendLine();

        foreach (string stem in Aluminium6061Parts)
        {
            string nativePath = Path.Combine(root, "cad", "parts", stem + ".SLDPRT");
            ModelDoc2 document = OpenDocument(nativePath, swDocumentTypes_e.swDocPART);
            PartDoc part = (PartDoc)document;

            part.SetMaterialPropertyName2(string.Empty, MaterialDatabase, MaterialName);
            string database;
            string actual = part.GetMaterialPropertyName2(string.Empty, out database);
            if (!string.Equals(actual, MaterialName, StringComparison.Ordinal))
            {
                Configuration configuration = document.ConfigurationManager.ActiveConfiguration;
                if (configuration == null)
                {
                    throw new InvalidOperationException("No active configuration exists for " + stem);
                }

                part.SetMaterialPropertyName2(configuration.Name, MaterialDatabase, MaterialName);
                actual = part.GetMaterialPropertyName2(configuration.Name, out database);
            }

            if (!string.Equals(actual, MaterialName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Physical material readback failed for " + stem + ": " + actual);
            }

            SaveDocument(document, nativePath);
            string existingStep = Path.Combine(root, "exports", stem + ".STEP");
            if (File.Exists(existingStep))
            {
                SaveDocument(document, existingStep);
            }

            report.AppendLine("- " + stem + ": " + actual);
            Log("MATERIAL_VERIFIED=" + stem + ":" + actual);
            application.CloseDoc(document.GetTitle());
        }

        report.AppendLine();
    }

    private static void VerifyPartInventoryAndBoxes()
    {
        string partDirectory = Path.Combine(root, "cad", "parts");
        string[] nativeParts = Directory.GetFiles(partDirectory, "*.SLDPRT");
        if (nativeParts.Length != 22)
        {
            throw new InvalidOperationException("Expected 22 native part files; actual " + nativeParts.Length);
        }

        Array.Sort(nativeParts, StringComparer.OrdinalIgnoreCase);
        report.AppendLine("## Native part inventory and solid bodies");
        report.AppendLine();

        foreach (string path in nativeParts)
        {
            ModelDoc2 document = OpenDocument(path, swDocumentTypes_e.swDocPART);
            PartDoc part = (PartDoc)document;
            Array bodies = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
            if (bodies == null || bodies.Length == 0)
            {
                throw new InvalidOperationException("A native part has no valid solid bodies: " + path);
            }

            string stem = Path.GetFileNameWithoutExtension(path);
            double[] box = part.GetPartBox(true) as double[];
            if (box == null || box.Length != 6)
            {
                throw new InvalidOperationException("No valid bounding box was returned for " + stem);
            }

            if (stem == "BackPanel_5052_2mm")
            {
                AssertBounds(stem, box, 548.0, 420.0, 2.0, 0.4);
            }
            else if (stem == "Rail_104HP_104xM3")
            {
                AssertBounds(stem, box, 528.32, 10.0, 12.0, 0.4);
            }
            else if (stem == "ThreadStrip_104HP_M3Pilot")
            {
                AssertBounds(stem, box, 528.32, 6.0, 2.0, 0.4);
            }
            else if (stem == "DeepTravelLid_70mmClearance")
            {
                AssertBounds(stem, box, 552.0, 424.0, 83.5, 0.5);
            }
            else if (stem == "FitGauge_104HP_3U")
            {
                AssertBounds(stem, box, 528.0, 128.5, 2.0, 0.4);
            }

            report.AppendLine("- " + stem + ": " + bodies.Length.ToString(CultureInfo.InvariantCulture) + " solid body/bodies.");
            application.CloseDoc(document.GetTitle());
        }

        report.AppendLine();
    }

    private static void VerifyAssembly(string stem, int expectedComponents)
    {
        string assemblyPath = Path.Combine(root, "cad", "assemblies", stem + ".SLDASM");
        ModelDoc2 document = OpenDocument(assemblyPath, swDocumentTypes_e.swDocASSEMBLY);
        AssemblyDoc assembly = (AssemblyDoc)document;
        assembly.ResolveAllLightWeightComponents(true);

        if (!document.ForceRebuild3(false))
        {
            throw new InvalidOperationException("Assembly rebuild failed: " + stem);
        }

        int actualComponents = assembly.GetComponentCount(false);
        if (actualComponents != expectedComponents)
        {
            throw new InvalidOperationException("Assembly component mismatch for " + stem +
                ": expected " + expectedComponents + ", actual " + actualComponents);
        }

        double[] box = assembly.GetBox(0) as double[];
        if (box == null || box.Length != 6)
        {
            throw new InvalidOperationException("Assembly bounding box is unavailable: " + stem);
        }

        double width = (box[3] - box[0]) * 1000.0;
        double height = (box[4] - box[1]) * 1000.0;
        double depth = (box[5] - box[2]) * 1000.0;

        MassProperty massProperties = document.Extension.CreateMassProperty();
        if (massProperties == null)
        {
            throw new InvalidOperationException("Assembly mass properties are unavailable: " + stem);
        }

        double mass = massProperties.Mass;
        if (double.IsNaN(mass) || double.IsInfinity(mass) || mass <= 0.0)
        {
            throw new InvalidOperationException("Assembly physical mass is invalid: " + stem);
        }

        InterferenceSummary interference = DetectInterference(assembly, stem);

        SaveDocument(document, assemblyPath);
        SaveDocument(document, Path.Combine(root, "exports", stem + ".STEP"));

        report.AppendLine("## " + stem);
        report.AppendLine();
        report.AppendLine("- Loaded native assembly components: " + actualComponents);
        report.AppendLine("- Overall envelope including external concept hardware: " +
            Format(width) + " x " + Format(height) + " x " + Format(depth) + " mm.");
        report.AppendLine("- SolidWorks material-derived mass: " + Format(mass) + " kg.");
        report.AppendLine("- Volumetric interferences, excluding mere coincident faces: " + interference.Count);

        foreach (string detail in interference.Details)
        {
            report.AppendLine("  - " + detail);
        }

        report.AppendLine();
        Log("ASSEMBLY_VERIFIED=" + stem + ":components=" + actualComponents +
            ":mass_kg=" + Format(mass) + ":interferences=" + interference.Count);
        application.CloseDoc(document.GetTitle());
    }

    private static InterferenceSummary DetectInterference(AssemblyDoc assembly, string stem)
    {
        InterferenceDetectionMgr manager = assembly.InterferenceDetectionManager;
        if (manager == null)
        {
            throw new InvalidOperationException("Interference detection manager unavailable for " + stem);
        }

        manager.TreatCoincidenceAsInterference = false;
        manager.IncludeMultibodyPartInterferences = false;
        manager.MakeInterferingPartsTransparent = false;
        manager.IgnoreHiddenBodies = false;

        InterferenceSummary summary = new InterferenceSummary();
        try
        {
            summary.Count = manager.GetInterferenceCount();
            Array interferences = manager.GetInterferences() as Array;
            if (interferences != null)
            {
                int limit = Math.Min(interferences.Length, 20);
                for (int index = 0; index < limit; index++)
                {
                    Interference interference = interferences.GetValue(index) as Interference;
                    if (interference == null)
                    {
                        continue;
                    }

                    Array components = interference.Components as Array;
                    List<string> names = new List<string>();
                    if (components != null)
                    {
                        foreach (object item in components)
                        {
                            Component2 component = item as Component2;
                            if (component != null)
                            {
                                names.Add(component.Name2);
                            }
                        }
                    }

                    double volume = interference.Volume * 1000000000.0;
                    summary.Details.Add(string.Join(" <-> ", names.ToArray()) +
                        " (" + Format(volume) + " mm^3)");
                }
            }
        }
        finally
        {
            manager.Done();
        }

        return summary;
    }

    private static ModelDoc2 OpenDocument(string path, swDocumentTypes_e type)
    {
        int errors = 0;
        int warnings = 0;
        ModelDoc2 document = application.OpenDoc6(path, (int)type,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
            string.Empty, ref errors, ref warnings) as ModelDoc2;

        if (document == null || errors != 0)
        {
            throw new InvalidOperationException("Unable to open native SOLIDWORKS document: " +
                path + "; errors=" + errors + "; warnings=" + warnings);
        }

        return document;
    }

    private static void SaveDocument(ModelDoc2 document, string path)
    {
        int activationError = 0;
        ModelDoc2 activated = application.ActivateDoc3(document.GetTitle(), false,
            (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref activationError) as ModelDoc2;
        if (activated == null)
        {
            throw new InvalidOperationException("Could not activate " + document.GetTitle());
        }

        document.ClearSelection2(true);
        document.ForceRebuild3(false);
        int errors = 0;
        int warnings = 0;
        bool saved = document.Extension.SaveAs(path,
            (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
            (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
            null, ref errors, ref warnings);

        if (!saved || errors != 0 || !File.Exists(path) || new FileInfo(path).Length == 0)
        {
            throw new InvalidOperationException("Document save failed: " + path +
                "; success=" + saved + "; errors=" + errors + "; warnings=" + warnings);
        }
    }

    private static void AssertBounds(string stem, double[] box,
        double width, double height, double depth, double tolerance)
    {
        double actualWidth = (box[3] - box[0]) * 1000.0;
        double actualHeight = (box[4] - box[1]) * 1000.0;
        double actualDepth = (box[5] - box[2]) * 1000.0;

        if (Math.Abs(actualWidth - width) > tolerance ||
            Math.Abs(actualHeight - height) > tolerance ||
            Math.Abs(actualDepth - depth) > tolerance)
        {
            throw new InvalidOperationException("Critical dimension check failed for " + stem +
                ": " + Format(actualWidth) + " x " + Format(actualHeight) + " x " + Format(actualDepth) + " mm");
        }

        Log("DIMENSION_VERIFIED=" + stem + ":" + Format(actualWidth) + "x" +
            Format(actualHeight) + "x" + Format(actualDepth));
    }

    private static void ShowDocument(ModelDoc2 document)
    {
        int activationError = 0;
        application.ActivateDoc3(document.GetTitle(), true, 0, ref activationError);
        document.ShowNamedView2(string.Empty, (int)swStandardViews_e.swIsometricView);
        document.ViewDisplayShaded();
        document.ViewZoomtofit2();
        document.GraphicsRedraw2();
    }

    private static void SavePreview(ModelDoc2 document)
    {
        string previewDirectory = Path.Combine(root, "artifacts", "preview");
        Directory.CreateDirectory(previewDirectory);
        string bitmapPath = Path.Combine(previewDirectory, "Rack4Modules_OpenCase.bmp");
        string previewPath = Path.Combine(previewDirectory, "Rack4Modules_OpenCase.png");

        if (!document.SaveBMP(bitmapPath, 1920, 1080))
        {
            Log("WARNING=SOLIDWORKS could not render the optional preview bitmap.");
            return;
        }

        using (Bitmap bitmap = new Bitmap(bitmapPath))
        {
            bitmap.Save(previewPath, ImageFormat.Png);
        }

        File.Delete(bitmapPath);
        Log("PREVIEW=" + previewPath);
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static void Log(string value)
    {
        Console.WriteLine("[RackFinal] " + value);
        Console.Out.Flush();
    }

    private sealed class InterferenceSummary
    {
        internal int Count;
        internal readonly List<string> Details = new List<string>();
    }
}
