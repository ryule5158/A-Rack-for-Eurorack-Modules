using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal sealed class RackCadSession
{
    private const string PartTemplate = @"C:\ProgramData\SolidWorks\SOLIDWORKS 2025\templates\gb_part.prtdot";
    private const string AssemblyTemplate = @"C:\ProgramData\SolidWorks\SOLIDWORKS 2025\templates\gb_assembly.asmdot";
    private const string MaterialDatabase = @"E:\SW2025\SOLIDWORKS\lang\english\sldmaterials\solidworks materials.sldmat";
    private const double Millimetre = 0.001;
    private const double TransformTolerance = 0.00000001;

    private readonly IDictionary<string, object> parameters;

    public SldWorks Application;
    public Modeler Modeler;
    public string Root;
    public string PartsDirectory;
    public string AssembliesDirectory;
    public string ExportsDirectory;
    public string ReportsDirectory;

    public RackCadSession(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("An existing project root is required.", "root");
        }

        Root = Path.GetFullPath(root);
        if (!Directory.Exists(Root))
        {
            throw new DirectoryNotFoundException("The existing project root was not found: " + Root);
        }

        PartsDirectory = Path.Combine(Root, "cad", "parts");
        AssembliesDirectory = Path.Combine(Root, "cad", "assemblies");
        ExportsDirectory = Path.Combine(Root, "exports");
        ReportsDirectory = Path.Combine(Root, "reports");

        string parameterPath = Path.Combine(Root, "design", "parameters.json");
        if (!File.Exists(parameterPath))
        {
            throw new FileNotFoundException("The rack design parameter file was not found.", parameterPath);
        }

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;
        parameters = serializer.DeserializeObject(File.ReadAllText(parameterPath)) as IDictionary<string, object>;
        if (parameters == null)
        {
            throw new InvalidDataException("The rack parameter file must contain a JSON object: " + parameterPath);
        }

        Directory.CreateDirectory(PartsDirectory);
        Directory.CreateDirectory(AssembliesDirectory);
        Directory.CreateDirectory(ExportsDirectory);
        Directory.CreateDirectory(ReportsDirectory);

        Application = AttachOrStartSolidWorks();
        Application.Visible = true;
        Application.UserControl = true;

        Modeler = Application.GetModeler() as Modeler;
        if (Modeler == null)
        {
            throw new InvalidOperationException("SOLIDWORKS did not provide its geometric modeler.");
        }

        Log("Connected to SOLIDWORKS " + Application.RevisionNumber() + "; project " + Root);
    }

    public double N(string group, string key)
    {
        if (string.IsNullOrWhiteSpace(group) || string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Both the parameter group and parameter name are required.");
        }

        object groupValue;
        if (!parameters.TryGetValue(group, out groupValue))
        {
            throw new KeyNotFoundException("The design parameter group does not exist: " + group);
        }

        IDictionary<string, object> values = groupValue as IDictionary<string, object>;
        if (values == null)
        {
            throw new InvalidDataException("The design parameter group is not a JSON object: " + group);
        }

        object value;
        if (!values.TryGetValue(key, out value) || value == null)
        {
            throw new KeyNotFoundException("The design parameter does not exist: " + group + "." + key);
        }

        try
        {
            double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            EnsureFinite(number, group + "." + key);
            return number;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("The design parameter is not a finite number: " + group + "." + key, exception);
        }
    }

    public ModelDoc2 NewPart(string title)
    {
        return NewDocument(PartTemplate, title, "part");
    }

    public ModelDoc2 NewAssembly(string title)
    {
        return NewDocument(AssemblyTemplate, title, "assembly");
    }

    public Body2 Box(double x, double y, double z, double width, double height, double depth)
    {
        EnsureFinite(x, "box x");
        EnsureFinite(y, "box y");
        EnsureFinite(z, "box z");
        EnsurePositive(width, "box width");
        EnsurePositive(height, "box height");
        EnsurePositive(depth, "box depth");

        double[] definition = new double[]
        {
            x * Millimetre,
            y * Millimetre,
            z * Millimetre,
            0.0,
            0.0,
            1.0,
            width * Millimetre,
            height * Millimetre,
            depth * Millimetre
        };

        Body2 body = Modeler.CreateBodyFromBox3(definition) as Body2;
        if (body == null)
        {
            throw new InvalidOperationException("SOLIDWORKS could not create the requested box body.");
        }

        return body;
    }

    public Body2 Cylinder(
        double x,
        double y,
        double z,
        double ax,
        double ay,
        double az,
        double diameter,
        double length)
    {
        EnsureFinite(x, "cylinder x");
        EnsureFinite(y, "cylinder y");
        EnsureFinite(z, "cylinder z");
        EnsureFinite(ax, "cylinder axis x");
        EnsureFinite(ay, "cylinder axis y");
        EnsureFinite(az, "cylinder axis z");
        EnsurePositive(diameter, "cylinder diameter");
        EnsurePositive(length, "cylinder length");

        double axisLength = Math.Sqrt((ax * ax) + (ay * ay) + (az * az));
        if (axisLength < 0.000000000001 || double.IsInfinity(axisLength))
        {
            throw new ArgumentException("The cylinder axis must have a finite, nonzero length.");
        }

        double[] definition = new double[]
        {
            x * Millimetre,
            y * Millimetre,
            z * Millimetre,
            ax / axisLength,
            ay / axisLength,
            az / axisLength,
            diameter * Millimetre * 0.5,
            length * Millimetre
        };

        Body2 body = Modeler.CreateBodyFromCyl(definition) as Body2;
        if (body == null)
        {
            throw new InvalidOperationException("SOLIDWORKS could not create the requested cylindrical body.");
        }

        return body;
    }

    public Body2 Cut(Body2 target, Body2 tool, string context)
    {
        if (target == null || tool == null)
        {
            throw new ArgumentNullException(target == null ? "target" : "tool");
        }

        int error;
        object operationResult = target.Operations2((int)swBodyOperationType_e.SWBODYCUT, tool, out error);
        if (error != (int)swBodyOperationError_e.swBodyOperationNoError)
        {
            throw new InvalidOperationException(
                "SOLIDWORKS boolean cut failed for " + context + "; operation error " + error.ToString(CultureInfo.InvariantCulture));
        }

        Array bodies = operationResult as Array;
        if (bodies == null || bodies.Rank != 1 || bodies.Length != 1)
        {
            throw new InvalidOperationException(
                "SOLIDWORKS boolean cut must produce exactly one body for " + context + "; actual count " +
                (bodies == null ? "none" : bodies.Length.ToString(CultureInfo.InvariantCulture)));
        }

        Body2 result = bodies.GetValue(bodies.GetLowerBound(0)) as Body2;
        if (result == null)
        {
            throw new InvalidOperationException("SOLIDWORKS boolean cut returned an invalid body for " + context);
        }

        return result;
    }

    public Feature AddBody(ModelDoc2 doc, Body2 body, string name)
    {
        if (doc == null || body == null)
        {
            throw new ArgumentNullException(doc == null ? "doc" : "body");
        }

        PartDoc part = doc as PartDoc;
        if (part == null)
        {
            throw new ArgumentException("A solid body can only be added to a SOLIDWORKS part document.", "doc");
        }

        int options = (int)swCreateFeatureBodyOpts_e.swCreateFeatureBodyCheck |
                      (int)swCreateFeatureBodyOpts_e.swCreateFeatureBodySimplify;
        Feature feature = part.CreateFeatureFromBody3(body, false, options) as Feature;
        if (feature == null)
        {
            throw new InvalidOperationException("SOLIDWORKS could not create the solid feature " + name);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            feature.Name = name;
        }

        return feature;
    }

    public void ApplyMaterial(ModelDoc2 doc, string exactMaterial, double[] rgb)
    {
        if (doc == null)
        {
            throw new ArgumentNullException("doc");
        }

        try
        {
            PartDoc part = doc as PartDoc;
            if (part == null)
            {
                throw new ArgumentException("Material assignment requires a SOLIDWORKS part document.", "doc");
            }

            if (string.IsNullOrWhiteSpace(exactMaterial))
            {
                throw new ArgumentException("An exact material database entry name is required.", "exactMaterial");
            }

            if (!File.Exists(MaterialDatabase))
            {
                throw new FileNotFoundException("The English SOLIDWORKS material database was not found.", MaterialDatabase);
            }

            part.SetMaterialPropertyName2(string.Empty, MaterialDatabase, exactMaterial);

            string appliedDatabase;
            string appliedMaterial = part.GetMaterialPropertyName2(string.Empty, out appliedDatabase);
            if (!string.Equals(appliedMaterial, exactMaterial, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Material readback mismatch; requested '" + exactMaterial + "', actual '" + appliedMaterial + "'.");
            }

            Log("Material " + exactMaterial + " applied to " + doc.GetTitle());
        }
        catch (Exception exception)
        {
            Log("WARNING: Material assignment failed for " + doc.GetTitle() + ": " + exception.Message);
        }

        if (rgb == null)
        {
            return;
        }

        try
        {
            if (rgb.Length < 3)
            {
                throw new ArgumentException("Appearance RGB requires at least three components.", "rgb");
            }

            double red = ValidateColor(rgb[0], "red");
            double green = ValidateColor(rgb[1], "green");
            double blue = ValidateColor(rgb[2], "blue");
            doc.MaterialPropertyValues = new double[]
            {
                red,
                green,
                blue,
                0.25,
                0.75,
                0.25,
                0.35,
                0.0,
                0.0
            };

            doc.GraphicsRedraw2();
        }
        catch (Exception exception)
        {
            Log("WARNING: Appearance assignment failed for " + doc.GetTitle() + ": " + exception.Message);
        }
    }

    public void Property(ModelDoc2 doc, string key, string value)
    {
        if (doc == null)
        {
            throw new ArgumentNullException("doc");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A custom property name is required.", "key");
        }

        CustomPropertyManager manager = doc.Extension.CustomPropertyManager[string.Empty];
        if (manager == null)
        {
            throw new InvalidOperationException("SOLIDWORKS did not provide a custom property manager.");
        }

        int result = manager.Add3(
            key,
            (int)swCustomInfoType_e.swCustomInfoText,
            value ?? string.Empty,
            (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);

        if (result != (int)swCustomInfoAddResult_e.swCustomInfoAddResult_AddedOrChanged)
        {
            throw new InvalidOperationException(
                "SOLIDWORKS could not write custom property '" + key + "'; result " + result.ToString(CultureInfo.InvariantCulture));
        }
    }

    public string SavePart(ModelDoc2 doc, string stem, bool exportStep)
    {
        return SaveNativeAndOptionalStep(doc, PartsDirectory, stem, ".SLDPRT", exportStep);
    }

    public string SaveAssembly(ModelDoc2 asm, string stem, bool exportStep)
    {
        return SaveNativeAndOptionalStep(asm, AssembliesDirectory, stem, ".SLDASM", exportStep);
    }

    public Component2 AddComponent(
        ModelDoc2 asm,
        string partPath,
        string componentLabel,
        double x,
        double y,
        double z)
    {
        if (asm == null)
        {
            throw new ArgumentNullException("asm");
        }

        if (string.IsNullOrWhiteSpace(partPath))
        {
            throw new ArgumentException("The component part path is required.", "partPath");
        }

        EnsureFinite(x, "component x");
        EnsureFinite(y, "component y");
        EnsureFinite(z, "component z");

        string fullPartPath = Path.GetFullPath(partPath);
        if (!File.Exists(fullPartPath))
        {
            throw new FileNotFoundException("The component part does not exist.", fullPartPath);
        }

        int openErrors = 0;
        int openWarnings = 0;
        ModelDoc2 loadedPart = Application.OpenDoc6(
            fullPartPath,
            (int)swDocumentTypes_e.swDocPART,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
            string.Empty,
            ref openErrors,
            ref openWarnings) as ModelDoc2;

        if (loadedPart == null || openErrors != 0)
        {
            throw new InvalidOperationException(
                "SOLIDWORKS could not preload component '" + fullPartPath + "'; error " +
                openErrors.ToString(CultureInfo.InvariantCulture));
        }

        if (openWarnings != 0)
        {
            Log("WARNING: Component preload returned warning " + openWarnings.ToString(CultureInfo.InvariantCulture));
        }

        ActivateDocument(asm);
        AssemblyDoc assembly = asm as AssemblyDoc;
        if (assembly == null)
        {
            throw new ArgumentException("Component insertion requires a SOLIDWORKS assembly document.", "asm");
        }

        Component2 component = assembly.AddComponent5(
            fullPartPath,
            (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig,
            string.Empty,
            false,
            string.Empty,
            0.0,
            0.0,
            0.0) as Component2;

        if (component == null)
        {
            throw new InvalidOperationException("SOLIDWORKS could not insert component " + fullPartPath);
        }

        double xMetres = x * Millimetre;
        double yMetres = y * Millimetre;
        double zMetres = z * Millimetre;
        double[] transformData = new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0,
            xMetres, yMetres, zMetres,
            1.0, 0.0, 0.0, 0.0
        };

        MathUtility utility = Application.GetMathUtility() as MathUtility;
        if (utility == null)
        {
            throw new InvalidOperationException("SOLIDWORKS did not provide its transformation utility.");
        }

        MathTransform transform = utility.CreateTransform(transformData) as MathTransform;
        if (transform == null)
        {
            throw new InvalidOperationException("SOLIDWORKS could not create the component positioning transform.");
        }

        component.Transform2 = transform;
        MathTransform actualTransform = component.Transform2;
        Array actualData = actualTransform == null ? null : actualTransform.ArrayData as Array;
        if (actualData == null || actualData.Length < 12 ||
            !NearlyEqual(Convert.ToDouble(actualData.GetValue(9), CultureInfo.InvariantCulture), xMetres) ||
            !NearlyEqual(Convert.ToDouble(actualData.GetValue(10), CultureInfo.InvariantCulture), yMetres) ||
            !NearlyEqual(Convert.ToDouble(actualData.GetValue(11), CultureInfo.InvariantCulture), zMetres))
        {
            throw new InvalidOperationException(
                "SOLIDWORKS component transform readback did not match the requested position for " +
                (componentLabel ?? fullPartPath));
        }

        if (!string.IsNullOrWhiteSpace(componentLabel))
        {
            try
            {
                component.Name2 = componentLabel;
            }
            catch (Exception exception)
            {
                Log("WARNING: Component display-name assignment failed for " + componentLabel + ": " + exception.Message);
            }
        }

        assembly.UpdateBox();
        return component;
    }

    public void Show(ModelDoc2 doc)
    {
        if (doc == null)
        {
            throw new ArgumentNullException("doc");
        }

        ActivateDocument(doc);
        doc.ShowNamedView2(string.Empty, (int)swStandardViews_e.swIsometricView);
        doc.ViewZoomtofit2();
        doc.GraphicsRedraw2();
    }

    public void Log(string message)
    {
        Console.WriteLine("[RackCad] " + message);
        Console.Out.Flush();
    }

    private static SldWorks AttachOrStartSolidWorks()
    {
        string[] programIds = new string[] { "SldWorks.Application.33", "SldWorks.Application" };
        for (int index = 0; index < programIds.Length; index++)
        {
            try
            {
                SldWorks existing = Marshal.GetActiveObject(programIds[index]) as SldWorks;
                if (existing != null)
                {
                    return existing;
                }
            }
            catch (COMException)
            {
                // No registered running instance is available for this program identifier.
            }
        }

        for (int index = 0; index < programIds.Length; index++)
        {
            Type serverType = Type.GetTypeFromProgID(programIds[index], false);
            if (serverType == null)
            {
                continue;
            }

            SldWorks application = Activator.CreateInstance(serverType) as SldWorks;
            if (application != null)
            {
                return application;
            }
        }

        throw new InvalidOperationException("The SOLIDWORKS 2025 COM automation server is not registered.");
    }

    private ModelDoc2 NewDocument(string templatePath, string title, string documentKind)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException("The SOLIDWORKS " + documentKind + " template was not found.", templatePath);
        }

        ModelDoc2 document = Application.NewDocument(templatePath, 0, 0.0, 0.0) as ModelDoc2;
        if (document == null)
        {
            throw new InvalidOperationException("SOLIDWORKS could not create a " + documentKind + " document.");
        }

        if (!string.IsNullOrWhiteSpace(title) && !document.SetTitle2(title))
        {
            Log("WARNING: SOLIDWORKS did not apply the requested document title " + title);
        }

        return document;
    }

    private string SaveNativeAndOptionalStep(
        ModelDoc2 document,
        string directory,
        string stem,
        string nativeExtension,
        bool exportStep)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        string cleanStem = ValidateStem(stem);
        string nativePath = Path.Combine(directory, cleanStem + nativeExtension);
        SaveDocument(document, nativePath);

        if (exportStep)
        {
            string stepPath = Path.Combine(ExportsDirectory, cleanStem + ".STEP");
            SaveDocument(document, stepPath);
        }

        return nativePath;
    }

    private void SaveDocument(ModelDoc2 document, string path)
    {
        ActivateDocument(document);
        document.ClearSelection2(true);
        if (!document.ForceRebuild3(false))
        {
            throw new InvalidOperationException("SOLIDWORKS reported a rebuild failure before saving " + path);
        }

        int errors = 0;
        int warnings = 0;
        bool saved = document.Extension.SaveAs(
            path,
            (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
            (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
            null,
            ref errors,
            ref warnings);

        if (!saved || errors != 0)
        {
            throw new InvalidOperationException(
                "SOLIDWORKS could not save '" + path + "'; success=" + saved.ToString() +
                ", errors=" + errors.ToString(CultureInfo.InvariantCulture) +
                ", warnings=" + warnings.ToString(CultureInfo.InvariantCulture));
        }

        FileInfo savedFile = new FileInfo(path);
        if (!savedFile.Exists || savedFile.Length <= 0)
        {
            throw new InvalidOperationException("SOLIDWORKS reported success but the saved file is missing or empty: " + path);
        }

        if (warnings != 0)
        {
            Log("WARNING: Saving " + path + " returned warning " + warnings.ToString(CultureInfo.InvariantCulture));
        }

        Log("Saved " + path + " (" + savedFile.Length.ToString(CultureInfo.InvariantCulture) + " bytes)");
    }

    private ModelDoc2 ActivateDocument(ModelDoc2 document)
    {
        string title = document.GetTitle();
        int activationError = 0;
        ModelDoc2 active = Application.ActivateDoc3(
            title,
            false,
            (int)swRebuildOnActivation_e.swDontRebuildActiveDoc,
            ref activationError) as ModelDoc2;

        if (active == null)
        {
            throw new InvalidOperationException(
                "SOLIDWORKS could not activate document '" + title + "'; error " +
                activationError.ToString(CultureInfo.InvariantCulture));
        }

        if (activationError != 0)
        {
            Log("WARNING: Activating " + title + " returned status " + activationError.ToString(CultureInfo.InvariantCulture));
        }

        return active;
    }

    private static string ValidateStem(string stem)
    {
        if (string.IsNullOrWhiteSpace(stem) || !string.Equals(stem, Path.GetFileName(stem), StringComparison.Ordinal))
        {
            throw new ArgumentException("A plain output filename stem without a directory is required.", "stem");
        }

        string cleanStem = Path.GetFileNameWithoutExtension(stem);
        if (string.IsNullOrWhiteSpace(cleanStem) || cleanStem.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("The output filename stem contains invalid characters.", "stem");
        }

        return cleanStem;
    }

    private static void EnsurePositive(double value, string description)
    {
        EnsureFinite(value, description);
        if (value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(description, "The dimension must be greater than zero.");
        }
    }

    private static void EnsureFinite(double value, string description)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(description, "The value must be a finite number.");
        }
    }

    private static double ValidateColor(double value, string channel)
    {
        EnsureFinite(value, channel);
        if (value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(channel, "Appearance RGB components must be in the range 0 to 1.");
        }

        return value;
    }

    private static bool NearlyEqual(double first, double second)
    {
        return Math.Abs(first - second) <= TransformTolerance;
    }
}
