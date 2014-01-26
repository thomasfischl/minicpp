// MiniCpp.cs                                                    HDO, 2006-08-28
// ----------                                                    SE,  2009-12-20
// Main program for MiniCpp compiler.
//=====================================|========================================

// *** start: not in Main.frm ***

#define GEN_SRC          // (en|dis)able gen. of source text (with symbol table dump)
#define GEN_CIL_AS_TEXT  // (en|dis)able CIL text generation and assembling to exe
#define GEN_CIL_REF_EMIT // (en|dis)able CIL generation with Reflection.Emit

#undef VERIFY_ASSEMBLY  // (en|dis)able CIL verification with PEVerify.exe
#undef MEASURE_TIME     // (en|dis)able time measurements for some phases

// *** end ***

using System;
using System.IO;

using Lex = MiniCppLex;
using Syn = MiniCppSyn;

public class MiniCpp {

  private static String NAME = "MiniCpp";

  private static void Abort(String abortKind, String moduleName,
                            String methName,  String descr) {
    Console.WriteLine();
    Console.WriteLine("*** {0} in module {1} function {2}", 
                      abortKind, moduleName, methName);
    Console.WriteLine("*** {0}", descr);
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine(NAME + " aborted");
    Utils.Modules(Utils.ModuleAction.cleanupModule);
    Environment.Exit(Utils.EXIT_FAILURE);
  } // Abort

  private static void CompileFile(String srcFileName) {
    try {
      FileStream srcFs = new FileStream(srcFileName, FileMode.Open);
      Lex.src = new StreamReader(srcFs);
    } catch (Exception) {
      Lex.src = null;
    } // try/catch
    if (Lex.src == null) {
      Console.WriteLine("*** file \"{0}\" not found", srcFileName);
      return;
    } // if
    Console.WriteLine("parsing           \"" + srcFileName + "\" ...");
    Syn.Parse();
    Lex.src.Close();
    Lex.src = null;
    int extStart = srcFileName.LastIndexOf('.');
    String lstFileName = srcFileName.Substring(0, extStart) + ".lst";
    if (Errors.NumOfErrors() > 0) {
      Console.WriteLine("{0} error(s) detected,", Errors.NumOfErrors());
      Console.WriteLine("  listing to      \"" + lstFileName + "\"...");
      StreamWriter lst = null;
      try {
        FileStream lstFs = new FileStream(lstFileName, FileMode.Create);
        lst = new StreamWriter(lstFs);
      } catch (Exception) {
        lst = null;
      } // try/catch
      if (lst == null) {
        Utils.FatalError(NAME, "CompileFile", "file \"{0}\" not created", lstFileName);
        return;
      } // if
      FileStream srcFs = new FileStream(srcFileName, FileMode.Open);
      Lex.src = new StreamReader(srcFs);
      lst.WriteLine(NAME + " (file: \"{0}\")", srcFileName);
      Errors.GenerateListing(Lex.src, lst, Errors.ListingShape.longListing);
      Lex.src.Close();
      Lex.src = null;      
      lst.Close();
      lst = null;
    } else {
      
// *** start: not in Main.frm ***

      if (File.Exists(lstFileName))
        File.Delete(lstFileName);

      String moduleName = (String)srcFileName.Clone();
      String path = String.Empty;

      int lastBackSlashIdx = moduleName.LastIndexOf('\\');
      if (lastBackSlashIdx >= 0) {
        path = moduleName.Substring(0, lastBackSlashIdx + 1);
        moduleName = moduleName.Substring(lastBackSlashIdx + 1);
      } // if

      int periodIdx = moduleName.IndexOf('.');
      if (periodIdx >= 0)
        moduleName = moduleName.Substring(0, periodIdx);
      
#if GEN_SRC  // symbol table gen. of source text with symbol table dump
      StartTimer();
      GenSrcText.DumpSymTabAndWriteSrcTxt(path, moduleName);
      WriteElapsedTime("GenSrcText");
#endif

#if GEN_CIL_AS_TEXT // CIL generation to il-file and assembling to exe
      StartTimer();
      GenCilAsText.GenerateAssembly(path, moduleName + ".text");
      WriteElapsedTime("GenCilAsText");      
      VerifyAssembly(path, moduleName);
#endif // GEN_CIL_AS_TEXT

#if GEN_CIL_REF_EMIT // CIL generation with Reflection.Emit
      StartTimer();
      GenCilByRefEmit.GenerateAssembly(path, moduleName+ ".emit");
      WriteElapsedTime("GenCilByReflectionEmit");
      VerifyAssembly(path, moduleName);
#endif

// *** end ***

      Console.WriteLine("compilation completed successfully");
    } // else
    Utils.Modules(Utils.ModuleAction.resetModule);
  } // CompileFile


// *** start: not in Main.frm ***

  private static readonly System.Diagnostics.Stopwatch Timer = 
    new System.Diagnostics.Stopwatch();
  
  [System.Diagnostics.Conditional("MEASURE_TIME")]
  private static void WriteElapsedTime(string label) {
    Console.WriteLine("elapsed time for '{0}': {1} ms", 
                      label, Timer.ElapsedMilliseconds);
  } // WriteElapsedTime

  [System.Diagnostics.Conditional("MEASURE_TIME")]
  private static void StartTimer() {
    Timer.Reset();
    Timer.Start();
  } // StartTimer

  [System.Diagnostics.Conditional("VERIFY_ASSEMBLY")]
  private static void VerifyAssembly(string path, string moduleName) {
    Console.WriteLine();
    Console.WriteLine("PEverifying       \"" + path + moduleName + ".exe ...");
    string filename = moduleName;
    if (!string.IsNullOrEmpty(path))
      filename = path + moduleName;
    using (System.Diagnostics.Process p = new System.Diagnostics.Process()) {
      p.StartInfo.FileName = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\PEVerify.exe";
      p.StartInfo.Arguments = "/NOLOGO " + filename + ".exe";
      p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardError = true;
      p.StartInfo.RedirectStandardOutput = true;
      try {
        p.Start();
        String output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        Console.Out.Write(output);
      } catch (Exception e) {
        Console.Out.WriteLine(e);
      } // catch
    } // using
    Console.WriteLine();
  } // VerifyAssembly
  
// *** end ***
  

  public static void Main(String[] args) {
  //-----------------------------------|----------------------------------------

    // --- install modules ---
    Utils.InstallModule("Utils",   Utils.UtilsMethod);
    Utils.InstallModule("Sets",    Sets.SetsMethod);
    Utils.InstallModule("Errors",  Errors.ErrorsMethod);

    Utils.InstallModule("MiniCppLex", MiniCppLex.MiniCppLexMethod);
    Utils.InstallModule("MiniCppSem", MiniCppSem.MiniCppSemMethod);
    Utils.InstallModule("MiniCppSyn", MiniCppSyn.MiniCppSynMethod);

    // --- initialize modules ---
    Utils.Modules(Utils.ModuleAction.initModule);

    Errors.PushAbortMethod(Abort);

    Console.WriteLine("--------------------------------");       
    Console.WriteLine(" {0} Compiler {1," + (5 - NAME.Length) +"}  V. 3 2011 ", NAME, "");
    Console.WriteLine(" Frontend generated with Coco-2");
    Console.WriteLine("--------------------------------");
    Console.WriteLine();

    if (args.Length > 0) { // command line mode
      Console.WriteLine();
      int i = 0;
      do {
        Console.WriteLine("source file       \"{0}\"", args[i]);
        CompileFile(args[i]);
        Console.WriteLine();
        i++;
      } while (i < args.Length);
    } else { // args.Length == 0, interactive mode
      for (;;) {
        String srcFileName;
        Utils.GetInputFileName("source file      > ", out srcFileName);
        if (srcFileName.Length > 0) {

// *** start: not in Main.frm ***
          if (!srcFileName.EndsWith(".mcpp"))
            srcFileName = srcFileName + ".mcpp";
          if (!File.Exists(srcFileName)) { 
            srcFileName = ".\\#McppPrograms\\" + srcFileName;
            if (!File.Exists(srcFileName)) { 
              srcFileName = "..\\..\\" + srcFileName;
            } // if
          } // if
// *** end ***

          CompileFile(srcFileName);
        } // if
        char answerCh;
        do {
          Console.WriteLine();
          Console.Write("[c]ontinue or [q]uit > ");
          String answer = Console.ReadLine();
          answerCh = answer.Length == 0 ? ' ' : Char.ToUpper(answer[0]);
        } while (answerCh != 'C' && answerCh != 'Q');
        if (answerCh == 'Q')
          break;
        Console.WriteLine();
      } // for
    } // else

    Utils.Modules(Utils.ModuleAction.cleanupModule);

  } // Main

} // MiniCpp

// End of MiniCpp.cs
//=====================================|========================================