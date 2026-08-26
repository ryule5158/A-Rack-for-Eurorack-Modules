using System;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class ReviseRackOriginalInterfacesV04
{
    private const string OriginalEdge = "UpperEdge_V04_Adapter_MIDI_Handle_Audio";
    private const string OriginalAudio = "RearEdgeAudio_V03_8xTRS635";
    private const string OriginalDigital = "UpperMidiUsb_V04_3xDIN_USB_C";
    private const string OriginalPower = "RearEdgePowerBlank_V03";

    private const string RevisedEdge = "UpperEdge_V04_Adapter_MIDI_Handle_Audio_ClearPower";
    private const string RevisedAudio = "UpperAudio_V04_2x4_TRS635";
    private const string RevisedDigital = "UpperMidiUsb_V04_3xDIN_USB_C_Inline";
    private const string RevisedAdapter = "UpperAdapterBlank_V04_95mm";
    private const string CorrectedHandle = "RearCarryHandle_V03_ClearanceFit";

    private const string SheetMaterial = "5052-H32";
    private static readonly double[] Silver = { 0.67, 0.70, 0.73 };
    private static readonly double[] Graphite = { 0.12, 0.15, 0.18 };
    private static readonly double[] AudioBlue = { 0.12, 0.35, 0.55 };
    private static readonly double[] MidiPlum = { 0.38, 0.25, 0.54 };

    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Exactly one existing Rack4Modules project root is required.");
            }

            RackCadSession cad = new RackCadSession(Path.GetFullPath(arguments[0]));
            string openPath = Path.Combine(cad.AssembliesDirectory, "Rack4Modules_OpenCase_V03.SLDASM");
            if (!File.Exists(openPath))
            {
                throw new FileNotFoundException("The existing visible open-case assembly is required.", openPath);
            }

            string edge = BuildUpperEdge(cad);
            string audio = BuildAudioMatrix(cad);
            string digital = BuildDigitalCassette(cad);
            string adapter = BuildAdapterBlank(cad);

            foreach (string stem in new string[]
            {
                "Rack4Modules_OpenCase_V03",
                "Rack4Modules_TransportClosed_V03",
                "Rack4Modules_ClearanceCheck_V03"
            })
            {
                ReviseAssembly(cad, stem, edge, audio, digital, adapter);
            }

            ModelDoc2 visible = OpenExactAssembly(cad, openPath);
            cad.Show(visible);
            cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
            cad.Log("V04_ORIGINAL_UPPER_EDGE_LAYOUT=ADAPTER_95_MM|INLINE_3_DIN_MIDI_USB_C|SINGLE_CENTRAL_HANDLE|2_BY_4_AUDIO");
            cad.Log("V04_ADAPTER_CASSETTE_ENVELOPE_MM=95x80;undrilled_functional_panel;clear_support_window=75x60");
            cad.Log("V04_AUDIO_GROUPING=two_rows_of_four_6.35_mm_TRS");
        cad.Log("V04_UPPER_JOINER_SLOTS_REMOVED=true;adapter_plate_no_longer_obscures_a_side_slot");
            cad.Log("V04_ORIGINAL_INTERFACE_REVISION_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V04_ORIGINAL_INTERFACE_REVISION_FAILED=" + exception);
            return 1;
        }
    }

    private static string BuildUpperEdge(RackCadSession cad)
    {
        string existing = ExistingPart(cad, RevisedEdge);
        if (existing != null)
        {
            return existing;
        }

        double thickness = cad.N("enclosure", "body_thickness");
        double depth = cad.N("enclosure", "body_depth") - thickness;
        double width = cad.N("enclosure", "outer_width") -
            2.0 * cad.N("enclosure", "side_frame_thickness");
        cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
        ModelDoc2 document = cad.NewPart(RevisedEdge);
        Body2 body = cad.Box(0.0, 0.0, 0.0, width, thickness, depth);

        body = CutWindow(cad, body, -218.5, 55.0, 75.0, 60.0, thickness,
            "Generous undrilled adapter-inlet cassette clearance");
        body = CutWindow(cad, body, -116.0, 55.0, 80.0, 60.0, thickness,
            "Three MIDI DIN connectors and one USB-C controller cassette");
        body = CutWindow(cad, body, 165.0, 55.0, 166.0, 60.0, thickness,
            "Original two-by-four audio matrix cassette");

        body = AddCassetteMounts(cad, body, -218.5, 41.0, thickness,
            "95 mm adapter cassette M3 mounting");
        body = AddCassetteMounts(cad, body, -116.0, 43.0, thickness,
            "100 mm inline MIDI USB cassette M3 mounting");
        body = AddCassetteMounts(cad, body, 165.0, 86.0, thickness,
            "186 mm two-row audio cassette M3 mounting");

        foreach (double x in new double[] { -55.0, 55.0 })
        {
            body = HoleY(cad, body, x, 50.0, 5.2, thickness,
                "The only carry handle lower M5 mounting");
            body = HoleY(cad, body, x, 61.0, 5.2, thickness,
                "The only carry handle upper M5 mounting");
        }

        cad.AddBody(document, body,
            "Original upper-edge order: adapter reserve, MIDI USB, sole carry handle, two-row audio");
        cad.Property(document, "Upper-edge order",
            "95 mm undrilled adapter reserve | inline 3xDIN + USB-C | one handle | 2x4 audio");
        cad.Property(document, "Originality boundary",
            "Signals are intentionally mirrored and audio is a two-row matrix; not a direct Intellijel copy");
        cad.Property(document, "Power boundary",
            "95 x 80 mm removable blank; no supply topology, inlet type or electrical circuit selected");
        cad.Property(document, "Upper joiner slots",
            "Removed; the left legacy slot would be obscured by the 95 mm adapter plate");
        return SavePart(cad, document, RevisedEdge, Silver);
    }

    private static string BuildAudioMatrix(RackCadSession cad)
    {
        string existing = ExistingPart(cad, RevisedAudio);
        if (existing != null)
        {
            return existing;
        }

        double thickness = cad.N("enclosure", "body_thickness");
        ModelDoc2 document = cad.NewPart(RevisedAudio);
        Body2 body = cad.Box(0.0, 0.0, 0.0, 186.0, thickness, 80.0);
        foreach (double z in new double[] { 22.0, 58.0 })
        {
            foreach (double x in new double[] { -60.0, -20.0, 20.0, 60.0 })
            {
                body = HoleY(cad, body, x, z, 11.2, thickness,
                    "Original two-row 6.35 mm TRS aperture");
            }
        }

        body = AddPanelMounts(cad, body, 86.0, thickness);
        cad.AddBody(document, body,
            "Eight 6.35 mm audio apertures arranged in a distinctive two-by-four matrix");
        cad.Property(document, "Connector layout", "2 rows x 4 columns; 40 mm column pitch; 36 mm row pitch");
        cad.Property(document, "Electrical boundary",
            "Mechanical connector provisions only; no audio circuitry or I/O direction is defined");
        return SavePart(cad, document, RevisedAudio, AudioBlue);
    }

    private static string BuildDigitalCassette(RackCadSession cad)
    {
        string existing = ExistingPart(cad, RevisedDigital);
        if (existing != null)
        {
            return existing;
        }

        double thickness = cad.N("enclosure", "body_thickness");
        ModelDoc2 document = cad.NewPart(RevisedDigital);
        Body2 body = cad.Box(0.0, 0.0, 0.0, 100.0, thickness, 80.0);
        foreach (double x in new double[] { -34.0, -10.0, 14.0 })
        {
            body = HoleY(cad, body, x, 40.0, 15.0, thickness,
                "DIN-5 MIDI IN OUT or THRU in one horizontal row");
            body = HoleY(cad, body, x, 28.9, 3.2, thickness,
                "DIN-5 lower vertical mounting ear");
            body = HoleY(cad, body, x, 51.1, 3.2, thickness,
                "DIN-5 upper vertical mounting ear");
        }

        body = cad.Cut(body, cad.Box(39.0, 0.0, 37.0,
            12.0, thickness + 0.8, 6.0), "Inline right-side USB-C opening");
        body = HoleY(cad, body, 39.0, 30.0, 2.4, thickness, "USB-C lower vertical carrier fixing");
        body = HoleY(cad, body, 39.0, 50.0, 2.4, thickness, "USB-C upper vertical carrier fixing");
        body = AddPanelMounts(cad, body, 43.0, thickness);

        cad.AddBody(document, body,
            "Clear one-row MIDI USB cassette directly beside the independent 95 mm adapter reserve");
        cad.Property(document, "Adjacent adapter reserve", "95 x 80 mm independent undrilled power cassette");
        cad.Property(document, "Connector layout",
            "One row at local z40: DIN x=-34,-10,+14 mm; USB-C x=+39 mm; all fixing pairs vertical");
        cad.Property(document, "Electrical boundary",
            "Three DIN-5 and one USB-C are mechanical openings; no digital electronics are included");
        return SavePart(cad, document, RevisedDigital, MidiPlum);
    }

    private static string BuildAdapterBlank(RackCadSession cad)
    {
        string existing = ExistingPart(cad, RevisedAdapter);
        if (existing != null)
        {
            return existing;
        }

        double thickness = cad.N("enclosure", "body_thickness");
        ModelDoc2 document = cad.NewPart(RevisedAdapter);
        Body2 body = cad.Box(0.0, 0.0, 0.0, 95.0, thickness, 80.0);
        body = AddPanelMounts(cad, body, 41.0, thickness);
        cad.AddBody(document, body,
            "Generous undrilled adapter-input cassette beside MIDI; four M3 cassette mounts only");
        cad.Property(document, "Panel envelope", "95 x 80 x 2 mm");
        cad.Property(document, "Clear support opening", "75 x 60 mm");
        cad.Property(document, "Future adapter boundary",
            "Reserve for one selected external DC adapter inlet and cable clearance; no functional opening yet");
        cad.Property(document, "Electrical boundary", "No AC inlet, DC voltage, connector or power supply topology selected");
        return SavePart(cad, document, RevisedAdapter, Graphite);
    }

    private static Body2 CutWindow(RackCadSession cad, Body2 body,
        double x, double centerZ, double width, double height, double thickness, string label)
    {
        return cad.Cut(body, cad.Box(x, 0.0, centerZ - height * 0.5,
            width, thickness + 0.8, height), label);
    }

    private static Body2 AddCassetteMounts(RackCadSession cad, Body2 body,
        double centerX, double halfPitch, double thickness, string label)
    {
        foreach (double side in new double[] { -1.0, 1.0 })
        {
            foreach (double z in new double[] { 20.0, 90.0 })
            {
                body = HoleY(cad, body, centerX + side * halfPitch,
                    z, 3.2, thickness, label);
            }
        }

        return body;
    }

    private static Body2 AddPanelMounts(RackCadSession cad, Body2 body,
        double halfPitch, double thickness)
    {
        foreach (double side in new double[] { -1.0, 1.0 })
        {
            foreach (double z in new double[] { 5.0, 75.0 })
            {
                body = HoleY(cad, body, side * halfPitch,
                    z, 3.2, thickness, "Removable cassette M3 mounting");
            }
        }

        return body;
    }

    private static Body2 HoleY(RackCadSession cad, Body2 body,
        double x, double z, double diameter, double thickness, string label)
    {
        return cad.Cut(body,
            cad.Cylinder(x, -thickness * 0.5 - 0.4, z,
                0.0, 1.0, 0.0, diameter, thickness + 0.8), label);
    }

    private static string ExistingPart(RackCadSession cad, string stem)
    {
        string path = Path.Combine(cad.PartsDirectory, stem + ".SLDPRT");
        if (!File.Exists(path))
        {
            return null;
        }

        cad.Log("REUSING_V04_UPPER_EDGE_PART=" + path);
        return path;
    }

    private static string SavePart(RackCadSession cad, ModelDoc2 document,
        string stem, double[] appearance)
    {
        cad.ApplyMaterial(document, SheetMaterial, appearance);
        string saved = cad.SavePart(document, stem, true);
        cad.Application.CloseDoc(document.GetTitle());
        return saved;
    }

    private static void ReviseAssembly(RackCadSession cad, string stem,
        string edgePath, string audioPath, string digitalPath, string adapterPath)
    {
        string path = Path.Combine(cad.AssembliesDirectory, stem + ".SLDASM");
        ModelDoc2 document = OpenExactAssembly(cad, path);
        AssemblyDoc assembly = document as AssemblyDoc;
        if (assembly == null)
        {
            throw new InvalidOperationException("The selected native document is not an assembly: " + path);
        }

        ReplaceOne(cad, document, assembly, OriginalEdge, RevisedEdge, edgePath,
            0.0, 209.0, 0.0, "Upper edge: adapter MIDI handle and original audio matrix");
        ReplaceOne(cad, document, assembly, OriginalAudio, RevisedAudio, audioPath,
            165.0, 211.0, 15.0, "Right upper-edge two-row audio cassette");
        ReplaceOne(cad, document, assembly, OriginalDigital, RevisedDigital, digitalPath,
            -116.0, 211.0, 15.0, "Left upper-edge inline MIDI and USB controller cassette");
        ReplaceOne(cad, document, assembly, OriginalPower, RevisedAdapter, adapterPath,
            -218.5, 211.0, 15.0, "Left upper-edge 95 mm external-adapter reserve");

        document.ForceRebuild3(false);
        ValidateCassettePositions(cad, assembly, stem);
        cad.Property(document, "Original upper-edge order",
            "95 mm adapter reserve | inline 3xDIN MIDI + USB-C | sole central carry handle | original 2x4 audio matrix");
        cad.Property(document, "Power adapter reserve",
            "Independent undrilled 95 x 80 mm panel next to MIDI; 75 x 60 mm support opening");
        cad.Property(document, "Audio visual layout", "Eight 6.35 mm TRS apertures in two rows of four");
        cad.Property(document, "Carry handles", "Exactly one upper-edge central handle; no side carry handles");
        cad.SaveAssembly(document, stem, true);
        cad.Log("REVISED_V04_UPPER_EDGE_ASSEMBLY=" + stem);
    }

    private static ModelDoc2 OpenExactAssembly(RackCadSession cad, string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The exact existing project assembly is missing.", path);
        }

        int errors = 0;
        int warnings = 0;
        ModelDoc2 document = cad.Application.OpenDoc6(path,
            (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
            string.Empty, ref errors, ref warnings) as ModelDoc2;
        if (document == null || errors != 0 ||
            !string.Equals(Path.GetFullPath(document.GetPathName()),
                Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Could not open exact assembly " + path +
                "; errors=" + errors.ToString(CultureInfo.InvariantCulture));
        }

        return document;
    }

    private static void ReplaceOne(RackCadSession cad, ModelDoc2 document,
        AssemblyDoc assembly, string oldStem, string newStem, string newPath,
        double x, double y, double z, string label)
    {
        Component2 oldComponent = null;
        Component2 revisedComponent = null;
        foreach (object item in Components(assembly))
        {
            Component2 component = item as Component2;
            if (component == null)
            {
                continue;
            }

            string componentStem = Path.GetFileNameWithoutExtension(component.GetPathName());
            if (string.Equals(componentStem, oldStem, StringComparison.OrdinalIgnoreCase))
            {
                if (oldComponent != null)
                {
                    throw new InvalidOperationException("More than one replaceable component exists: " + oldStem);
                }

                oldComponent = component;
            }
            else if (string.Equals(componentStem, newStem, StringComparison.OrdinalIgnoreCase))
            {
                if (revisedComponent != null)
                {
                    throw new InvalidOperationException("More than one revised component exists: " + newStem);
                }

                revisedComponent = component;
            }
        }

        if (oldComponent != null && revisedComponent != null)
        {
            throw new InvalidOperationException("Old and revised cassette coexist: " + oldStem);
        }

        if (oldComponent != null)
        {
            document.ClearSelection2(true);
            if (!oldComponent.Select4(false, null, false) ||
                !assembly.ReplaceComponents(newPath, string.Empty, false, true))
            {
                throw new InvalidOperationException("SOLIDWORKS refused the component replacement " + oldStem);
            }

            document.ClearSelection2(true);
            revisedComponent = FindUnique(assembly, newStem);
        }

        if (revisedComponent == null)
        {
            throw new InvalidOperationException("The revised component is missing: " + newStem);
        }

        SetTranslation(cad, revisedComponent, x, y, z);
        try
        {
            revisedComponent.Name2 = label;
        }
        catch (Exception exception)
        {
            cad.Log("WARNING: Revised component label unavailable: " + exception.Message);
        }
    }

    private static void SetTranslation(RackCadSession cad, Component2 component,
        double x, double y, double z)
    {
        MathUtility math = cad.Application.GetMathUtility() as MathUtility;
        if (math == null)
        {
            throw new InvalidOperationException("No SOLIDWORKS transformation utility is available.");
        }

        double[] values = new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0,
            x / 1000.0, y / 1000.0, z / 1000.0,
            1.0, 0.0, 0.0, 0.0
        };
        MathTransform transform = math.CreateTransform(values) as MathTransform;
        if (transform == null)
        {
            throw new InvalidOperationException("Cannot create an upper-edge cassette transformation.");
        }

        component.Transform2 = transform;
    }

    private static void ValidateCassettePositions(RackCadSession cad,
        AssemblyDoc assembly, string stage)
    {
        Component2 audio = FindUnique(assembly, RevisedAudio);
        Component2 midi = FindUnique(assembly, RevisedDigital);
        Component2 adapter = FindUnique(assembly, RevisedAdapter);
        Component2 handle = FindUnique(assembly, CorrectedHandle);
        if (audio == null || midi == null || adapter == null || handle == null)
        {
            throw new InvalidOperationException("All original-layout cassettes and the one carry handle are required.");
        }

        Array audioBox = audio.GetBox(false, false) as Array;
        Array midiBox = midi.GetBox(false, false) as Array;
        Array adapterBox = adapter.GetBox(false, false) as Array;
        Array handleBox = handle.GetBox(false, false) as Array;
        double leftHandleGap = (Coordinate(handleBox, 0) - Coordinate(midiBox, 3)) * 1000.0;
        double rightHandleGap = (Coordinate(audioBox, 0) - Coordinate(handleBox, 3)) * 1000.0;
        double adapterWidth = (Coordinate(adapterBox, 3) - Coordinate(adapterBox, 0)) * 1000.0;
        double adapterToMidi = Math.Abs(Coordinate(adapterBox, 3) - Coordinate(midiBox, 0)) * 1000.0;
        if (leftHandleGap < 2.95 || rightHandleGap < 8.95 ||
            Math.Abs(adapterWidth - 95.0) > 0.05 || Math.Abs(adapterToMidi - 5.0) > 0.05)
        {
            throw new InvalidOperationException("Revised upper-edge fit failed: handle gaps " +
                leftHandleGap.ToString("F2", CultureInfo.InvariantCulture) + "/" +
                rightHandleGap.ToString("F2", CultureInfo.InvariantCulture) +
                " mm; adapter width " + adapterWidth.ToString("F2", CultureInfo.InvariantCulture) +
                " mm; adapter-to-MIDI separation " + adapterToMidi.ToString("F2", CultureInfo.InvariantCulture));
        }

        int handles = 0;
        foreach (object item in Components(assembly))
        {
            Component2 component = item as Component2;
            string stem = component == null ? string.Empty :
                Path.GetFileNameWithoutExtension(component.GetPathName());
            if (stem.StartsWith("RearCarryHandle_", StringComparison.OrdinalIgnoreCase))
            {
                handles++;
            }
        }

        if (handles != 1)
        {
            throw new InvalidOperationException("Exactly one upper-edge carry handle is required; actual " + handles);
        }

        cad.Log(stage + "_SOLE_HANDLE_GAPS_MM=" +
            leftHandleGap.ToString("F2", CultureInfo.InvariantCulture) + "," +
            rightHandleGap.ToString("F2", CultureInfo.InvariantCulture));
        cad.Log(stage + "_ADAPTER_PANEL_WIDTH_MM=" +
            adapterWidth.ToString("F2", CultureInfo.InvariantCulture));
    }

    private static Component2 FindUnique(AssemblyDoc assembly, string stem)
    {
        Component2 found = null;
        foreach (object item in Components(assembly))
        {
            Component2 component = item as Component2;
            if (component == null ||
                !string.Equals(Path.GetFileNameWithoutExtension(component.GetPathName()),
                    stem, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (found != null)
            {
                throw new InvalidOperationException("More than one component exists: " + stem);
            }

            found = component;
        }

        return found;
    }

    private static Array Components(AssemblyDoc assembly)
    {
        Array result = assembly.GetComponents(false) as Array;
        if (result == null)
        {
            throw new InvalidOperationException("SOLIDWORKS did not expose project assembly components.");
        }

        return result;
    }

    private static double Coordinate(Array values, int index)
    {
        if (values == null || values.Length <= index)
        {
            throw new InvalidOperationException("SOLIDWORKS did not expose the required component envelope.");
        }

        return Convert.ToDouble(values.GetValue(index), CultureInfo.InvariantCulture);
    }
}
