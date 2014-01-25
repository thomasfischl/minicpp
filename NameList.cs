// NameList.cs                                                   HDO, 2006-08-28
// -----------
// Unique Mapping of names to spelling indices (spix).
//=====================================|========================================

#undef TEST_NAMELIST

using System;
using System.Collections;
using System.Collections.Specialized;


public class NameList {


  private static bool       caseSensitive;

  private static Hashtable  nHt;   // hash table for names: name -> spix 
  private static ArrayList  nl;    // name list  for names, index is spix


  public static void Init(bool caseSensitive) {
  //-----------------------------------|----------------------------------------
    NameList.caseSensitive = caseSensitive;
    if (caseSensitive)
      nHt = new Hashtable();
    else
      nHt = CollectionsUtil.CreateCaseInsensitiveHashtable();
    nl  = new ArrayList();
  } // Init
 
 
  public static int SpixOf(String name) {
  //-----------------------------------|----------------------------------------
    Object spix = nHt[name];
    if (spix == null) {
      if (caseSensitive)
        nl.Add(name);
      else
        nl.Add(name.ToUpper());
      spix = nl.Count - 1;
      nHt[name] = spix;
    } // if
    return (int)spix;
  } // SpixOf
  
  
  public static String NameOf(int spix) {
  //-----------------------------------|----------------------------------------
    return (String)nl[spix];
  } // NameOf


#if TEST_NAMELIST

  public static void Main(String[] args) {
    Console.WriteLine("START: NameList");

    NameList.Init(true);

    int anneSpix1 = NameList.SpixOf("Anne");
    Console.WriteLine("NameList.SpixOf(\"Anne\") " + anneSpix1);

    int patSpix = NameList.SpixOf("Pat");
    Console.WriteLine("NameList.SpixOf(\"Pat\") " + patSpix);

    int anneSpix2 = NameList.SpixOf("ANNE");
    Console.WriteLine("NameList.SpixOf(\"ANNE\") " + anneSpix2);

    Console.WriteLine("END");
    Console.WriteLine("type [CR] to continue ...");
    Console.ReadLine();
  } // Main

#endif

} // NameList

// End of NameList.cs
//=====================================|========================================

