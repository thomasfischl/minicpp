// GenCilAsText.cs:                                   HDO, 1998-2005, 2006-08-28
// ---------------
// Generate CIL in form of text to be assembled later on.
//=====================================|========================================

#define GENCILASTEXT

#if GENCILASTEXT

#undef TEST_GENCILASTEXT

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;


public static class GenCilAsText
{

  private static String MODULE;
  private static Stack loopEndLabels;


  // --- provide lables ---

  private static int nextLabNr;

  private static String NewLabel()
  {
    String label = "L" + nextLabNr;
    nextLabNr++;
    return label;
  } // NewLabel


  // === generate CIL for declarations ===

  private static void GenGlobConsts(StringBuilder sb)
  {
    Symbol sy = SymTab.CurSymbols();
    while (sy != null)
    {
      if (sy.kind == Symbol.Kind.constKind)
      {
        sb.Append("  .field public static literal ");
        GenType(sb, sy.type);
        sb.Append(NameList.NameOf(sy.spix) + " = ");
        if (sy.type == Type.boolType)
        {
          sb.Append("bool(");
          if (sy.val == 0)
            sb.Append("false)\n");
          else // sy.val != 0
            sb.Append("true)\n");
        }
        else
        { // sy.type == intType
          sb.Append("int32(" + sy.val + ")\n");
        } // else
      } // if
      sy = sy.next;
    } // while
  } // GenGlobConsts

  private static void GenGlobVars(StringBuilder sb)
  {
    Symbol sy = SymTab.CurSymbols();
    while (sy != null)
    {
      if (sy.kind == Symbol.Kind.varKind)
      {
        sb.Append("  .field public static ");
        GenType(sb, sy.type);
        sb.Append(NameList.NameOf(sy.spix) + "\n");
      } // if
      sy = sy.next;
    } // while
  } // GenGlobVars

  private static void GenLocVars(StringBuilder sb, Symbol symbols)
  {
    Symbol sy = symbols;
    // skip parameters
    while (sy != null && sy.kind == Symbol.Kind.parKind)
    {
      sy = sy.next;
    } // while
    // generate local declarations for constants and variables
    Symbol firstConstOrVarSy = sy;
    int i = 0;
    while (sy != null)
    {
      if (sy.kind == Symbol.Kind.constKind ||
          sy.kind == Symbol.Kind.varKind)
      {
        if (i == 0)
          sb.Append("    .locals init (\n      ");
        else
          sb.Append(",\n      ");
        sb.Append("[" + i + "] ");
        GenType(sb, sy.type);
        sb.Append(NameList.NameOf(sy.spix));
        if (sy.next == null)
          sb.Append(")\n");
        i++;
      } // if
      sy = sy.next;
    } // while
    // generate initializations for constants
    sy = firstConstOrVarSy;
    while (sy != null)
    {
      if (sy.kind == Symbol.Kind.constKind ||
          sy.kind == Symbol.Kind.varKind && sy.init)
      {
        if (sy.type.IsPtrType() && sy.val == 0)
          sb.Append("    ldnull\n");
        else
          sb.Append("    ldc.i4 " + sy.val + "\n");
        sb.Append("    stloc  " + sy.addr + "\n");
      } // if
      sy = sy.next;
    } // while
  } // GenLocVars

  private static void GenType(StringBuilder sb, Type t)
  {
    switch (t.kind)
    {
      case Type.Kind.voidKind:
        sb.Append("void ");
        break;
      case Type.Kind.boolKind:
        sb.Append("bool ");
        break;
      case Type.Kind.boolPtrKind:
        sb.Append("bool[] ");
        break;
      case Type.Kind.intKind:
        sb.Append("int32 ");
        break;
      case Type.Kind.intPtrKind:
        sb.Append("int32[] ");
        break;
      default:
        throw new Exception("invalid type kind");
    } // switch
  } // GenType

  private static void GenGlobFuncs(StringBuilder sb)
  {
    Symbol sy = SymTab.CurSymbols();
    while (sy != null)
    {
      if (sy.kind == Symbol.Kind.funcKind && NameList.NameOf(sy.spix) != "main")
      {
        sb.Append("  .method public hidebysig static ");
        GenType(sb, sy.type);
        sb.Append(NameList.NameOf(sy.spix) + "(");
        Symbol parSy = sy.symbols;
        bool first = true;
        while (parSy != null && parSy.kind == Symbol.Kind.parKind)
        {
          if (first)
            sb.Append("\n      ");
          else
            sb.Append(",\n      ");
          first = false;
          GenType(sb, parSy.type);
          sb.Append(NameList.NameOf(parSy.spix));
          parSy = parSy.next;
        } // while
        sb.Append(") cil managed {\n");
        sb.Append("    .maxstack 100\n");
        GenLocVars(sb, sy.symbols);
        GenFuncBody(sb, sy);
        sb.Append("  } // .method\n\n");
      } // if
      sy = sy.next;
    } // while
  } // GenGlobFuncs


  // === generate CIL for expresssions ===

  private static void GenLoadConstOperand(StringBuilder sb, LitOperand lo)
  {
    if (lo.type.kind == Type.Kind.boolKind ||
        lo.type.kind == Type.Kind.intKind)
      sb.Append("    ldc.i4 " + lo.val + "\n");
    else if (lo.type.kind == Type.Kind.voidPtrKind &&
             lo.val == 0)
      sb.Append("    ldnull\n");
    else
      throw new Exception("invalid const operand type");
  } // GenLoadConstOperand

  private static void GenLoadVarOperand(StringBuilder sb, VarOperand vo)
  {
    switch (vo.sy.kind)
    {
      case Symbol.Kind.constKind:
        sb.Append("    ldc.i4 " + vo.sy.val + "\n");
        break;
      case Symbol.Kind.varKind:
        if (vo.sy.level == 0)
        { // global scope
          sb.Append("    ldsfld ");
          GenType(sb, vo.sy.type);
          sb.Append(MODULE + "::" + NameList.NameOf(vo.sy.spix) + "\n");
        }
        else if (vo.sy.level == 1) // function scope
          sb.Append("    ldloc " + vo.sy.addr + "\n");
        else
          throw new Exception("invalid operand scope level");
        break;
      case Symbol.Kind.parKind:
        sb.Append("    ldarg " + vo.sy.addr + "\n");
        break;
      default:
        throw new Exception("invalid operand kind");
    } // switch
  } // GenLoadVarOperand

  private static void GenStoreVarOperand(StringBuilder sb, VarOperand vo)
  {
    switch (vo.sy.kind)
    {
      case Symbol.Kind.varKind:
        if (vo.sy.level == 0)
        { // global scope
          sb.Append("    stsfld ");
          GenType(sb, vo.sy.type);
          sb.Append(MODULE + "::" + NameList.NameOf(vo.sy.spix) + "\n");
        }
        else if (vo.sy.level == 1) // function scope
          sb.Append("    stloc " + vo.sy.addr + "\n");
        else
          throw new Exception("invalid operand scope level");
        break;
      case Symbol.Kind.parKind:
        sb.Append("    starg " + vo.sy.addr + "\n");
        break;
      default:
        throw new Exception("invalid operand kind");
    } // switch
  } // GenStoreVarOperand

  private static void GenUnaryOperator(StringBuilder sb, UnaryOperator uo)
  {
    GenExpr(sb, uo.e);
    switch (uo.op)
    {
      case UnaryOperator.Operation.notOp:
        sb.Append("    ldc.i4 1\n");
        sb.Append("    xor\n");
        break;
      case UnaryOperator.Operation.posOp:
        sb.Append("    nop // posOp\n");
        break;
      case UnaryOperator.Operation.negOp:
        sb.Append("    neg\n");
        break;
      default:
        throw new Exception("invalid unary operator");
    } // switch
  } // GenUnaryOperator

  private static void GenBinaryOperator(StringBuilder sb, BinaryOperator bo)
  {
    GenExpr(sb, bo.left);
    if (bo.op == BinaryOperator.Operation.orOp ||
        bo.op == BinaryOperator.Operation.andOp)
    {
      String label1 = NewLabel();
      if (bo.op == BinaryOperator.Operation.orOp)
        sb.Append("    brtrue " + label1 + "\n");
      else // bo.op == BinaryOperator.Operation.andOp
        sb.Append("    brfalse " + label1 + "\n");
      GenExpr(sb, bo.right);
      String label2 = NewLabel();
      sb.Append("    br " + label2 + "\n");
      sb.Append(label1 + ":\n");
      if (bo.op == BinaryOperator.Operation.orOp)
        sb.Append("    ldc.i4 1\n");
      else // bo.op == BinaryOperator.Operation.andOp
        sb.Append("    ldc.i4 0\n");
      sb.Append(label2 + ":\n");
    }
    else
    {
      GenExpr(sb, bo.right);
      switch (bo.op)
      {
        case BinaryOperator.Operation.orOp:
          sb.Append("    // orOp\n");
          break;
        case BinaryOperator.Operation.andOp:
          sb.Append("    // andOp\n");
          break;
        case BinaryOperator.Operation.eqOp:
          sb.Append("    ceq\n");
          break;
        case BinaryOperator.Operation.neOp:
          sb.Append("    ceq\n");
          sb.Append("    ldc.i4 1\n");
          sb.Append("    xor\n");
          break;
        case BinaryOperator.Operation.ltOp:
          sb.Append("    clt\n");
          break;
        case BinaryOperator.Operation.leOp:
          sb.Append("    cgt\n");
          sb.Append("    ldc.i4 1\n");
          sb.Append("    xor\n");
          break;
        case BinaryOperator.Operation.gtOp:
          sb.Append("    cgt\n");
          break;
        case BinaryOperator.Operation.geOp:
          sb.Append("    clt\n");
          sb.Append("    ldc.i4 1\n");
          sb.Append("    xor\n");
          break;
        case BinaryOperator.Operation.addOp:
          sb.Append("    add\n");
          break;
        case BinaryOperator.Operation.subOp:
          sb.Append("    sub\n");
          break;
        case BinaryOperator.Operation.mulOp:
          sb.Append("    mul\n");
          break;
        case BinaryOperator.Operation.divOp:
          sb.Append("    div\n");
          break;
        case BinaryOperator.Operation.modOp:
          sb.Append("    rem\n");
          break;
        default:
          throw new Exception("invalid binary operator");
      } // switch
    } // else
  } // GenBinaryOperator

  private static void GenArrIdxOperator(StringBuilder sb, ArrIdxOperator aio)
  {
    GenLoadVarOperand(sb, aio.arr);
    GenExpr(sb, aio.idx);
    switch (aio.type.kind)
    {
      case Type.Kind.boolKind:
        sb.Append("    ldelem.i1\n");
        break;
      case Type.Kind.intKind:
        sb.Append("    ldelem.i4\n");
        break;
      default:
        throw new Exception("invalid type kind");
    } // switch
  } // GenArrIdxOperator

  private static void GenCall(StringBuilder sb, Symbol func, Expr apl)
  {
    Expr ap = apl;
    while (ap != null)
    {
      GenExpr(sb, ap);
      ap = ap.next;
    } // while
    sb.Append("    call ");
    GenType(sb, func.type);
    sb.Append(MODULE + "::" + NameList.NameOf(func.spix) + "(");
    Symbol fp = func.symbols;
    bool first = true;
    while (fp != null && fp.kind == Symbol.Kind.parKind)
    {
      if (first)
        sb.Append("\n        ");
      else
        sb.Append(",\n        ");
      first = false;
      GenType(sb, fp.type);
      fp = fp.next;
    } // while
    sb.Append(")\n");
  } // GenCall

  private static void GenFuncCallOperator(StringBuilder sb, FuncCallOperator fco)
  {
    GenCall(sb, fco.func, fco.apl);
  } // GenFuncCallOperator

  private static void GenNewOperator(StringBuilder sb, NewOperator no)
  {
    GenExpr(sb, no.noe);
    switch (no.elemType.kind)
    {
      case Type.Kind.boolKind:
        sb.Append("    newarr [mscorlib]System.Boolean\n");
        break;
      case Type.Kind.intKind:
        sb.Append("    newarr [mscorlib]System.Int32\n");
        break;
      default:
        throw new Exception("invalid type kind");
    } // switch
  } // GenNewOperator

  private static void GenExpr(StringBuilder sb, Expr e)
  {
    switch (e.kind)
    {
      case Expr.Kind.litOperandKind:
        GenLoadConstOperand(sb, (LitOperand)e);
        break;
      case Expr.Kind.varOperandKind:
        GenLoadVarOperand(sb, (VarOperand)e);
        break;
      case Expr.Kind.unaryOperatorKind:
        GenUnaryOperator(sb, (UnaryOperator)e);
        break;
      case Expr.Kind.binaryOperatorKind:
        GenBinaryOperator(sb, (BinaryOperator)e);
        break;
      case Expr.Kind.arrIdxOperatorKind:
        GenArrIdxOperator(sb, (ArrIdxOperator)e);
        break;
      case Expr.Kind.funcCallOperatorKind:
        GenFuncCallOperator(sb, (FuncCallOperator)e);
        break;
      case Expr.Kind.newOperatorKind:
        GenNewOperator(sb, (NewOperator)e);
        break;
      default:
        throw new Exception("invalid expression kind");
    } // switch
  } // GenExpr

  private static void GenLoadAddrOrVarOperand(StringBuilder sb, VarOperand vo)
  {
    switch (vo.sy.kind)
    {
      case Symbol.Kind.varKind:
        if (vo.sy.level == 0)
        { // global scope
          sb.Append("    ldsflda ");
          GenType(sb, vo.sy.type);
          sb.Append(MODULE + "::" + NameList.NameOf(vo.sy.spix) + "\n");
        }
        else if (vo.sy.level == 1) // function scope
          sb.Append("    ldloca " + vo.sy.addr + "\n");
        else
          throw new Exception("invalid operand scope level");
        break;
      case Symbol.Kind.parKind:
        sb.Append("    ldarga " + vo.sy.addr + "\n");
        break;
      default:
        throw new Exception("invalid operand kind");
    } // switch
  } // GenLoadAddrOrVarOperand


  // === generate CIL for statements ===

  private static void GenBlockStat(StringBuilder sb, BlockStat s)
  {
    GenStatList(sb, s.statList);
  } // GenBlockStat

  private static void GenIncStat(StringBuilder sb, IncStat s)
  {
    GenLoadVarOperand(sb, s.vo);
    sb.Append("    ldc.i4 1\n");
    sb.Append("    add\n");
    GenStoreVarOperand(sb, s.vo);
  } // GenIncStat

  private static void GenDecStat(StringBuilder sb, DecStat s)
  {
    GenLoadVarOperand(sb, s.vo);
    sb.Append("    ldc.i4 1\n");
    sb.Append("    sub\n");
    GenStoreVarOperand(sb, s.vo);
  } // GenDecStat

  private static void GenAssignStat(StringBuilder sb, AssignStat s)
  {
    switch (s.lhs.kind)
    {
      case Expr.Kind.varOperandKind:
        GenExpr(sb, s.rhs);
        GenStoreVarOperand(sb, (VarOperand)s.lhs);
        break;
      case Expr.Kind.arrIdxOperatorKind:
        ArrIdxOperator aio = (ArrIdxOperator)s.lhs;
        GenExpr(sb, aio.arr);
        GenExpr(sb, aio.idx);
        GenExpr(sb, s.rhs);
        switch (aio.type.kind)
        {
          case Type.Kind.boolKind:
            sb.Append("    stelem.i1\n");
            break;
          case Type.Kind.intKind:
            sb.Append("    stelem.i4\n");
            break;
          default:
            throw new Exception("invalid type kind");
        } // switch
        break;
      default:
        throw new Exception("invalid lhs kind");
    } // switch
  } // GenAssignStat

  private static void GenCallStat(StringBuilder sb, CallStat s)
  {
    GenCall(sb, s.func, s.apl);
    if (s.func.type.kind != Type.Kind.voidKind)
      sb.Append("    pop\n");
  } // GenCallStat

  private static void GenIfStat(StringBuilder sb, IfStat s)
  {
    GenExpr(sb, s.cond);
    String label1 = NewLabel();
    sb.Append("    brfalse " + label1 + "\n");
    GenStatList(sb, s.thenStat);
    if (s.elseStat != null)
    {
      String label2 = NewLabel();
      sb.Append("    br " + label2 + "\n");
      sb.Append(label1 + ":\n");
      label1 = label2;
      GenStatList(sb, s.elseStat);
    } // if
    sb.Append(label1 + ":\n");
  } // GenIfStat

  private static void GenWhileStat(StringBuilder sb, WhileStat s)
  {
    String label1 = NewLabel();
    sb.Append(label1 + ":\n");
    GenExpr(sb, s.cond);
    String label2 = NewLabel();
    sb.Append("    brfalse " + label2 + "\n");
    loopEndLabels.Push(label2);
    GenStatList(sb, s.body);
    loopEndLabels.Pop();
    sb.Append("    br " + label1 + "\n");
    sb.Append(label2 + ":\n");
  } // GenWhileStat

  private static void GenBreakStat(StringBuilder sb)
  {
    if (loopEndLabels.Count <= 0)
      throw new Exception("break with no loop around");
    String label = (String)loopEndLabels.Peek();
    sb.Append("    br " + label + "\n");
  } // GenBreakStat

  private static void GenInputStat(StringBuilder sb, InputStat s)
  {
    GenLoadAddrOrVarOperand(sb, s.vo);
    switch (s.vo.sy.type.kind)
    {
      case Type.Kind.boolKind:
        sb.Append("    call void BasicIO::ReadFromCin(bool&)\n");
        break;
      case Type.Kind.intKind:
        sb.Append("    call void BasicIO::ReadFromCin(int32&)\n");
        break;
      default:
        throw new Exception("invalid type");
    } // switch
  } // GenInputStat

  private static void GenOutputStat(StringBuilder sb, OutputStat s)
  {
    foreach (Object o in s.values)
    {
      if (o is Expr)
      {
        Expr e = o as Expr;
        GenExpr(sb, e);
        sb.Append("    call void BasicIO::WriteToCout(");
        GenType(sb, e.type);
        sb.Append(")\n");
      }
      else if (o is String)
      {
        String str = o as String;
        if (str == "\n")
        {
          sb.Append("    call void BasicIO::WriteEndlToCout()\n");
        }
        else
        {
          sb.Append("    ldstr \"" + str + "\"\n");
          sb.Append("    call void BasicIO::WriteToCout(string)\n");
        } // else
      }
      else
        throw new Exception("invalid value");
    } // foreach
  } // GenOutputStat

  private static void GenDeleteStat(StringBuilder sb, DeleteStat s)
  {
    sb.Append("    ldnull\n");
    GenStoreVarOperand(sb, s.vo);
  } // GenDeleteStat

  private static void GenReturnStat(StringBuilder sb, ReturnStat s)
  {
    if (s.e != null)
      GenExpr(sb, s.e);
    sb.Append("    ret\n");
  } // GenReturnStat

  private static void GenStatList(StringBuilder sb, Stat statList)
  {
    Stat stat = statList;
    while (stat != null)
    {
      switch (stat.kind)
      {
        case Stat.Kind.emptyStatKind:
          sb.Append("    nop\n");
          break;
        case Stat.Kind.blockStatKind:
          GenBlockStat(sb, (BlockStat)stat);
          break;
        case Stat.Kind.incStatKind:
          GenIncStat(sb, (IncStat)stat);
          break;
        case Stat.Kind.decStatKind:
          GenDecStat(sb, (DecStat)stat);
          break;
        case Stat.Kind.assignStatKind:
          GenAssignStat(sb, (AssignStat)stat);
          break;
        case Stat.Kind.callStatKind:
          GenCallStat(sb, (CallStat)stat);
          break;
        case Stat.Kind.ifStatKind:
          GenIfStat(sb, (IfStat)stat);
          break;
        case Stat.Kind.whileStatKind:
          GenWhileStat(sb, (WhileStat)stat);
          break;
        case Stat.Kind.breakStatKind:
          GenBreakStat(sb);
          break;
        case Stat.Kind.inputStatKind:
          GenInputStat(sb, (InputStat)stat);
          break;
        case Stat.Kind.outputStatKind:
          GenOutputStat(sb, (OutputStat)stat);
          break;
        case Stat.Kind.returnStatKind:
          GenReturnStat(sb, (ReturnStat)stat);
          break;
        case Stat.Kind.deleteStatKind:
          GenDeleteStat(sb, (DeleteStat)stat);
          break;
        default:
          throw new Exception("invalid statement kind");
      } // switch
      stat = stat.next;
    } // while
  } //GenStatList

  private static void GenFuncBody(StringBuilder sb, Symbol funcSy)
  {
    nextLabNr = 0;
    GenStatList(sb, funcSy.statList);
    switch (funcSy.type.kind)
    {
      case Type.Kind.voidKind:
        sb.Append("    ret\n");
        break;
      case Type.Kind.boolKind:
      case Type.Kind.intKind:
        sb.Append("DummyReturn:\n");
        sb.Append("    ldc.i4 0\n");
        sb.Append("    ret\n");
        break;
      case Type.Kind.boolPtrKind:
      case Type.Kind.intPtrKind:
        sb.Append("DummyReturn:\n");
        sb.Append("    ldnull\n");
        sb.Append("    ret\n");
        break;
      default:
        throw new Exception("invalid function type kind");
    } // switch
  } // GenFuncBody

  private static void GenMainFuncBody(StringBuilder sb)
  {
    Symbol sy = SymTab.CurSymbols();
    while (sy != null)
    {
      if (sy.kind == Symbol.Kind.funcKind && NameList.NameOf(sy.spix) == "main")
      {
        sb.Append("    .maxstack 100\n");
        GenLocVars(sb, sy.symbols);
        GenFuncBody(sb, sy);
        return;
      } // if
      sy = sy.next;
    } // while
  } // GenMainFuncBody

  private static void GenCctorBody(StringBuilder sb)
  {
    sb.Append("    .maxstack 100\n");
    Symbol sy = SymTab.CurSymbols();
    while (sy != null)
    {
      if (sy.kind == Symbol.Kind.varKind && sy.init)
      {
        sb.Append("    ldc.i4  " + sy.val + "\n");
        sb.Append("    stsfld ");
        GenType(sb, sy.type);
        sb.Append(MODULE + "::" + NameList.NameOf(sy.spix) + "\n");
      } // if
      sy = sy.next;
    } // while
    sb.Append("    ret\n");
  } // GenCctorBody

  public static void GenerateCilFile(String path, String moduleName)
  {

    if (path.Length != 0 && path[path.Length - 1] != '\\')
      path = path + "\\";

    MODULE = moduleName;
    loopEndLabels = new Stack();

    StringBuilder GLOBALS = new StringBuilder();
    StringBuilder METHODS = new StringBuilder();
    StringBuilder MAINBODY = new StringBuilder();
    StringBuilder CCTORBODY = new StringBuilder();

    GenGlobConsts(GLOBALS);
    GenGlobVars(GLOBALS);
    GenGlobFuncs(METHODS);
    GenMainFuncBody(MAINBODY);
    GenCctorBody(CCTORBODY);

    try
    {
      FileStream ilFs = new FileStream(path + moduleName + ".il", FileMode.Create);
      StreamWriter cil = new StreamWriter(ilFs);
      Template t = new Template(path, "CIL.frm");
      cil.WriteLine(t.Instance(
        new String[] { "MODULE", "GLOBALS", "METHODS", "MAINBODY", "CCTORBODY" },
        new String[]{ MODULE,    
                      GLOBALS.ToString(),  
                      METHODS.ToString(), 
                      MAINBODY.ToString(),
                      CCTORBODY.ToString()
                    }));
      cil.Close();
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
    } // catch

  } // GenerateCilFile


  public static void GenerateAssembly(string path, string moduleName)
  {
    //-----------------------------------|---------------------------------------

    String ilFileName;
    if (path == "")
      ilFileName = moduleName + ".il";
    else
      ilFileName = path + moduleName + ".il";
    if (File.Exists(ilFileName))
      File.Delete(ilFileName);

    Console.WriteLine("emitting   CIL to \"" + ilFileName + "\" ...");
    GenCilAsText.GenerateCilFile(path, moduleName);

    String exeFileName;
    if (path == "")
      exeFileName = moduleName + ".exe";
    else
      exeFileName = path + moduleName + ".exe";
    if (File.Exists(exeFileName))
      File.Delete(exeFileName);

    String winDir = Environment.GetEnvironmentVariable("windir");
    String dotNetFwDir = winDir + "\\Microsoft.NET\\Framework\\v4.0.30319\\";
    if (!File.Exists(dotNetFwDir + "ilasm.exe"))
    {
      Console.WriteLine("ilasm.exe not found in \"" + dotNetFwDir);
      Console.WriteLine("  assembling \"" + ilFileName + "\" has to be done manually");
      return;
    } // if

    Console.WriteLine("assembling CIL to \"" + exeFileName + "\" ...");
    ProcessStartInfo psi = new ProcessStartInfo
    {
      FileName = dotNetFwDir + "ilasm.exe",
      Arguments = "/QUIET " + ilFileName,
      UseShellExecute = false,
      RedirectStandardError = true,
      RedirectStandardOutput = true
    }; // new

    using (Process p = Process.Start(psi))
    {
      StreamReader stderr = p.StandardError;
      String line = stderr.ReadLine();
      while (line != null)
      {
        Console.WriteLine(line);
        line = stderr.ReadLine();
      } // while
      p.WaitForExit();
    } // using

    if (!File.Exists(exeFileName))
      Console.WriteLine("some errors during assembly detected");

  } // GenerateAssembly


#if TEST_GENCILASTEXT

  public static void Main(String[] args) {
    Console.WriteLine("START: GenCilAsText");

    Console.WriteLine("END");

    Console.WriteLine();
    Console.WriteLine("type [CR] to continue ...");
    Console.ReadLine();
  } // main

#endif // TEST_GENCILASTEXT

} // GenCilAsText

#endif // GENCILASTEXT

// End of GenCilAsText.cs
//=====================================|========================================