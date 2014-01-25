// Errors.cs                                                     HDO, 2006-08-28
// ---------
// Error handling, storage of error messages and listing generation.
//=====================================|========================================

#undef TEST_ERRORS

using System;
using System.IO;

public class Errors {

  public const String MODULENAME = "Errors";

  public static void ErrorsMethod(Utils.ModuleAction action, out String moduleName) {
    //-----------------------------------|----------------------------------------
    moduleName = MODULENAME;
    switch (action) {
      case Utils.ModuleAction.getModuleName:
        return;
      case Utils.ModuleAction.initModule:
        aps = new AbortMethod[MAXABORTPROCS];
        sp = 0;
        PushAbortMethod(DefaultAbort);
        stopParsing = null;
        eowCnt = new int[4];
        curEoW = new ErrorWarn[4];
        break;
      case Utils.ModuleAction.resetModule:
        break;
      case Utils.ModuleAction.cleanupModule:
        eowl = null;
        eowCnt = null;
        curEoW = null;
        return;
    } // switch
    // --- for initModule and resetModule ---
    eowl = null;
    numOfErrs = 0;
    numOfWarns = 0;
    for (int i = 0; i < 4; i++)
      eowCnt[i] = 0;
  } // ErrorsMethod


  public const int MAXABORTPROCS = 3;
  public const int MAXNUMOFERRS = 30; // sum of lex., syn. and sem. errors
  public const int MAXNUMOFWARNS = 50;

  public delegate void StopParsingMethod();

  public delegate void AbortMethod(String abortKind, String moduleName,
                                   String methodName, String descr);

  enum EoWKind {
    lexErr,
    synErr,
    semErr,
    warn
  } // EoWKind

  public class ErrorWarnInfo {
    public String msg;
    public int line, col;
  } // ErrorWarnInfo

  private class ErrorWarn {
    public EoWKind kind;
    public ErrorWarnInfo info;
    public ErrorWarn next;
  } // ErrorWarn

  private static AbortMethod[] aps; // abort method stack
  private static int sp;  // stack pointer for aps
  private static StopParsingMethod stopParsing;

  // --- errors and/or warnings handling ---

  private static ErrorWarn eowl;        // sorted list of errors or warnings
  private static int numOfErrs, numOfWarns;
  private static int[] eowCnt;

  private static void DefaultAbort(String abortKind, String moduleName,
                                   String funcName, String descr) {
    Console.WriteLine();
    Console.WriteLine("*** {0} in module {1} function {2}",
                      abortKind, moduleName, funcName);
    Console.WriteLine("*** {0}", descr);
    Utils.Modules(Utils.ModuleAction.cleanupModule);
    Environment.Exit(Utils.EXIT_FAILURE);
  } // DefaultAbort


  // --- install stop parsing and abort functions ---

  public static void InstallStopParsingMethod(StopParsingMethod spp) {
    //-----------------------------------|----------------------------------------
    stopParsing = spp;
  } // InstallStopParsingMethod

  public static void PushAbortMethod(AbortMethod ap) {
    //-----------------------------------|----------------------------------------
    if (sp == MAXABORTPROCS)
      Restriction(MODULENAME, "PushAbortFunc", "too many abort functions");
    aps[sp++] = ap;
  } // PushAbortMethod


  // --- report error or restriction and call abort functions ---

  public static void CompilerError(String moduleName, String funcName,
                                   String fmt, params Object[] p) {
    //-----------------------------------|----------------------------------------
    TextWriter w = new StringWriter();
    w.WriteLine(fmt, p);
    String msg = w.ToString();
    for (int i = sp - 1; i >= 0; i--)
      aps[i]("compiler error", moduleName, funcName, msg); // should not return*
    DefaultAbort("compiler error", moduleName, funcName, msg);
  } // CompilerError

  public static void Restriction(String moduleName, String funcName,
                                 String fmt, params Object[] p) {
    //-----------------------------------|----------------------------------------
    TextWriter w = new StringWriter();
    w.WriteLine(fmt, p);
    String msg = w.ToString();
    for (int i = sp - 1; i >= 0; i--)
      aps[i]("restriction", moduleName, funcName, msg); // should not return
    DefaultAbort("restriction", moduleName, funcName, msg);
  } // Restriction

  public static void CallStopParsing() {
    //-----------------------------------|----------------------------------------
    if (stopParsing != null)
      stopParsing();
  } // CallStopParsing


  // --- storage of errors found on compilation ---

  private static void EnterMessage(EoWKind knd, int line, int col, String msg) {
    ErrorWarn eow, prveow, nxteow;
    eowCnt[(int)knd]++;
    eow = new ErrorWarn();
    eow.kind = knd;
    eow.info = new ErrorWarnInfo();
    eow.info.line = line;
    eow.info.col = col;
    eow.info.msg = msg;
    prveow = null;
    nxteow = eowl;
    while (nxteow != null && nxteow.info.line <= line) {
      prveow = nxteow;
      nxteow = nxteow.next;
    } // while
    while (nxteow != null && nxteow.info.line == line && nxteow.info.col <= col) {
      prveow = nxteow;
      nxteow = nxteow.next;
    } // while
    if (prveow == null)
      eowl = eow;
    else
      prveow.next = eow;
    eow.next = nxteow;
  } // EnterMessage

  private static void CheckForLimits(EoWKind knd, int line, int col) {
    if (knd == EoWKind.lexErr || knd == EoWKind.synErr || knd == EoWKind.semErr) {
      numOfErrs++;
      if (numOfErrs == MAXNUMOFERRS) {
        Warning(line, col, "too many errors, parsing stopped");
        CallStopParsing();
      } // if
    } else { // knd == EoWKind.warn
      numOfWarns++;
      if (numOfWarns == MAXNUMOFWARNS) {
        Warning(line, col, "too many warnings, parsing stopped");
        CallStopParsing();
      } // if
    } // else
  } // CheckForLimits


  public static void LexError(int line, int col, String fmt, params Object[] p) {
    //-----------------------------------|----------------------------------------
    if (numOfErrs == MAXNUMOFERRS)
      return;
    TextWriter w = new StringWriter();
    w.WriteLine(fmt, p);
    String msg = w.ToString();
    EnterMessage(EoWKind.lexErr, line, col, msg);
    CheckForLimits(EoWKind.lexErr, line, col);
  } // SynError

  public static void SynError(int line, int col, String fmt, params Object[] p) {
    //-----------------------------------|----------------------------------------
    if (numOfErrs == MAXNUMOFERRS)
      return;
    TextWriter w = new StringWriter();
    w.WriteLine(fmt, p);
    String msg = w.ToString();
    EnterMessage(EoWKind.synErr, line, col, msg);
    CheckForLimits(EoWKind.synErr, line, col);
  } // SynError

  public static void SemError(int line, int col, String fmt, params Object[] p) {
    //-----------------------------------|----------------------------------------
    if (numOfErrs == MAXNUMOFERRS)
      return;
    TextWriter w = new StringWriter();
    w.WriteLine(fmt, p);
    String msg = w.ToString();
    EnterMessage(EoWKind.semErr, line, col, msg);
    CheckForLimits(EoWKind.semErr, line, col);
  } // SemError

  public static void Warning(int line, int col, String fmt, params Object[] p) {
    //-----------------------------------|----------------------------------------
    if (numOfWarns == MAXNUMOFWARNS)
      return;
    TextWriter w = new StringWriter();
    w.WriteLine(fmt, p);
    String msg = w.ToString();
    EnterMessage(EoWKind.warn, line, col, msg);
    CheckForLimits(EoWKind.warn, line, col);
  } // Warning


  // --- retrieval of stored source errors and warnings ---

  public static int NumOfErrors() {
    //-----------------------------------|----------------------------------------
    return numOfErrs;
  } // NumOfErrors

  public static int NumOfLexErrors() {
    //-----------------------------------|----------------------------------------
    return eowCnt[(int)EoWKind.lexErr];
  } // NumOfLexErrors

  public static int NumOfSynErrors() {
    //-----------------------------------|----------------------------------------
    return eowCnt[(int)EoWKind.synErr];
  } // NumOfSynErrors

  public static int NumOfSemErrors() {
    //-----------------------------------|----------------------------------------
    return eowCnt[(int)EoWKind.semErr];
  } // NumOfSemErrors

  public static int NumOfWarnings() {
    //-----------------------------------|----------------------------------------
    return eowCnt[(int)EoWKind.warn];
  } // NumOfWarnings


  private static ErrorWarn[] curEoW;

  private static ErrorWarnInfo GetMessage(bool first, EoWKind knd) {
    ErrorWarn eow = null;
    ErrorWarnInfo eowi = null;
    if (first)
      eow = eowl;
    else
      eow = curEoW[(int)knd].next;
    while (eow != null && eow.kind != knd)
      eow = eow.next;
    curEoW[(int)knd] = eow;
    if (eow == null) {
      eowi = new ErrorWarnInfo();
      eowi.msg = "";
      eowi.line = 0;
      eowi.col = 0;
    } else
      eowi = eow.info;
    return eowi;
  } // GetMessage

  public static ErrorWarnInfo GetLexError(bool first) {
    //-----------------------------------|----------------------------------------
    return GetMessage(first, EoWKind.lexErr);
  } // ErrorWarnInfo

  public static ErrorWarnInfo GetSynError(bool first) {
    //-----------------------------------|----------------------------------------
    return GetMessage(first, EoWKind.synErr);
  } // ErrorWarnInfo

  public static ErrorWarnInfo GetSemError(bool first) {
    //-----------------------------------|----------------------------------------
    return GetMessage(first, EoWKind.semErr);
  } // ErrorWarnInfo

  public static ErrorWarnInfo GetWarning(bool first) {
    //-----------------------------------|----------------------------------------
    return GetMessage(first, EoWKind.warn);
  } // ErrorWarnInfo


  // --- listing generation ---

  private static void PutMsg(TextWriter lst, ErrorWarn eow) {
    switch (eow.kind) {
      case EoWKind.lexErr:
        lst.Write("+LEX+");
        break;
      case EoWKind.synErr:
        lst.Write("*SYN*");
        break;
      case EoWKind.semErr:
        lst.Write("#SEM#");
        break;
      case EoWKind.warn:
        lst.Write("!WRN!");
        break;
    } // switch 
    for (int i = 0; i < eow.info.col; i++)
      lst.Write(" ");
    lst.Write(" ^{0}", eow.info.msg);
  } // PutMsg

  public enum ListingShape {
    longListing,
    shortListing
  } // ListingShape

  public static void GenerateListing(TextReader src, TextWriter lst,
                                     ListingShape listShape) {
    //-----------------------------------|----------------------------------------
    ErrorWarn eow = null;
    int lnr, skip;
    String srcLine;

    ((StreamReader)src).BaseStream.Seek(0, SeekOrigin.Begin);

    eow = eowl;
    if (eow != null) {
      while (eow != null && eow.info.line < 1) {
        PutMsg(lst, eow);
        eow = eow.next;
      } // while
      lst.WriteLine();
    } // if
    lnr = 1;
    for (; ; ) {
      if (listShape == ListingShape.shortListing) {
        if (eow == null) {
          lst.WriteLine("...");
          break;
        } // if
        skip = eow.info.line - lnr;
        if (skip > 0) {
          lst.WriteLine("...");
          lnr = eow.info.line;
          while (skip-- > 0) {
            srcLine = src.ReadLine();
            if (srcLine == null)
              break;
          } // while
        } // if
      } // if
      srcLine = src.ReadLine();
      if (srcLine == null)
        break;
      lst.WriteLine("{0,5}| {1}", lnr, srcLine);
      while (eow != null && eow.info.line == lnr) {
        PutMsg(lst, eow);
        eow = eow.next;
      } // while
      lnr++;
    } // for
    lst.WriteLine();
    while (eow != null) {
      PutMsg(lst, eow);
      eow = eow.next;
    } // while
    lst.WriteLine();
    lst.WriteLine("error(s) and warning(s):");
    lst.WriteLine("-----------------------");
    lst.WriteLine();
    lst.WriteLine("{0,5} lexical error(s) ", NumOfLexErrors());
    lst.WriteLine("{0,5} syntax error(s)  ", NumOfSynErrors());
    lst.WriteLine("{0,5} semantic error(s)", NumOfSemErrors());
    lst.WriteLine("{0,5} warning(s)       ", NumOfWarnings());
  } // GenerateListing


#if TEST_ERRORS

  public static void Main(String[] args) {
    Console.WriteLine("START: Errors");

    Console.WriteLine("installing ...");
    Utils.InstallModule("Utils",  new Utils.ModuleMethodDelegate(Utils.UtilsMethod));
    Utils.InstallModule("Sets",   new Utils.ModuleMethodDelegate(Sets.SetsMethod  ));
    Utils.InstallModule("Errors", new Utils.ModuleMethodDelegate(Errors.ErrorsMethod));
    Console.WriteLine("initModule ...");
    Utils.Modules(Utils.ModuleAction.initModule);

    LexError(1, 1, "LexError at {0}, {1}", 1, 1);
    SynError(3, 10, "SynError at {0}, {1}", 2, 10);
    SemError(5, 5, "SemError at {0}, {1}", 5, 5);
    Warning(0, 0, "Warning  at {0}, {1}", 0, 0);
    Warning(9, 0, "Warning  at {0}, {1}", 9, 0);

    String srcName;
    Utils.GetInputFileName("source file name > ", out srcName);
    FileStream   srcFs = new FileStream(srcName, FileMode.Open);
    StreamReader src   = new StreamReader(srcFs);
    FileStream   lstFs = new FileStream(srcName + ".lst", FileMode.Create);
    StreamWriter lst   = new StreamWriter(lstFs);

    lst.WriteLine("START");
    GenerateListing(src, lst, ListingShape.longListing);
    lst.WriteLine("END");

    src.Close();
    lst.Close();
    
    Console.WriteLine("resetModule ...");
    Utils.Modules(Utils.ModuleAction.resetModule);

    Console.WriteLine("cleanupModule ...");
    Utils.Modules(Utils.ModuleAction.cleanupModule);
    Console.WriteLine("END");
    // Console.WriteLine("type [CR] to continue ...");
    // Console.ReadLine();
  } // Main

#endif

} // Errors

// End of Errors.cs
//=====================================|========================================

