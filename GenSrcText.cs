// GenSrcText.cs:                                     HDO, 1998-2005, 2006-08-28
// -------------
//
// Generate symbols as text from symbol table and full source text from AST.
//=====================================|========================================

#undef TEST_GENSRCTEXT

using System;
using System.Collections;
using System.IO;
using System.Text;


public class GenSrcText {

  public static StreamWriter genMcpp = null;

  public static int indentLevel = 1;

  public static void IncIndent() {
    indentLevel++;
  } // Indent

  public static void DecIndent() {
    indentLevel--;
  } // Outdent

  public static String Indent() {
    StringBuilder sb = new StringBuilder();
    for (int i = 1; i <= indentLevel; i++)
      sb.Append("  ");
    return sb.ToString();
  } // Margin

  
  // === generate dump of symbol table ===

  private static void DumpSymbols(Symbol symbols) {
    Symbol sy = symbols;
    while (sy != null) {
      genMcpp.WriteLine(Indent() + sy);
      if (sy.kind == Symbol.Kind.funcKind) {
        IncIndent();
        DumpSymbols(sy.symbols);
        DecIndent();
      } // if
      sy = sy.next;
    } // while
  } // DumpSymbols

  public static void DumpSymTab() {
  //-----------------------------------|----------------------------------------
    genMcpp.WriteLine("/*Dump of Symbol Table:");
    genMcpp.WriteLine("  --------------------");
    Symbol.shortSymbolInfo = false;
    indentLevel = 1;
    DumpSymbols(SymTab.CurSymbols());
    Symbol.shortSymbolInfo = true;
    genMcpp.WriteLine("*/");
  } // DumpSymTab


  // === generate source text from symbol table and AST ===

  public static void WriteStat(Stat stat) {
    switch (stat.kind) {
      case Stat.Kind.emptyStatKind:
        genMcpp.WriteLine(Indent() + ";");
        break;
      case Stat.Kind.blockStatKind:
        BlockStat b_s = (BlockStat)stat;
        genMcpp.WriteLine(Indent() + "{");
        IncIndent();
        WriteStatList(b_s.statList);
        DecIndent();
        genMcpp.WriteLine(Indent() + "}");
        break;
      case Stat.Kind.incStatKind:
        IncStat i_s = (IncStat)stat;
        genMcpp.WriteLine(Indent() + i_s.vo.sy + "++;");
        break;
      case Stat.Kind.decStatKind:
        DecStat d_s = (DecStat)stat;
        genMcpp.WriteLine(Indent() + d_s.vo.sy + "--;");
        break;
      case Stat.Kind.assignStatKind:
        AssignStat a_s = (AssignStat)stat;
        genMcpp.WriteLine(Indent() + a_s.lhs + " = " + a_s.rhs + ";");
        break;
      case Stat.Kind.callStatKind:
        CallStat c_s = (CallStat)stat;
        genMcpp.WriteLine(Indent() + c_s.func + "(" + c_s.apl + ");");
        break;
      case Stat.Kind.ifStatKind:
        IfStat if_s = (IfStat)stat;
        genMcpp.WriteLine(Indent() + "if (" + if_s.cond + ")");
        IncIndent();
        WriteStatList(if_s.thenStat);
        DecIndent();
        if (if_s.elseStat != null) {
          genMcpp.WriteLine(Indent() + "else ");
          IncIndent();
          WriteStatList(if_s.elseStat);
          DecIndent();
        } // if
        break;
      case Stat.Kind.whileStatKind:
        WhileStat w_s = (WhileStat)stat;
        genMcpp.WriteLine(Indent() + "while (" + w_s.cond + ")");
        IncIndent();
        WriteStatList(w_s.body);
        DecIndent();
        break;
      case Stat.Kind.switchStatKind:
        SwitchStat ss = (SwitchStat)stat;
        genMcpp.WriteLine(Indent() + "switch (" + ss.e + ") {");
        IncIndent();
        WriteStatList(ss.caseStat);
        if (ss.defaultStat != null)
        {
          genMcpp.WriteLine(Indent() + "default:");
          IncIndent();
          WriteStatList(ss.defaultStat);
          DecIndent();
        }
        DecIndent();
        genMcpp.WriteLine(Indent() + "}");
        break;
      case Stat.Kind.caseStatKind:
        CaseStat cs = (CaseStat)stat;
        genMcpp.WriteLine(Indent() + "case " + cs.val + ":" );
        IncIndent();
        WriteStatList(cs.stat);
        DecIndent();
        break;
      case Stat.Kind.breakStatKind:
        genMcpp.WriteLine(Indent() + "break;");
        break;
      case Stat.Kind.inputStatKind:
        InputStat in_s = (InputStat)stat;
        genMcpp.WriteLine(Indent() + "cin >> " + in_s.vo.sy + ";");
        break;
      case Stat.Kind.outputStatKind:
        OutputStat out_s = (OutputStat)stat;
        genMcpp.Write(Indent() + "cout");
        foreach (Object o in out_s.values) {
          genMcpp.Write(" << ");
          if (o is Expr) {
            genMcpp.Write(o);
          } else if (o is String) {
            String s = o as String;
            if (s == "\n")
              genMcpp.Write("endl");
            else
              genMcpp.Write('"' + s + '"');
          } else 
            throw new Exception("invalid value");
        } // foreach
        genMcpp.WriteLine(";");
        break;
      case Stat.Kind.deleteStatKind:
        DeleteStat del_s = (DeleteStat)stat;
        genMcpp.WriteLine(Indent() + "delete[] " + 
                          NameList.NameOf(del_s.vo.sy.spix) + ";");
        break;
      case Stat.Kind.returnStatKind:
        ReturnStat r_s = (ReturnStat)stat;
        genMcpp.Write(Indent() + "return");
        if (r_s.e != null)
          genMcpp.Write(" " + r_s.e);
        genMcpp.WriteLine(";");
        break;
      default:
        throw new Exception("invalid statement kind");
    } // switch
  } // WriteStatList

  public static void WriteStatList(Stat statList) {
    Stat stat = statList;
    while (stat != null) {
      WriteStat(stat);
      stat = stat.next;
    } // while
  } // WriteStatList

  public static void WriteSymbolList(Symbol syList) {
    Symbol sy = syList;
    while (sy != null) {
       WriteSymbol(sy);
      sy = sy.next;
    } // while
  } // WriteSymbolList
  
  public static void WriteSymbol(Symbol sy) {
    switch (sy.kind) {
      case Symbol.Kind.undefKind:
        genMcpp.WriteLine(Indent() + "undefined " + NameList.NameOf(sy.spix) + ";");
        break;
      case Symbol.Kind.constKind:
        genMcpp.WriteLine(Indent() + "const " + sy.type + " " + NameList.NameOf(sy.spix) +
                          " = " + sy.val + ";");
        break;
      case Symbol.Kind.parKind:
        break; // nothing to do
      case Symbol.Kind.varKind:
        genMcpp.Write(Indent() + sy.type + " " + NameList.NameOf(sy.spix));
        if (sy.init)
          genMcpp.Write(" = " + sy.val);
        genMcpp.WriteLine(";");
        break;
      case Symbol.Kind.funcKind:
        WriteFuncDecl(sy);
        genMcpp.WriteLine(" {");
        IncIndent();
        Symbol localSy = sy.symbols;
        WriteSymbolList(localSy);
        WriteStatList(sy.statList);
        DecIndent();
        genMcpp.WriteLine(Indent() + "} // " + NameList.NameOf(sy.spix));
        break;
      default:
        throw new Exception("invalid symbol kind");
    } // case
  } // WriteSymbol

  public static void WriteGlobals(Symbol syList) {
    Symbol sy = syList;
    while (sy != null) {
      if (sy.kind == Symbol.Kind.constKind || sy.kind == Symbol.Kind.varKind) {
        WriteSymbol(sy);
      } // if
      sy = sy.next;
    } // while
  } //WriteFuncDecls

  public static void WriteFuncDecl(Symbol sy) {
    genMcpp.WriteLine();
    genMcpp.Write(Indent() + sy.type + " " + NameList.NameOf(sy.spix) + "(");
    Symbol localSy = sy.symbols;
    bool first = true;
    while (localSy != null && localSy.kind == Symbol.Kind.parKind) {
      if (!first)
        genMcpp.Write(", ");
      first = false;
      genMcpp.Write(localSy.type + " " + NameList.NameOf(localSy.spix));
      localSy = localSy.next;
    } // while
    genMcpp.Write(")");
  } // WriteFuncDecl

  public static void WriteFuncDecls(Symbol syList) {
    Symbol sy = syList;
    while (sy != null) {
      if (sy.kind == Symbol.Kind.funcKind) {
        WriteFuncDecl(sy);
        genMcpp.Write(";");
      } // if
      sy = sy.next;
    } // while
  } //WriteFuncDecls

  public static void WriteFuncDefs(Symbol syList) {
    Symbol sy = syList;
    while (sy != null) {
      if (sy.kind == Symbol.Kind.funcKind)
        WriteSymbol(sy);
      sy = sy.next;
    } // while
  } // WriteFuncDecfs

  public static void WriteProgramSource() {
  //-----------------------------------|----------------------------------------
    genMcpp.WriteLine();
    genMcpp.WriteLine("/*Program Source (from AST):");
    genMcpp.WriteLine("  -------------------------*/");
    genMcpp.WriteLine();
    indentLevel = 1;
    WriteGlobals(SymTab.CurSymbols());   // global contants and variables
    WriteFuncDecls(SymTab.CurSymbols()); // necessary to allow indirect recursive calls
    genMcpp.WriteLine();
    WriteFuncDefs(SymTab.CurSymbols());
    genMcpp.WriteLine();
  } // WriteProgramSource

  public static void DumpSymTabAndWriteSrcTxt(String path, String moduleName) {
//-----------------------------------|----------------------------------------
    String genMcppFileName = path + moduleName + "_Gen.mcpp";
    if (File.Exists(genMcppFileName))
      File.Delete(genMcppFileName);
    Console.WriteLine("emitting  mcpp to \"" + genMcppFileName + "\" ...");
    FileStream genMcppFs = new FileStream(genMcppFileName, FileMode.Create);
    genMcpp = new StreamWriter(genMcppFs);
    GenSrcText.DumpSymTab();
    GenSrcText.WriteProgramSource();
    genMcpp.Close();
  } // DumpSymTabAndWriteSrcTxt

#if TEST_GENSRCTEXT

  public static void Main(String[] args) {
    genMcpp.WriteLine("START: GenSrcText");

    genMcpp.WriteLine("END");

    genMcpp.WriteLine();
    genMcpp.WriteLine("type [CR] to continue ...");
    genMcpp.ReadLine();
  } // main

#endif

} // GenSrcText


// End of GenSrcText.cs
//=====================================|========================================