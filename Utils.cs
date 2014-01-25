// Utils.cs                                       HDO, 2006-08-28; SN, 10.9.2007
// --------
// Collection of useful utilities (constants, types, and methods).
//=====================================|========================================

#undef TEST_UTILS

using System;
using System.IO;

public class Utils {

  public const String MODULENAME = "Utils";

  public static void UtilsMethod(ModuleAction action, out String moduleName) {
    //-----------------------------------|----------------------------------------
    moduleName = MODULENAME;
    switch (action) {
      case ModuleAction.getModuleName:
        return;
      case ModuleAction.initModule:
        timerRuns = false;
        break;
      case ModuleAction.resetModule:
        timerRuns = false;
        break;
      case ModuleAction.cleanupModule:
        return;
    } // switch
  } // UtilsMethod

  // --- general char constants ---
  public const char EF = '\0';
  public const char LF = '\n';
  public const char NL = '\n';
  public const char HT = '\t';
  public const char VT = '\v';
  public const char BS = '\b';
  public const char CR = '\r';
  public const char FF = '\f';
  public const char BEL = '\a';

  public const int UCHAR_MAX = 255;
  public const int CHAR_BIT = 8;
  public const long INT_MAX = 2147483647;
  public const long INT_MIN = -INT_MAX;
  public const int BYTE_MAX = UCHAR_MAX;
  public const int BYTE_BIT = CHAR_BIT;

  // --- general integer constants ---
  public const int EXIT_FAILURE = -1;
  public const int EXIT_SUCCESS = 0;
  public const int KB = 1024;
  public const int MB = KB * KB;

  // --- module handling types and program module handling ---

  public enum ModuleAction {
    getModuleName,
    initModule,
    resetModule,
    cleanupModule
  } // ModuleAction

  public delegate void ModuleMethodDelegate(ModuleAction action,
                                            out String moduleName);

  private struct ModuleInfo {
    public String moduleName;
    public ModuleMethodDelegate moduleMethod;
    public bool init;
  } // ModuleInfo

  private const int MAXMODULES = 20;
  private static ModuleInfo[] mil = new ModuleInfo[MAXMODULES];
  private static int modCnt;

  public static void InstallModule(String moduleName, ModuleMethodDelegate moduleMethod) {
    //-----------------------------------|----------------------------------------
    if (moduleName == MODULENAME) {
      modCnt = 0;
      for (int i = 0; i < MAXMODULES; i++) {
        mil[i].moduleName = "";
        mil[i].moduleMethod = null;
        mil[i].init = false;
      } // for
    } // if
    if (modCnt == MAXMODULES)
      FatalError(MODULENAME, "InstallModule", "too many modules");
    String mn;
    moduleMethod(ModuleAction.getModuleName, out mn);
    if (moduleName != mn)
      FatalError(MODULENAME, "InstallModule",
                 "incorrect module name \"{0}\"", moduleName);
    mil[modCnt].moduleName = moduleName;
    mil[modCnt].moduleMethod = moduleMethod;
    mil[modCnt].init = false;
    modCnt++;
  } // InstallModule

  private static bool withinCleanUp = false;

  public static void Modules(ModuleAction action) {
    //-----------------------------------|----------------------------------------
    String dummy;
    switch (action) {
      case ModuleAction.initModule:
        for (int i = 0; i < modCnt; i++) {
          if (!mil[i].init) {
            mil[i].moduleMethod(ModuleAction.initModule, out dummy);
            mil[i].init = true;
          } else
            FatalError(MODULENAME, "Modules",
                       "{0} reinitialized", mil[i].moduleName);
        } // for
        break;
      case ModuleAction.resetModule:
      case ModuleAction.cleanupModule:
        if (!withinCleanUp) {
          withinCleanUp = true;
          for (int i = modCnt - 1; i >= 0; i--)
            if (mil[i].init) {
              mil[i].moduleMethod(action, out dummy);
              mil[i].init = action != ModuleAction.cleanupModule;
            } else
              FatalError(MODULENAME, "Modules",
                         "{0} not initialized", mil[i].moduleName);
          withinCleanUp = false;
        } // if
        break;
      default:
        FatalError(MODULENAME, "Modules",
                   "invalid ModuleAction {0}", action.ToString());
        break;
    } // switch
  } // Modules


  // --- misc. utilities ---

  public static void FatalError(String moduleName, String methodName,
                                String fmt, params Object[] p) {
    //-----------------------------------|----------------------------------------
    Console.WriteLine();
    Console.WriteLine("*** FATAL ERROR in module {0}, method {1}",
            moduleName, methodName);
    Console.WriteLine("*** " + fmt, p);
    Modules(ModuleAction.cleanupModule);
    Environment.Exit(EXIT_FAILURE);
  } // FatalError

  public static void GetInputFileName(String prompt, out String fileName) {
    //-----------------------------------|----------------------------------------
    Console.WriteLine();
    Console.Write("{0}", prompt);
    fileName = Console.ReadLine();
  } // GetInputFileName


  // --- timer utilities ---

  private static bool timerRuns = false;
  private static DateTime startedAt, stoppedAt;

  public static void StartTimer() {
    //-----------------------------------|----------------------------------------
    if (timerRuns)
      FatalError(MODULENAME, "StartTimer", "timer still running");
    timerRuns = true;
    startedAt = DateTime.Now;
  } // StartTimer

  public static void StopTimer() {
    //-----------------------------------|----------------------------------------
    if (!timerRuns)
      FatalError(MODULENAME, "StopTimer", "timer not running");
    stoppedAt = DateTime.Now;
    timerRuns = false;
  } // StopTimer

  public static TimeSpan ElapsedTime() {
    //-----------------------------------|----------------------------------------
    TimeSpan elapsed = stoppedAt - startedAt;
    return elapsed;
  } // ElapsedTime


  // --- Min/Max methods ---

  public static T Min<T>(T a, T b) where T : IComparable {
    //-----------------------------------|----------------------------------------
    return a.CompareTo(b) < 0 ? a : b;
  } // Min

  public static T Max<T>(T a, T b) where T : IComparable {
    //-----------------------------------|----------------------------------------
    return a.CompareTo(b) > 0 ? a : b;
  } // Max


  // --- shift methods ---

  public static int BSL(int x, int i) {
    //-----------------------------------|----------------------------------------
    return x << i;
  } // BSL

  public static int BSR(int x, int i) {
    //-----------------------------------|----------------------------------------
    return x >> i;
  } // BSR


  // --- bit manipulation methods ---

  public static ushort Bit(ushort i) {
    //-----------------------------------|----------------------------------------
    return (ushort)(1 << i);
  } // Bit

  public static void SetBit(ref ushort x, ushort i) {
    //-----------------------------------|----------------------------------------
    x |= Bit(i);
  } // SetBit

  public static void ClrBit(ref ushort x, ushort i) {
    //-----------------------------------|----------------------------------------
    x &= (ushort)(~Bit(i));
  } // ClrBit

  public static bool TestBit(ushort x, ushort i) {
    //-----------------------------------|----------------------------------------
    return (x & Bit(i)) != 0;
  } // TestBit

  public static int Strquotechar(ref string s, char ch) {
    // -------------------------------------|--------------------------------------
    switch (ch) {
      case EF:
        s = "EF";
        break;
      case LF:
        s = "LF";
        break;
      case HT:
        s = "HT";
        break;
      case VT:
        s = "VT";
        break;
      case BS:
        s = "BS";
        break;
      case CR:
        s = "CR";
        break;
      case FF:
        s = "FF";
        break;
      case BEL:
        s = "BEL";
        break;
      case '\\':
      case '\'':
      case '\"':
        s = "\'\\" + (char)ch + "\'";
        break;
      default:
        if (!Char.IsControl(ch))
          s = "\'" + (char)ch + "\'";
        else
          s = "\'\\" + (char)ch + "\'";
        break;
    }
    return s.Length;
  } // Strquotechar

  public static int Strquotestr(ref string s, string t) {
    // -------------------------------------|--------------------------------------
    char ch = ' ';
    string s1;
    int i = 0;

    s1 = s;
    s += '"';
    while (i < t.Length) {
      ch = t[i];
      i++;
      switch (ch) {
        case LF:
          s += '\\';
          s += 'n';
          break;
        case HT:
          s += '\\';
          s += 't';
          break;
        case VT:
          s += '\\';
          s += 'v';
          break;
        case BS:
          s += '\\';
          s += 'b';
          break;
        case CR:
          s += '\\';
          s += 'r';
          break;
        case FF:
          s += '\\';
          s += 'f';
          break;
        case BEL:
          s += '\\';
          s += 'a';
          break;
        case '\\':
        case '\'':
        case '"':
          s += '\\';
          s += ch;
          break;
        default:
          if (!Char.IsControl(ch))
            s += ch;
          else {
            s = "\\" + (char)ch;
            s += s.Length.ToString();
          }
          break;
      }
    }
    s += '"';
    return s1.Length;
  } // Strquotestr

#if TEST_UTILS

  public static void Main(String[] args) {
    Console.WriteLine("START: Utils");

    Console.WriteLine("installing ...");
    InstallModule("Utils", new ModuleMethodDelegate(UtilsMethod));
    Console.WriteLine("initModule ...");
    Modules(ModuleAction.initModule);

    // FatalError(MODULENAME, "Main", 
    //            "any {0} error {1}", "additional", "messages");

    Console.WriteLine("StartTimer ...");
    StartTimer();
    String fn;
    GetInputFileName("input file name > ", out fn);
    Console.WriteLine("input file name = " + fn);
    Console.WriteLine("StopTimer ...");
    StopTimer();
    Console.WriteLine(ElapsedTime());

    Console.WriteLine("Min(17, 4) = " + Min(17, 4));
    Console.WriteLine("Max(17, 4) = " + Max(17, 4));

    Console.WriteLine();
    Console.WriteLine("BSL(1, 2) = " + BSL(1, 2));
    Console.WriteLine("BSR(4, 2) = " + BSR(4, 2));
    Console.WriteLine("Bit(4)    = " + Bit(4));
    ushort x = 1;
    Console.WriteLine("x = " + x);
    SetBit (ref x, 2);
    Console.WriteLine("after SetBit (x, 2), x = " + x);
    Console.WriteLine("TestBit(x, 2) = " + TestBit(x, 2));
    ClrBit (ref x, 2);
    Console.WriteLine("after ClrBit (x, 2), x = " + x);
    Console.WriteLine("TestBit(x, 2) = " + TestBit(x, 2));


    Console.WriteLine("resetModule ...");
    Modules(ModuleAction.resetModule);

    Console.WriteLine("cleanupModule ...");
    Modules(ModuleAction.cleanupModule);

    Console.WriteLine("END");
    // Console.WriteLine("type [CR] to continue ...");
    // Console.ReadLine();
  } // Main

#endif

} // Utils

// End of Utils.cs
//=====================================|========================================


