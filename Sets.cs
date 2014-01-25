// Sets.cs                                                      HDO, 2006-08-28
// -------
// Definition of types BitSet and Set256 as well as methods for their manipulation.
//=====================================|========================================

#undef TEST_SETS

using System;
using System.Text;

public class Sets {

  public const String MODULENAME = "Sets";

  public static void SetsMethod(Utils.ModuleAction action, out String moduleName) {
    //-----------------------------------|----------------------------------------
    moduleName = MODULENAME;
    switch (action) {
      case Utils.ModuleAction.getModuleName:
        return;
      case Utils.ModuleAction.initModule:
        emptySet = new Set256();
        break;
      case Utils.ModuleAction.resetModule:
        break;
      case Utils.ModuleAction.cleanupModule:
        break;
    } // switch
  } // SetsFunc

  public const ushort BITSET_BIT = 16;
  public const ushort BITSET_MASK = 15;
  public const ushort ELEM_MAX = 256; // max. nr. of elems in a Set256
  public const ushort SET256_LEN = 16;

  public class Set256 {

    public ushort[] ushorts;

    public Set256() {
      ushorts = new ushort[SET256_LEN];
    } // Set256

    public Set256(params ushort[] paramUshorts) {
      ushorts = new ushort[SET256_LEN];
      for (int i = 0; i < paramUshorts.Length; i++)
        ushorts[i] = paramUshorts[i];
    } // Set256

    public ushort this[int i] {
      get {
        return ushorts[i];
      }
      set {
        ushorts[i] = value;
      }
    }

    public override String ToString() {
      StringBuilder sb = new StringBuilder("{");
      bool first = true;
      for (ushort i = 0; i < ELEM_MAX; i++)
        if (member(i, this)) {
          if (!first)
            sb.Append(", ");
          sb.Append(i.ToString());
          first = false;
        } // if
      sb.Append("}");
      return sb.ToString();
    } // ToString

  } // Set256

  public static Set256 emptySet; // empty set constant 

  // --- operations for BitSet arrays ---

  private static void checkVal(ushort x) {
    if ((x < 0) || (x > 255)) {
      Errors.Restriction("Sets", "setincl", "value {0} out of supported range 0..255", x);
    } // if
 } //checkVal

  // x member of s?
  public static bool member(ushort x, Set256 s) {
    //-----------------------------------|----------------------------------------
    checkVal(x);
    return Utils.TestBit(s.ushorts[x / BITSET_BIT], (ushort)(x & BITSET_MASK));
  } // member

  // include x in s 
  public static void setincl(ref Set256 s, ushort x) {
    //-----------------------------------|----------------------------------------
    checkVal(x);
    Utils.SetBit(ref s.ushorts[x / BITSET_BIT], (ushort)(x & BITSET_MASK));
  } // setincl

  // exclude x from s 
  //-----------------------------------|----------------------------------------
  public static void setexcl(ref Set256 s, ushort x) {
    checkVal(x);
    Utils.ClrBit(ref s.ushorts[x / BITSET_BIT], (ushort)(x & BITSET_MASK));
  } // setexcl


  // --- operations for type Set256 only ---

  // init s, use
  //   e or E for an <e>lement, e.g., setinit(s, "E", e)
  //   r or R for a  <r>ange,   e.g., setinit(s, "R", lb, ub) 
  public static void setinit(out Set256 s, String form, params ushort[] p) {
    //-----------------------------------|----------------------------------------
    int pi = 0;
    ushort e, lb, ub;
    s = new Set256();
    foreach (char op in form) {
      switch (op) {
        case 'E':
        case 'e':
          e = p[pi++];  // single element e
          setincl(ref s, e);
          break;
        case 'R':
        case 'r':
          lb = p[pi++]; // range lb .. ub
          ub = p[pi++];
          if (lb > ub)
            Utils.FatalError(MODULENAME, "setinit", "lb > ub");
          e = lb;
          while (e <= ub) {
            setincl(ref s, e);
            e++;
          } // while
          break;
        default:
          Utils.FatalError(MODULENAME, "setinit",
                           "unknown operation \'{0}\'", op.ToString());
          break;
      } // switch
    } // while
  } // setinit


  // s1 <= s2? 
  public static bool setle(Set256 s1, Set256 s2) {
    //-----------------------------------|----------------------------------------
    int i = 0;
    while (i < SET256_LEN) {
      if ((s1.ushorts[i] & ~(s2.ushorts[i])) != 0)
        return false;
      i++;
    } // while 
    return true;
  } // setle


  // sets s1 and s2 disjoint?
  public static bool setdisjoint(Set256 s1, Set256 s2) {
    //-----------------------------------|----------------------------------------
    int i = 0;
    while (i < SET256_LEN) {
      if ((s1.ushorts[i] & s2.ushorts[i]) != 0)
        return false;
      i++;
    } // while
    return true;
  } // setdisjoint


  // s1 = s1 inters s2 
  public static void setinters(ref Set256 s1, Set256 s2) {
    //-----------------------------------|----------------------------------------
    int i = 0;
    while (i < SET256_LEN) {
      s1.ushorts[i] &= s2.ushorts[i];
      i++;
    } // while
  } // setinters


  // s1 = s1 union s2
  public static void setunion(ref Set256 s1, Set256 s2) {
    //-----------------------------------|----------------------------------------
    int i = 0;
    while (i < SET256_LEN) {
      s1.ushorts[i] |= s2.ushorts[i];
      i++;
    } // while
  } // setunion

  // s1 = s1 - s2 
  public static void setdiff(ref Set256 s1, Set256 s2) {
    //-----------------------------------|----------------------------------------
    int i = 0;
    while (i < SET256_LEN) {
      s1.ushorts[i] &= (ushort)(~(s2.ushorts[i]));
      i++;
    } // while
  } // setdiff


  // clear s, s = {}
  public static void setclear(ref Set256 s) {
    //-----------------------------------|----------------------------------------
    for (int i = 0; i < SET256_LEN; i++)
      s.ushorts[i] = 0;
  } // setclear

  // set assignment, s1 = s2
  public static void setcpy(ref Set256 s1, Set256 s2) {
    //-----------------------------------|----------------------------------------
    for (int i = 0; i < SET256_LEN; i++)
      s1.ushorts[i] = s2.ushorts[i];
  } // setcpy

  // set comparison, s1 == s2? 
  public static bool seteq(Set256 s1, Set256 s2) {
    //-----------------------------------|----------------------------------------
    for (int i = 0; i < SET256_LEN; i++)
      if (s1.ushorts[i] != s2.ushorts[i])
        return false;
    return true;
  } // seteq

  // s empty?
  public static bool setempty(Set256 s) {
    //-----------------------------------|----------------------------------------
    for (int i = 0; i < SET256_LEN; i++)
      if (s.ushorts[i] != 0)
        return false;
    return true;
  } // setempty


#if TEST_SETS

  public static void Main(String[] args) {
    Console.WriteLine("START: Sets");

    Console.WriteLine("installing ...");
    Utils.InstallModule("Utils", new Utils.ModuleMethodDelegate(Utils.UtilsMethod));
    Utils.InstallModule("Sets",  new Utils.ModuleMethodDelegate(Sets.SetsMethod ));
    Console.WriteLine("initModule ...");
    Utils.Modules(Utils.ModuleAction.initModule);

    Console.WriteLine("emptySet = " + emptySet);

    Set256 s = new Set256();
    Console.WriteLine("s = " + s);
    setincl(ref s, 177);
    setincl(ref s, 4);
    Console.WriteLine("after setincl(ref s, 177) and setincl(ref s, 4), s = " + s);
    setexcl(ref s, 177);
    setexcl(ref s, 4);
    Console.WriteLine("after setexcl(ref s, 177) and setexcl(ref s, 4), s = " + s);

    setinit(out s, "E", 4);
    Console.WriteLine("after setinit(out s, \"E\", 4), s = " + s);
    setinit(out s, "R", 4, 177);
    Console.WriteLine("after setinit(out s, \"R\", 4, 177), s = " + s);

    Set256 a, b;
    setinit(out a, "E", 4);
    setinit(out b, "E", 5);
    Console.WriteLine("setle(a, b) = " + setle(a, b));

    setinit(out a, "R",   0,   9);
    setinit(out b, "R",   9,  100);
    Console.WriteLine("setdisjoint(a, b) = " + setdisjoint(a, b));

    setinit(out a, "R", 0, 99);
    setinit(out b, "R", 80, 100);
    setinters(ref a, b);
    Console.WriteLine("setinters(ref a, b) = " + a);

    setinit(out a, "R", 0, 9);
    setinit(out b, "R", 100, 109);
    setunion(ref a, b);
    Console.WriteLine("setunion(ref a, b) = " + a);

    setinit(out a, "R", 0, 99);
    setinit(out b, "R", 1, 98);
    setdiff(ref a, b);
    Console.WriteLine("setdiff(ref a, b) = " + a);

    setclear(ref a);
    Console.WriteLine("setclear(a) = " + a);

    setinit(out b, "R", 0, 9);
    setcpy(ref a, b);
    Console.WriteLine("setcpy(a) = " + a);

    Console.WriteLine("seteq(a, b) = " + seteq(a, b));

    Console.WriteLine("setempty(a) = " + setempty(a));
    Console.WriteLine("setempty(emptySet) = " + setempty(emptySet));

    Console.WriteLine("resetModule ...");
    Utils.Modules(Utils.ModuleAction.resetModule);

    Console.WriteLine("cleanupModule ...");
    Utils.Modules(Utils.ModuleAction.cleanupModule);
    Console.WriteLine("END");
    // Console.WriteLine("type [CR] to continue ...");
    // Console.ReadLine();
  } // Main

#endif

} // Sets

// End of Sets.cs
//=====================================|========================================
