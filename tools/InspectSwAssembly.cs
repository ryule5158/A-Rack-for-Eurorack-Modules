using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class InspectSwAssembly
{
    private static void Main(string[] args)
    {
        SldWorks sw;
        try
        {
            sw=(SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            Console.WriteLine("SOLIDWORKS_STARTED=false");
        }
        catch(COMException)
        {
            sw=new SldWorks();
            sw.Visible=true;
            sw.UserControl=true;
            Console.WriteLine("SOLIDWORKS_STARTED=true");
        }
        sw.Visible=true;
        sw.UserControl=true;
        ModelDoc2 doc=sw.ActiveDoc as ModelDoc2;
        string restorePath=System.Environment.GetEnvironmentVariable(
            "RACK_INSPECT_RESTORE_NATIVE");
        if(!string.IsNullOrWhiteSpace(restorePath))
        {
            restorePath=Path.GetFullPath(restorePath);
            if(!File.Exists(restorePath))
                throw new FileNotFoundException("Native restore document not found",restorePath);
            string activePath=doc==null?string.Empty:doc.GetPathName();
            if(!string.Equals(activePath,restorePath,StringComparison.OrdinalIgnoreCase))
            {
                int restoreErrors=0,restoreWarnings=0;
                int restoreType=string.Equals(Path.GetExtension(restorePath),".SLDPRT",
                        StringComparison.OrdinalIgnoreCase)
                    ? (int)swDocumentTypes_e.swDocPART
                    : (int)swDocumentTypes_e.swDocASSEMBLY;
                doc=sw.OpenDoc6(restorePath,restoreType,
                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                    string.Empty,ref restoreErrors,ref restoreWarnings) as ModelDoc2;
                Console.WriteLine("RESTORE_OPEN_ERRORS="+restoreErrors+
                    " WARNINGS="+restoreWarnings);
                if(doc==null||restoreErrors!=0)
                    throw new InvalidOperationException(
                        "Unable to open native restore document: "+restorePath);
            }
            Console.WriteLine("RESTORE_NATIVE="+restorePath);
        }
        if(string.Equals(System.Environment.GetEnvironmentVariable(
            "RACK_INSPECT_LIST_OPEN_DOCS"),"1",StringComparison.Ordinal))
        {
            ModelDoc2 cursor=sw.GetFirstDocument() as ModelDoc2;
            int openCount=0;
            while(cursor!=null)
            {
                openCount++;
                ModelView firstView=null;
                try { firstView=cursor.GetFirstModelView() as ModelView; }
                catch { }
                Console.WriteLine("OPEN_DOC="+cursor.GetTitle()+" PATH="+
                    cursor.GetPathName()+" DIRTY="+cursor.GetSaveFlag()+
                    " ACTIVE="+SameDocument(cursor,doc)+
                    " HAS_VIEW="+(firstView!=null));
                cursor=cursor.GetNext() as ModelDoc2;
            }
            Console.WriteLine("OPEN_DOC_COUNT="+openCount);
        }
        ModelDoc2 original=doc;
        bool opened=false;
        string temporaryDirectory=null;
        if(args!=null&&args.Length==1)
        {
            int errors=0,warnings=0;
            string path=Path.GetFullPath(args[0]);
            if(string.Equals(Path.GetExtension(path),".STEP",StringComparison.OrdinalIgnoreCase)||
                string.Equals(Path.GetExtension(path),".STP",StringComparison.OrdinalIgnoreCase))
            {
                string validatedBuildRoot=Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
                temporaryDirectory=Path.Combine(validatedBuildRoot,"step_readback",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temporaryDirectory);
                // Keep the imported document title distinct from any native
                // assembly already open in SolidWorks. CloseDoc is title-based;
                // reusing the source title can close the native document and
                // leave the pathless STEP import resident in the session.
                string copy=Path.Combine(temporaryDirectory,
                    "STEP_READBACK_"+Guid.NewGuid().ToString("N")+"_"+
                    Path.GetFileName(path));
                File.Copy(path,copy,false);
                object importData=sw.GetImportFileData(copy);
                doc=sw.LoadFile4(copy,"r",importData,ref errors) as ModelDoc2;
            }
            else
            {
                int type=string.Equals(Path.GetExtension(path),".SLDPRT",StringComparison.OrdinalIgnoreCase)
                    ? (int)swDocumentTypes_e.swDocPART
                    : (int)swDocumentTypes_e.swDocASSEMBLY;
                doc=sw.OpenDoc6(path,type,(int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                    string.Empty,ref errors,ref warnings) as ModelDoc2;
                // OpenDoc6 can return the already-active native document when
                // the requested path is the final showcase.  Do not close
                // that original document in the cleanup path.
                opened=doc!=null&&!SameDocument(doc,original);
            }
            if(string.Equals(Path.GetExtension(path),".STEP",StringComparison.OrdinalIgnoreCase)||
                string.Equals(Path.GetExtension(path),".STP",StringComparison.OrdinalIgnoreCase))
                opened=true;
            Console.WriteLine("OPEN_ERRORS="+errors+" WARNINGS="+warnings);
        }
        if(doc==null) throw new InvalidOperationException("No active SolidWorks document");
        Console.WriteLine("DOC="+doc.GetTitle()+" PATH="+doc.GetPathName());
        MassProperty mass=doc.Extension.CreateMassProperty();
        Console.WriteLine("MASS_KG="+(mass==null?double.NaN:mass.Mass));
        CustomPropertyManager properties=doc.Extension.CustomPropertyManager[string.Empty];
        Array propertyNames=properties==null?null:properties.GetNames() as Array;
        if(propertyNames!=null)
        {
            foreach(object rawName in propertyNames)
            {
                string name=Convert.ToString(rawName);
                string value,resolved;
                bool wasResolved,linked;
                properties.Get6(name,false,out value,out resolved,out wasResolved,out linked);
                Console.WriteLine("PROPERTY="+name+"="+
                    (string.IsNullOrWhiteSpace(resolved)?value:resolved));
            }
        }
        AssemblyDoc asm=doc as AssemblyDoc;
        if(asm==null)
        {
            PartDoc part=doc as PartDoc;
            Array bodies=part==null?null:part.GetBodies2((int)swBodyType_e.swSolidBody,true) as Array;
            Console.WriteLine("SOLID_BODIES="+(bodies==null?0:bodies.Length));
            if(bodies!=null)
            {
                foreach(object item in bodies)
                {
                    Body2 body=item as Body2;
                    Array box=body==null?null:body.GetBodyBox() as Array;
                    if(box!=null) Console.WriteLine("BODY_BOX_MM="+string.Join(",",box.Cast<object>()
                        .Select(x=>Convert.ToDouble(x)*1000.0)));
                }
            }
            if(opened&&!SameDocument(doc,original)) sw.CloseDoc(doc.GetTitle());
            CleanupTemporaryDirectory(temporaryDirectory);
            if(original!=null)
            {
                int activateError=0;
                sw.ActivateDoc3(original.GetTitle(),false,
                    (int)swRebuildOnActivation_e.swDontRebuildActiveDoc,ref activateError);
            }
            return;
        }
        Array raw=asm.GetComponents(true) as Array;
        if(raw==null) return;
        Console.WriteLine("COMPONENT_COUNT="+raw.Length);
        int unresolved=0,unfixed=0;
        foreach(object item in raw)
        {
            Component2 component=item as Component2;
            if(component==null) continue;
            if(component.GetModelDoc2()==null) unresolved++;
            if(!component.IsFixed()) unfixed++;
        }
        Console.WriteLine("UNRESOLVED_COMPONENTS="+unresolved+" UNFIXED_COMPONENTS="+unfixed);
        bool summaryOnly=string.Equals(
            System.Environment.GetEnvironmentVariable("RACK_INSPECT_SUMMARY"),"1",
            StringComparison.Ordinal);
        if(!summaryOnly) foreach(object o in raw)
        {
            Component2 c=o as Component2;
            if(c==null) continue;
            Console.WriteLine("COMP="+c.Name2+" FIXED="+c.IsFixed()+" PATH="+c.GetPathName());
            try
            {
                Array b=c.GetBox(false,false) as Array;
                if(b!=null) Console.WriteLine("BOX="+string.Join(",",b.Cast<object>().Select(x=>Convert.ToString(x))));
            }
            catch(Exception e){Console.WriteLine("BOXERR="+e.Message);}
            try
            {
                MathTransform t=c.Transform2;
                Array a=t==null?null:t.ArrayData as Array;
                if(a!=null) Console.WriteLine("T="+string.Join(",",a.Cast<object>().Select(x=>Convert.ToString(x))));
            }
            catch(Exception e){Console.WriteLine("TERR="+e.Message);}
        }
        InterferenceDetectionMgr mgr=asm.InterferenceDetectionManager;
        try
        {
            mgr.TreatCoincidenceAsInterference=false;
            mgr.IncludeMultibodyPartInterferences=false;
            Array items=mgr.GetInterferences() as Array;
            Console.WriteLine("INTERFERENCE_COUNT="+(items==null?0:items.Length));
            if(items!=null) foreach(object o in items)
            {
                Interference i=o as Interference;
                if(i==null) continue;
                Array participants=i.Components as Array;
                Console.WriteLine("INTERFERENCE="+string.Join(" + ",participants.Cast<object>()
                    .Select(x=>x as Component2).Where(x=>x!=null)
                    .Select(x=>Path.GetFileNameWithoutExtension(x.GetPathName())))+
                    " VOLUME_MM3="+(i.Volume*1000000000.0));
                Body2 body=i.GetInterferenceBody() as Body2;
                Array box=body==null?null:body.GetBodyBox() as Array;
                if(box!=null) Console.WriteLine("IBOX="+string.Join(",",box.Cast<object>()
                    .Select(x=>Convert.ToDouble(x)*1000.0)));
            }
        }
        finally
        {
            if(mgr!=null) try { mgr.Done(); } catch { }
            if(opened&&doc!=null&&!SameDocument(doc,original)) sw.CloseDoc(doc.GetTitle());
            CleanupTemporaryDirectory(temporaryDirectory);
            if(original!=null)
            {
                int activateError=0;
                sw.ActivateDoc3(original.GetTitle(),false,
                    (int)swRebuildOnActivation_e.swDontRebuildActiveDoc,ref activateError);
                if(string.Equals(System.Environment.GetEnvironmentVariable(
                    "RACK_INSPECT_SAVE_RESTORE_AFTER_CHECK"),"1",StringComparison.Ordinal))
                {
                    if(string.IsNullOrWhiteSpace(restorePath)||
                        !string.Equals(Path.GetFullPath(original.GetPathName()),
                            Path.GetFullPath(restorePath),StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "Refusing to save an active document other than the explicit restore target");
                    int saveErrors=0,saveWarnings=0;
                    bool saved=original.Save3(
                        (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                        ref saveErrors,ref saveWarnings);
                    if(!saved||saveErrors!=0||original.GetSaveFlag())
                        throw new InvalidOperationException(
                            "Final restore document did not save cleanly; errors="+saveErrors+
                            " warnings="+saveWarnings);
                    Console.WriteLine("SAVED_RESTORE_NATIVE=true WARNINGS="+saveWarnings);
                }
                sw.Visible=true;
                sw.UserControl=true;
                sw.FrameState=(int)swWindowState_e.swWindowMaximized;
                original.GraphicsRedraw2();
            }
        }
    }

    private static void CleanupTemporaryDirectory(string path)
    {
        if(string.IsNullOrWhiteSpace(path)||!Directory.Exists(path)) return;
        string full=Path.GetFullPath(path);
        string buildRoot=Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
        string allowed=Path.Combine(buildRoot,"step_readback")+Path.DirectorySeparatorChar;
        if(!full.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing to delete unexpected STEP readback path: "+full);
        Directory.Delete(full,true);
    }

    private static bool SameDocument(ModelDoc2 a,ModelDoc2 b)
    {
        if(a==null||b==null) return false;
        string ap=a.GetPathName(),bp=b.GetPathName();
        if(!string.IsNullOrWhiteSpace(ap)&&!string.IsNullOrWhiteSpace(bp))
            return string.Equals(Path.GetFullPath(ap),Path.GetFullPath(bp),
                StringComparison.OrdinalIgnoreCase);
        return string.Equals(a.GetTitle(),b.GetTitle(),StringComparison.OrdinalIgnoreCase);
    }
}
