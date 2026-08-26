using System;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class CorrectRackV03MechanicalClearance
{
    private const string CorrectedHandleStem = "RearCarryHandle_V03_ClearanceFit";
    private static readonly double[] DarkHardware = { 0.06, 0.07, 0.08 };

    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Exactly one Rack4Modules project root is required.");
            }

            RackCadSession cad = new RackCadSession(Path.GetFullPath(arguments[0]));
            string assemblyPath = Path.Combine(cad.AssembliesDirectory, "Rack4Modules_OpenCase_V03.SLDASM");
            ModelDoc2 assemblyDocument = OpenAssembly(cad, assemblyPath);
            PreserveProjectAssembly(assemblyDocument, assemblyPath);

            string handlePath = BuildClearanceFitHandle(cad);
            assemblyDocument = OpenAssembly(cad, assemblyPath);
            AssemblyDoc assembly = assemblyDocument as AssemblyDoc;
            if (assembly == null)
            {
                throw new InvalidOperationException("The V0.3 open-case document is not an assembly.");
            }

            ReplaceReferenceHandle(cad, assemblyDocument, assembly, handlePath);
            CorrectSideHardware(cad, assembly);
            assemblyDocument.ForceRebuild3(false);
            ValidatePhysicalClearances(cad, assembly);

            cad.Property(assemblyDocument, "Fabrication fit revision", "0.3.1: 126 mm handle; flush legs and internal lid catches");
            cad.Property(assemblyDocument, "Module depth boundary", "85 mm normal; 73 mm bus zone; 60 mm central PSU zone");
            cad.Property(assemblyDocument, "Side hardware envelope", "Legs and lid catches remain within 548 mm body width");
            cad.SaveAssembly(assemblyDocument, "Rack4Modules_OpenCase_V03", true);
            cad.Show(assemblyDocument);
            cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
            WriteReport(cad);
            cad.Log("V03_MECHANICAL_CLEARANCE_CORRECTION_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V03_MECHANICAL_CLEARANCE_CORRECTION_FAILED=" + exception);
            return 1;
        }
    }

    private static ModelDoc2 OpenAssembly(RackCadSession cad, string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException("The exact V0.3 open assembly does not exist.", assemblyPath);
        }

        int errors = 0;
        int warnings = 0;
        ModelDoc2 document = cad.Application.OpenDoc6(assemblyPath, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty, ref errors, ref warnings) as ModelDoc2;
        if (document == null || errors != 0 ||
            !string.Equals(Path.GetFullPath(document.GetPathName()), assemblyPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot open the exact V0.3 project assembly; errors=" +
                errors.ToString(CultureInfo.InvariantCulture));
        }

        return document;
    }

    private static void PreserveProjectAssembly(ModelDoc2 document, string expectedPath)
    {
        if (!document.GetSaveFlag())
        {
            return;
        }

        int errors = 0;
        int warnings = 0;
        if (!document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errors, ref warnings) || errors != 0)
        {
            throw new InvalidOperationException("Cannot preserve existing V0.3 assembly changes: " + expectedPath);
        }
    }

    private static string BuildClearanceFitHandle(RackCadSession cad)
    {
        string path = Path.Combine(cad.PartsDirectory, CorrectedHandleStem + ".SLDPRT");
        if (File.Exists(path))
        {
            cad.Log("REUSING_CORRECTED_HANDLE=" + path);
            return path;
        }

        cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
        ModelDoc2 handle = cad.NewPart(CorrectedHandleStem);
        cad.AddBody(handle, cad.Box(-55, 0, 0, 16, 10, 14), "Left handle mount - 110 mm pitch");
        cad.AddBody(handle, cad.Box(55, 0, 0, 16, 10, 14), "Right handle mount - 110 mm pitch");
        cad.AddBody(handle, cad.Box(0, 0, 6, 126, 8, 9), "126 mm grip with 1 mm clearance to each interface cassette");
        cad.ApplyMaterial(handle, "6061-T6 (SS)", DarkHardware);
        cad.Property(handle, "Reference layout", "Central rear-edge handle between audio and MIDI USB panels");
        cad.Property(handle, "Mounting pitch", "110 mm");
        cad.Property(handle, "Grip width", "126 mm");
        cad.Property(handle, "Side clearance", "1 mm minimum to audio and MIDI USB faceplates");
        cad.Property(handle, "Hardware boundary", "Envelope only; supplier drawing and load rating still required");
        string savedPath = cad.SavePart(handle, CorrectedHandleStem, true);
        cad.Application.CloseDoc(handle.GetTitle());
        cad.Log("CORRECTED_HANDLE_CREATED=" + savedPath);
        return savedPath;
    }

    private static void ReplaceReferenceHandle(RackCadSession cad, ModelDoc2 document,
        AssemblyDoc assembly, string correctedPath)
    {
        Component2 original = null;
        int correctedCount = 0;
        foreach (object item in Components(assembly))
        {
            Component2 component = item as Component2;
            if (component == null)
            {
                continue;
            }

            string filename = Path.GetFileName(component.GetPathName());
            if (string.Equals(filename, "RearCarryHandle_V03_Reference.SLDPRT", StringComparison.OrdinalIgnoreCase))
            {
                if (original != null)
                {
                    throw new InvalidOperationException("More than one original carry-handle component was found.");
                }

                original = component;
            }
            else if (string.Equals(filename, CorrectedHandleStem + ".SLDPRT", StringComparison.OrdinalIgnoreCase))
            {
                correctedCount++;
            }
        }

        if (original == null && correctedCount == 1)
        {
            cad.Log("CORRECTED_HANDLE_ALREADY_PRESENT=true");
            return;
        }

        if (original == null || correctedCount != 0)
        {
            throw new InvalidOperationException("The assembly does not contain exactly one replaceable reference handle.");
        }

        document.ClearSelection2(true);
        if (!original.Select4(false, null, false))
        {
            throw new InvalidOperationException("Could not select the original reference carry handle.");
        }

        if (!assembly.ReplaceComponents(correctedPath, string.Empty, false, true))
        {
            throw new InvalidOperationException("SOLIDWORKS refused the clearance-fit carry handle replacement.");
        }

        document.ClearSelection2(true);
        cad.Log("REPLACED_132_MM_GRIP_WITH_126_MM_GRIP=true");
    }

    private static void CorrectSideHardware(RackCadSession cad, AssemblyDoc assembly)
    {
        int legs = 0;
        int catches = 0;
        MathUtility math = cad.Application.GetMathUtility() as MathUtility;
        if (math == null)
        {
            throw new InvalidOperationException("No SOLIDWORKS transform utility is available.");
        }

        foreach (object item in Components(assembly))
        {
            Component2 component = item as Component2;
            if (component == null)
            {
                continue;
            }

            string filename = Path.GetFileName(component.GetPathName());
            if (string.Equals(filename, "SideRecessedLeg_V03_TwoPosition.SLDPRT", StringComparison.OrdinalIgnoreCase))
            {
                Array existing = component.Transform2.ArrayData as Array;
                double currentX = Convert.ToDouble(existing.GetValue(9), CultureInfo.InvariantCulture) * 1000.0;
                SetTranslation(component, math, currentX < 0 ? -271.0 : 271.0, -56.0,
                    Convert.ToDouble(existing.GetValue(11), CultureInfo.InvariantCulture) * 1000.0);
                legs++;
                cad.Log("RECESSED_LEG_POSITION_MM=" + (currentX < 0 ? "-271" : "271") + ",-56");
            }
            else if (string.Equals(filename, "InternalLidCatch_V03.SLDPRT", StringComparison.OrdinalIgnoreCase))
            {
                Array existing = component.Transform2.ArrayData as Array;
                double currentX = Convert.ToDouble(existing.GetValue(9), CultureInfo.InvariantCulture) * 1000.0;
                double y = Convert.ToDouble(existing.GetValue(10), CultureInfo.InvariantCulture) * 1000.0;
                double z = Convert.ToDouble(existing.GetValue(11), CultureInfo.InvariantCulture) * 1000.0;
                SetTranslation(component, math, currentX < 0 ? -272.0 : 272.0, y, z);
                catches++;
                cad.Log("INTERNAL_CATCH_POSITION_MM=" + (currentX < 0 ? "-272" : "272") + "," +
                    y.ToString("F0", CultureInfo.InvariantCulture));
            }
        }

        if (legs != 2 || catches != 4)
        {
            throw new InvalidOperationException("Expected two legs and four internal catches; actual " +
                legs.ToString(CultureInfo.InvariantCulture) + " and " + catches.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void SetTranslation(Component2 component, MathUtility math, double x, double y, double z)
    {
        Array current = component.Transform2.ArrayData as Array;
        if (current == null || current.Length < 16)
        {
            throw new InvalidOperationException("Component positioning transform is unavailable.");
        }

        double[] values = new double[16];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = Convert.ToDouble(current.GetValue(index), CultureInfo.InvariantCulture);
        }

        values[9] = x / 1000.0;
        values[10] = y / 1000.0;
        values[11] = z / 1000.0;
        MathTransform transform = math.CreateTransform(values) as MathTransform;
        if (transform == null)
        {
            throw new InvalidOperationException("Cannot create the corrected component transform.");
        }

        component.Transform2 = transform;
    }

    private static void ValidatePhysicalClearances(RackCadSession cad, AssemblyDoc assembly)
    {
        Component2 handle = null;
        Component2 audio = null;
        Component2 midi = null;

        foreach (object item in Components(assembly))
        {
            Component2 component = item as Component2;
            if (component == null)
            {
                continue;
            }

            string filename = Path.GetFileName(component.GetPathName());
            if (string.Equals(filename, CorrectedHandleStem + ".SLDPRT", StringComparison.OrdinalIgnoreCase))
            {
                handle = component;
            }
            else if (string.Equals(filename, "RearEdgeAudio_V03_8xTRS635.SLDPRT", StringComparison.OrdinalIgnoreCase))
            {
                audio = component;
            }
            else if (string.Equals(filename, "RearEdgeMidiUsb_V03_3xDIN_USB_C.SLDPRT", StringComparison.OrdinalIgnoreCase))
            {
                midi = component;
            }

            if (string.Equals(filename, "SideRecessedLeg_V03_TwoPosition.SLDPRT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(filename, "InternalLidCatch_V03.SLDPRT", StringComparison.OrdinalIgnoreCase))
            {
                Array box = component.GetBox(false, false) as Array;
                ValidateSideBounds(box, filename);
            }
        }

        if (handle == null || audio == null || midi == null)
        {
            throw new InvalidOperationException("The corrected rear-edge carry handle and both interface plates are required.");
        }

        Array handleBox = handle.GetBox(false, false) as Array;
        Array audioBox = audio.GetBox(false, false) as Array;
        Array midiBox = midi.GetBox(false, false) as Array;
        double leftGap = (Coordinate(handleBox, 0) - Coordinate(audioBox, 3)) * 1000.0;
        double rightGap = (Coordinate(midiBox, 0) - Coordinate(handleBox, 3)) * 1000.0;
        if (leftGap < 0.95 || rightGap < 0.95)
        {
            throw new InvalidOperationException("Carry handle side clearance is insufficient; left=" +
                leftGap.ToString("F3", CultureInfo.InvariantCulture) + ", right=" +
                rightGap.ToString("F3", CultureInfo.InvariantCulture));
        }

        cad.Log("HANDLE_AUDIO_CLEARANCE_MM=" + leftGap.ToString("F3", CultureInfo.InvariantCulture));
        cad.Log("HANDLE_MIDI_CLEARANCE_MM=" + rightGap.ToString("F3", CultureInfo.InvariantCulture));
    }

    private static void ValidateSideBounds(Array box, string name)
    {
        double minimum = Coordinate(box, 0) * 1000.0;
        double maximum = Coordinate(box, 3) * 1000.0;
        if (minimum < -274.01 || maximum > 274.01)
        {
            throw new InvalidOperationException(name + " extends beyond the 548 mm case envelope: " +
                minimum.ToString("F3", CultureInfo.InvariantCulture) + ".." +
                maximum.ToString("F3", CultureInfo.InvariantCulture));
        }
    }

    private static double Coordinate(Array values, int index)
    {
        if (values == null || values.Length <= index)
        {
            throw new InvalidOperationException("SOLIDWORKS did not provide the required component bounding box.");
        }

        return Convert.ToDouble(values.GetValue(index), CultureInfo.InvariantCulture);
    }

    private static Array Components(AssemblyDoc assembly)
    {
        Array components = assembly.GetComponents(false) as Array;
        if (components == null)
        {
            throw new InvalidOperationException("The V0.3 assembly does not expose its physical components.");
        }

        return components;
    }

    private static void WriteReport(RackCadSession cad)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("# Rack4Modules V0.3.1 mechanical-clearance corrections");
        report.AppendLine();
        report.AppendLine("- Case width: 548 mm, physical side envelope x = -274..+274 mm.");
        report.AppendLine("- Recessed side-leg centres: x = -271/+271 mm; y = -56 mm.");
        report.AppendLine("- Internal cover-catch centres: x = -272/+272 mm; four catches total.");
        report.AppendLine("- Central handle: 110 mm mounting pitch, 126 mm grip, 1 mm clear to each I/O cassette.");
        report.AppendLine("- Standard module depth: 85 mm only outside occupied power-reservation zones.");
        report.AppendLine("- Distributed bus region: 73 mm module clearance; central supply region: 60 mm.");
        report.AppendLine("- No PSU topology, inlet, electrical circuitry, load capacity or EMC approval is claimed.");
        File.WriteAllText(Path.Combine(cad.ReportsDirectory, "layout-v03-clearances.md"),
            report.ToString(), new UTF8Encoding(false));
    }
}
