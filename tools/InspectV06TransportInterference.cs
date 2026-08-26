using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Read-only diagnostic for the exact V0.6 transport assembly. It reports the
// folded-leg / V0.6 relieved-lid pair and all interference pairs; it does not
// save or mutate CAD.
internal static class InspectV06TransportInterference
{
    private const double Mm = 1000.0;

    [STAThread]
    private static int Main(string[] args)
    {
        SldWorks app = null;
        ModelDoc2 doc = null;
        bool opened = false;
        try
        {
            if (args.Length != 1 || !Directory.Exists(args[0]))
                throw new ArgumentException("Pass one existing Rack4Modules root.");
            string path = Path.GetFullPath(Path.Combine(args[0], "cad", "assemblies",
                "Rack4Modules_TransportClosed_V06.SLDASM"));
            app = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            doc = FindExact(app, path);
            if (doc == null)
            {
                int e = 0, w = 0;
                doc = app.OpenDoc6(path, (int)swDocumentTypes_e.swDocASSEMBLY,
                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent |
                    (int)swOpenDocOptions_e.swOpenDocOptions_ReadOnly,
                    string.Empty, ref e, ref w) as ModelDoc2;
                if (doc == null || e != 0) throw new InvalidOperationException("open errors=" + e + ", warnings=" + w);
                opened = true;
            }
            AssemblyDoc asm = doc as AssemblyDoc;
            if (asm == null) throw new InvalidOperationException("Not an assembly.");
            object raw = asm.GetComponents(true);
            Array components = raw as Array;
            if (components == null) throw new InvalidOperationException("No components.");

            Component2 leg = null, lid = null;
            foreach (object value in components)
            {
                Component2 c = value as Component2;
                if (c == null) continue;
                string stem = Path.GetFileNameWithoutExtension(c.GetPathName());
                if (stem.Equals("SideKickstand_V06_170mm_6mm", StringComparison.OrdinalIgnoreCase) && leg == null)
                    leg = c;
                if (stem.Equals("DeepTravelLid_V06_StandRelief", StringComparison.OrdinalIgnoreCase))
                    lid = c;
            }
            DumpComponent("LEG", leg);
            DumpComponent("LID", lid);

            InterferenceDetectionMgr mgr = asm.InterferenceDetectionManager;
            if (mgr != null)
            {
                mgr.TreatCoincidenceAsInterference = false;
                mgr.IncludeMultibodyPartInterferences = false;
                mgr.MakeInterferingPartsTransparent = false;
                mgr.IgnoreHiddenBodies = false;
                Array rawInterferences = mgr.GetInterferences() as Array;
                Console.WriteLine("API_INTERFERENCE_COUNT=" + mgr.GetInterferenceCount());
                if (rawInterferences != null)
                {
                    foreach (object value in rawInterferences)
                    {
                        Interference item = value as Interference;
                        if (item == null) continue;
                        Array participants = item.Components as Array;
                        List<string> names = new List<string>();
                        if (participants != null)
                        {
                            foreach (object p in participants)
                            {
                                Component2 c = p as Component2;
                                if (c != null) names.Add(Path.GetFileNameWithoutExtension(c.GetPathName()));
                            }
                        }
                        if (names.Exists(n => n.IndexOf("SideKickstand_V06", StringComparison.OrdinalIgnoreCase) >= 0) &&
                            names.Exists(n => n.IndexOf("DeepTravelLid_V06_StandRelief", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            Console.WriteLine("LEG_LID_INTERFERENCE_VOLUME_MM3=" +
                                (item.Volume * 1.0e9).ToString("0.###", CultureInfo.InvariantCulture));
                            Console.WriteLine("LEG_LID_PARTICIPANTS=" + string.Join(" <-> ", names.ToArray()));
                        }
                    }
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("TRANSPORT_INSPECT_FAILED=" + ex);
            return 1;
        }
        finally
        {
            if (opened && app != null && doc != null)
            {
                try { app.CloseDoc(doc.GetTitle()); } catch { }
            }
        }
    }

    private static void DumpComponent(string label, Component2 c)
    {
        if (c == null) { Console.WriteLine(label + "_MISSING=true"); return; }
        double[] box = c.GetBox(false, false) as double[];
        Console.WriteLine(label + "_PATH=" + c.GetPathName());
        Console.WriteLine(label + "_NAME=" + c.Name2);
        if (box != null && box.Length >= 6)
            Console.WriteLine(label + "_BOX_MM=" + F(box[0]) + "," + F(box[1]) + "," + F(box[2]) + "," + F(box[3]) + "," + F(box[4]) + "," + F(box[5]));
        double[] t = c.Transform2 == null ? null : c.Transform2.ArrayData as double[];
        if (t != null && t.Length >= 16)
            Console.WriteLine(label + "_TRANSFORM=" + string.Join(",", t));
    }

    private static string F(double metres) { return (metres * Mm).ToString("0.###", CultureInfo.InvariantCulture); }

    private static ModelDoc2 FindExact(SldWorks app, string path)
    {
        ModelDoc2 current = app.GetFirstDocument() as ModelDoc2;
        while (current != null)
        {
            string p = current.GetPathName();
            if (!string.IsNullOrWhiteSpace(p) && string.Equals(Path.GetFullPath(p), path, StringComparison.OrdinalIgnoreCase)) return current;
            current = current.GetNext() as ModelDoc2;
        }
        return null;
    }
}
