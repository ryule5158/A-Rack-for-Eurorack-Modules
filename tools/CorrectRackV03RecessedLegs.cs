using System;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class CorrectRackV03RecessedLegs
{
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
            string expectedPath = Path.Combine(cad.AssembliesDirectory, "Rack4Modules_OpenCase_V03.SLDASM");
            if (!File.Exists(expectedPath))
            {
                throw new FileNotFoundException("V0.3 open assembly does not exist.", expectedPath);
            }

            int errors = 0;
            int warnings = 0;
            ModelDoc2 model = cad.Application.OpenDoc6(expectedPath, (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty, ref errors, ref warnings) as ModelDoc2;
            if (model == null || errors != 0 ||
                !string.Equals(Path.GetFullPath(model.GetPathName()), expectedPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Could not safely open the exact V0.3 project assembly; errors=" +
                    errors.ToString(CultureInfo.InvariantCulture));
            }

            AssemblyDoc assembly = model as AssemblyDoc;
            Array components = assembly == null ? null : assembly.GetComponents(false) as Array;
            if (components == null)
            {
                throw new InvalidOperationException("No V0.3 assembly components are available.");
            }

            MathUtility math = cad.Application.GetMathUtility() as MathUtility;
            if (math == null)
            {
                throw new InvalidOperationException("SOLIDWORKS did not expose its transform utility.");
            }

            int corrected = 0;
            foreach (object item in components)
            {
                Component2 component = item as Component2;
                if (component == null ||
                    !string.Equals(Path.GetFileName(component.GetPathName()),
                        "SideRecessedLeg_V03_TwoPosition.SLDPRT", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                MathTransform existing = component.Transform2;
                Array values = existing == null ? null : existing.ArrayData as Array;
                if (values == null || values.Length < 16)
                {
                    throw new InvalidOperationException("Missing transform for recessed folding leg.");
                }

                double[] transform = new double[16];
                for (int index = 0; index < transform.Length; index++)
                {
                    transform[index] = Convert.ToDouble(values.GetValue(index), CultureInfo.InvariantCulture);
                }

                double previousX = transform[9] * 1000.0;
                double correctedX = previousX < 0 ? -271.0 : 271.0;
                transform[9] = correctedX / 1000.0;
                MathTransform replacement = math.CreateTransform(transform) as MathTransform;
                if (replacement == null)
                {
                    throw new InvalidOperationException("Cannot create corrected recessed-leg transform.");
                }

                component.Transform2 = replacement;
                Array actual = component.Transform2.ArrayData as Array;
                if (actual == null || Math.Abs(Convert.ToDouble(actual.GetValue(9),
                    CultureInfo.InvariantCulture) * 1000.0 - correctedX) > 0.001)
                {
                    throw new InvalidOperationException("Recessed-leg transform readback did not match.");
                }

                Array bounds = component.GetBox(false, false) as Array;
                if (bounds != null && bounds.Length >= 6)
                {
                    double minimum = Convert.ToDouble(bounds.GetValue(0), CultureInfo.InvariantCulture) * 1000.0;
                    double maximum = Convert.ToDouble(bounds.GetValue(3), CultureInfo.InvariantCulture) * 1000.0;
                    if (minimum < -274.01 || maximum > 274.01)
                    {
                        throw new InvalidOperationException("Folding leg remains outside the 548 mm case width: " +
                            minimum.ToString("F3", CultureInfo.InvariantCulture) + ".." +
                            maximum.ToString("F3", CultureInfo.InvariantCulture));
                    }

                    cad.Log("RECESSED_LEG_BOUNDS_MM=" + minimum.ToString("F3", CultureInfo.InvariantCulture) +
                        ".." + maximum.ToString("F3", CultureInfo.InvariantCulture));
                }

                cad.Log("RECESSED_LEG_X_CORRECTED_MM=" + previousX.ToString("F1", CultureInfo.InvariantCulture) +
                    "->" + correctedX.ToString("F1", CultureInfo.InvariantCulture));
                corrected++;
            }

            if (corrected != 2)
            {
                throw new InvalidOperationException("Expected exactly two recessed folding legs; found " +
                    corrected.ToString(CultureInfo.InvariantCulture));
            }

            model.ForceRebuild3(false);
            cad.SaveAssembly(model, "Rack4Modules_OpenCase_V03", true);
            cad.Show(model);
            cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
            cad.Log("RECESSED_LEGS_FLUSH_WITH_548_MM_CASE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("RECESSED_LEG_CORRECTION_FAILED=" + exception);
            return 1;
        }
    }
}
