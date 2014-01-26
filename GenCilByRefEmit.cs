// GenCilByRefEmit.cs:                            SE, 2009-12-17; HDO 2011-09-26
// ------------------
// Generate assembly using Reflection.Emit.
//=====================================|========================================

#define GENCILBYREFEMIT

#if GENCILBYREFEMIT

#undef TEST_GENCILBYREFEMIT

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;


public static class GenCilByRefEmit {

  private static AssemblyBuilder assemblyBuilder;
  private static TypeBuilder     typeBuilder;
  private static ILGenerator     ilGenerator;

  // lables for break statements
  private static readonly Stack<Label> BreakLabelStack = new Stack<Label>();

  // spix -> LocalBuilder (spix is not consecutive since constant values are inlined)
  private static readonly IDictionary<int, LocalBuilder> LocalBuilders =
    new Dictionary<int, LocalBuilder>();

  // spix -> FieldBuilder (spix is not consecutive since constant values are inlined)
  private static readonly IDictionary<int, FieldBuilder> FieldBuilders =
    new Dictionary<int, FieldBuilder>();

  // spix -> ParameterBuilder
  private static readonly IDictionary<int, ParameterBuilder> ParameterBuilders =
    new Dictionary<int, ParameterBuilder>();

  // spix -> MethodBuilder
  private static readonly IDictionary<int, MethodBuilder> MethodBuilders =
    new Dictionary<int, MethodBuilder>();

  // method references needed for IO operations (replacement for BasicIO class)
  private static readonly MethodInfo ReadLine =
    typeof (Console).GetMethod("ReadLine", System.Type.EmptyTypes);
  private static readonly MethodInfo ConvertToBool =
    typeof (Convert).GetMethod("ToBoolean", new[] {typeof (string)});
  private static readonly MethodInfo ConvertToInt =
    typeof (Convert).GetMethod("ToInt32", new[] {typeof (string)});
  private static readonly MethodInfo WriteBoolToCout =
    typeof (Console).GetMethod("Write", new[] {typeof (bool)});
  private static readonly MethodInfo WriteIntToCout =
    typeof (Console).GetMethod("Write", new[] {typeof (int)});
  private static readonly MethodInfo WriteStringToCout =
    typeof (Console).GetMethod("Write", new[] {typeof (string)});
  private static readonly MethodInfo WriteEndlToCout =
    typeof (Console).GetMethod("WriteLine", System.Type.EmptyTypes);


  // === generate CIL for declarations ===

  private static void GenGlobConsts() {
    // not used, as literals are inserted in place of constants
  } // GenGlobConsts

  private static void GenGlobVars() {
    Symbol sy = SymTab.CurSymbols();
    while (sy != null) {
      if (sy.kind == Symbol.Kind.varKind) {
        FieldBuilder fieldBuilder = typeBuilder.DefineField(
          NameList.NameOf(sy.spix), GetCilType(sy.type), FieldAttributes.Static);
        FieldBuilders[sy.spix] = fieldBuilder;
        // initialize vars
        if (sy.init) {
          if (sy.type == Type.boolType)
            fieldBuilder.SetConstant(sy.val != 0);
          else
            fieldBuilder.SetConstant(sy.val);
        } // if
      } // if
      sy = sy.next;
    } // while
  } // GenGlobVars

  private static void GenLocVars(Symbol symbols) {
    LocalBuilders.Clear();
    Symbol sy = symbols;
    // skip parameters
    while (sy != null && sy.kind == Symbol.Kind.parKind)
      sy = sy.next;
    // generate local declarations for constants and variables
    while (sy != null) {
      if (sy.kind == Symbol.Kind.varKind) {
        LocalBuilder localBuilder = ilGenerator.DeclareLocal(GetCilType(sy.type));
        LocalBuilders[sy.spix] = localBuilder;
        if (sy.init) {
          if (sy.type.IsPtrType() && sy.val == 0)
            ilGenerator.Emit(OpCodes.Ldnull);
          else 
            ilGenerator.Emit(OpCodes.Ldc_I4, sy.val);
          ilGenerator.Emit(OpCodes.Stloc, localBuilder);
        } // if
      } // if
      sy = sy.next;
    } // while
  } // GenLocVars

  private static System.Type GetCilType(Type type) {
    switch (type.kind) {
      case Type.Kind.voidKind:
        return typeof(void);
      case Type.Kind.boolKind:
        return typeof(bool);
      case Type.Kind.boolPtrKind:
        return typeof(bool[]);
      case Type.Kind.intKind:
        return typeof(int);
      case Type.Kind.intPtrKind:
        return typeof(int[]);
      default:
        throw new Exception("invalid type kind");
    } // switch
  } // GetCilType

  private static void GenFuncStubs() {
    Symbol sy = SymTab.CurSymbols();
    while (sy != null) {
      if (sy.kind == Symbol.Kind.funcKind) {
        // collect parameter types
        Symbol parameterSy = sy.symbols;
        IList<System.Type> parameterTypes = new List<System.Type>();
        while (parameterSy != null && parameterSy.kind == Symbol.Kind.parKind) {
          parameterTypes.Add(GetCilType(parameterSy.type));
          parameterSy = parameterSy.next;
        } // while
        System.Type[] parameterTypesArray = new System.Type[parameterTypes.Count];
        parameterTypes.CopyTo(parameterTypesArray, 0);
        MethodBuilders[sy.spix] = typeBuilder.DefineMethod(
          NameList.NameOf(sy.spix), MethodAttributes.Static,
          GetCilType(sy.type), parameterTypesArray);
      } // if
      sy = sy.next;
    } // while
  } // GenFuncStubs

  private static void GenGlobFuncs() {
    Symbol sy = SymTab.CurSymbols();
    while (sy != null) {
      ParameterBuilders.Clear();
      if (sy.kind == Symbol.Kind.funcKind) {
        MethodBuilder methodBuilder = MethodBuilders[sy.spix];
        if (NameList.NameOf(sy.spix) == "main")
          assemblyBuilder.SetEntryPoint(methodBuilder);
        // define parameter properties
        Symbol parameterSy = sy.symbols;
        int position = 1; // position 0 is return value (doc)
        while (parameterSy != null && parameterSy.kind == Symbol.Kind.parKind) {
          ParameterBuilders[parameterSy.spix] = methodBuilder.DefineParameter(
            position, ParameterAttributes.None, NameList.NameOf(parameterSy.spix));
          position++;
          parameterSy = parameterSy.next;
        } // while
        ilGenerator = methodBuilder.GetILGenerator();
        GenLocVars(sy.symbols);
        GenFuncBody(sy);
      } // if
      sy = sy.next;
    } // while
  } // GenGlobFuncs


  // === generate CIL for expresssions ===

  private static void GenLoadConstOperand(LitOperand lo) {
    if (lo.type.kind == Type.Kind.boolKind ||
        lo.type.kind == Type.Kind.intKind)
      ilGenerator.Emit(OpCodes.Ldc_I4, lo.val);
    else if (lo.type.kind == Type.Kind.voidPtrKind &&
             lo.val == 0)
      ilGenerator.Emit(OpCodes.Ldnull);
    else
      throw new Exception("invalid const operand type");
  } // GenLoadConstOperand

  private static void GenLoadVarOperand(VarOperand vo) {
    switch (vo.sy.kind) {
      case Symbol.Kind.constKind: // inline constants
        ilGenerator.Emit(OpCodes.Ldc_I4, vo.sy.val);
        break;
      case Symbol.Kind.varKind:
        if (vo.sy.level == 0) // global scope
          ilGenerator.Emit(OpCodes.Ldsfld, FieldBuilders[vo.sy.spix]);
        else if (vo.sy.level == 1) // function scope
          ilGenerator.Emit(OpCodes.Ldloc, LocalBuilders[vo.sy.spix]);
        else
          throw new Exception("invalid operand scope level");
        break;
      case Symbol.Kind.parKind:
        ilGenerator.Emit(OpCodes.Ldarg, ParameterBuilders[vo.sy.spix].Position - 1);
        break;
      default:
        throw new Exception("invalid operand kind");
    } // switch
  } // GenLoadVarOperand

  private static void GenStoreVarOperand(VarOperand vo) {
    switch (vo.sy.kind) {
      case Symbol.Kind.varKind:
        if (vo.sy.level == 0) // global scope
          ilGenerator.Emit(OpCodes.Stsfld, FieldBuilders[vo.sy.spix]);
        else if (vo.sy.level == 1) // function scope
          ilGenerator.Emit(OpCodes.Stloc, LocalBuilders[vo.sy.spix]);
        else
          throw new Exception("invalid operand scope level");
        break;
      case Symbol.Kind.parKind:
        ilGenerator.Emit(OpCodes.Starg, ParameterBuilders[vo.sy.spix].Position - 1);
        break;
      default:
        throw new Exception("invalid operand kind");
    } // switch
  } // GenStoreVarOperand

  private static void GenUnaryOperator(UnaryOperator uo) {
    GenExpr(uo.e);
    switch (uo.op) {
      case UnaryOperator.Operation.notOp:
        ilGenerator.Emit(OpCodes.Not);
        break;
      case UnaryOperator.Operation.posOp:
        break; // ignore
      case UnaryOperator.Operation.negOp:
        ilGenerator.Emit(OpCodes.Neg);
        break;
      default:
        throw new Exception("invalid unary operator");
    } // switch
  } // GenUnaryOperator

  private static void GenBinaryOperator(BinaryOperator bo) {
    GenExpr(bo.left);
    if (bo.op == BinaryOperator.Operation.orOp || 
        bo.op == BinaryOperator.Operation.andOp) {
      Label label1 = ilGenerator.DefineLabel();
      if (bo.op == BinaryOperator.Operation.orOp)
        ilGenerator.Emit(OpCodes.Brtrue, label1);
      else // bo.op == BinaryOperator.Operation.andOp
        ilGenerator.Emit(OpCodes.Brfalse, label1);
      GenExpr(bo.right);
      Label label2 = ilGenerator.DefineLabel();
      ilGenerator.Emit(OpCodes.Br, label2);
      ilGenerator.MarkLabel(label1);
      if (bo.op == BinaryOperator.Operation.orOp)
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
      else // bo.op == BinaryOperator.Operation.andOp
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
      ilGenerator.MarkLabel(label2);
    } else {
      GenExpr(bo.right);
      switch (bo.op) {
        case BinaryOperator.Operation.orOp:
          break;
        case BinaryOperator.Operation.andOp:
          break;
        case BinaryOperator.Operation.eqOp:
          ilGenerator.Emit(OpCodes.Ceq);
          break;
        case BinaryOperator.Operation.neOp:
          ilGenerator.Emit(OpCodes.Ceq);
          ilGenerator.Emit(OpCodes.Ldc_I4_0);
          ilGenerator.Emit(OpCodes.Ceq);
          break;
        case BinaryOperator.Operation.ltOp:
          ilGenerator.Emit(OpCodes.Clt);
          break;
        case BinaryOperator.Operation.leOp:
          ilGenerator.Emit(OpCodes.Cgt);
          ilGenerator.Emit(OpCodes.Ldc_I4_0);
          ilGenerator.Emit(OpCodes.Ceq);
          break;
        case BinaryOperator.Operation.gtOp:
          ilGenerator.Emit(OpCodes.Cgt);
          break;
        case BinaryOperator.Operation.geOp:
          ilGenerator.Emit(OpCodes.Clt);
          ilGenerator.Emit(OpCodes.Ldc_I4_0);
          ilGenerator.Emit(OpCodes.Ceq);
          break;
        case BinaryOperator.Operation.addOp:
          ilGenerator.Emit(OpCodes.Add);
          break;
        case BinaryOperator.Operation.subOp:
          ilGenerator.Emit(OpCodes.Sub);
          break;
        case BinaryOperator.Operation.mulOp:
          ilGenerator.Emit(OpCodes.Mul);
          break;
        case BinaryOperator.Operation.divOp:
          ilGenerator.Emit(OpCodes.Div);
          break;
        case BinaryOperator.Operation.modOp:
          ilGenerator.Emit(OpCodes.Rem);
          break;
        default:
          throw new Exception("invalid binary operator");
      } // switch
    } // else
  } // GenBinaryOperator

  private static void GenArrIdxOperator(ArrIdxOperator aio) {
    GenLoadVarOperand(aio.arr);
    GenExpr(aio.idx);
    switch (aio.type.kind) {
      case Type.Kind.boolKind:
        ilGenerator.Emit(OpCodes.Ldelem_I1);
        break;
      case Type.Kind.intKind:
        ilGenerator.Emit(OpCodes.Ldelem_I4);
        break;
      default:
        throw new Exception("invalid type kind");
    } // switch
  } // GenArrIdxOperator

  private static void GenCall(Symbol func, Expr apl) {
    Expr ap = apl;
    while (ap != null) {
      GenExpr(ap);
      ap = ap.next;
    } // while
    ilGenerator.EmitCall(OpCodes.Call, MethodBuilders[func.spix], System.Type.EmptyTypes);
  } // GenCall

  private static void GenFuncCallOperator(FuncCallOperator fco) {
    GenCall(fco.func, fco.apl);
  } // GenFuncCallOperator

  private static void GenNewOperator(NewOperator no) {
    GenExpr(no.noe);
    switch (no.elemType.kind) {
      case Type.Kind.boolKind:
        ilGenerator.Emit(OpCodes.Newarr, typeof (bool));
        break;
      case Type.Kind.intKind:
        ilGenerator.Emit(OpCodes.Newarr, typeof (int));
        break;
      default:
        throw new Exception("invalid type kind");
    } // switch
  } // GenNewOperator

  private static void GenExpr(Expr e) {
    switch (e.kind) {
      case Expr.Kind.litOperandKind:
        GenLoadConstOperand(e as LitOperand);
        break;
      case Expr.Kind.varOperandKind:
        GenLoadVarOperand(e as VarOperand);
        break;
      case Expr.Kind.unaryOperatorKind:
        GenUnaryOperator(e as UnaryOperator);
        break;
      case Expr.Kind.binaryOperatorKind:
        GenBinaryOperator(e as BinaryOperator);
        break;
      case Expr.Kind.arrIdxOperatorKind:
        GenArrIdxOperator(e as ArrIdxOperator);
        break;
      case Expr.Kind.funcCallOperatorKind:
        GenFuncCallOperator(e as FuncCallOperator);
        break;
      case Expr.Kind.newOperatorKind:
        GenNewOperator(e as NewOperator);
        break;
      default:
        throw new Exception("invalid expression kind");
    } // switch
  } // GenExpr


// === generate CIL for statements ===

  private static void GenBlockStat(BlockStat s) {
    GenStatList(s.statList);
  } // GenBlockStat

  private static void GenIncStat(IncStat s) {
    GenLoadVarOperand(s.vo);
    ilGenerator.Emit(OpCodes.Ldc_I4_1);
    ilGenerator.Emit(OpCodes.Add);
    GenStoreVarOperand(s.vo);
  } // GenIncStat

  private static void GenDecStat(DecStat s) {
    GenLoadVarOperand(s.vo);
    ilGenerator.Emit(OpCodes.Ldc_I4_1);
    ilGenerator.Emit(OpCodes.Sub);
    GenStoreVarOperand(s.vo);
  } // GenDecStat

  private static void GenAssignStat(AssignStat s) {
    switch (s.lhs.kind) {
      case Expr.Kind.varOperandKind:
        GenExpr(s.rhs);
        GenStoreVarOperand((VarOperand) s.lhs);
        break;
      case Expr.Kind.arrIdxOperatorKind:
        ArrIdxOperator aio = (ArrIdxOperator) s.lhs;
        GenExpr(aio.arr);
        GenExpr(aio.idx);
        GenExpr(s.rhs);
        switch (aio.type.kind) {
          case Type.Kind.boolKind:
            ilGenerator.Emit(OpCodes.Stelem_I1);
            break;
          case Type.Kind.intKind:
            ilGenerator.Emit(OpCodes.Stelem_I4);
            break;
          default:
            throw new Exception("invalid type kind");
        } // switch
        break;
      default:
        throw new Exception("invalid lhs kind");
    } // switch
  } // GenAssignStat

  private static void GenCallStat(CallStat s) {
    GenCall(s.func, s.apl);
    // remove unused return value from stack for non void functions
    if (s.func.type.kind != Type.Kind.voidKind)
      ilGenerator.Emit(OpCodes.Pop);
  } // GenCallStat

  private static void GenIfStat(IfStat s) {
    // start of the else block
    Label elseStatements = ilGenerator.DefineLabel();
    // label after the else block
    Label afterIfStatement = ilGenerator.DefineLabel();
    GenExpr(s.cond);
    // if condition is false, jump to the beginning of the label elseStatements
    ilGenerator.Emit(OpCodes.Brfalse, elseStatements);
    GenStatList(s.thenStat);
    if (s.elseStat != null) // label is not required if no else block exists
      ilGenerator.Emit(OpCodes.Br, afterIfStatement);
    ilGenerator.MarkLabel(elseStatements);
    if (s.elseStat != null) {
      GenStatList(s.elseStat);
      ilGenerator.MarkLabel(afterIfStatement);
    } // if
  } // GenIfStat

  private static void GenWhileStat(WhileStat s) {
    Label beginLabel = ilGenerator.DefineLabel();
    ilGenerator.MarkLabel(beginLabel);
    GenExpr(s.cond);
    Label escapeLabel = ilGenerator.DefineLabel();
    ilGenerator.Emit(OpCodes.Brfalse, escapeLabel);
    BreakLabelStack.Push(escapeLabel);
    GenStatList(s.body);
    BreakLabelStack.Pop();
    // jump to beginning of while statement and check condition again ...
    ilGenerator.Emit(OpCodes.Br, beginLabel);
    ilGenerator.MarkLabel(escapeLabel);
  } // GenWhileStat

  private static void GenSwitchStat(SwitchStat s)
  {
    if (s.caseStat == null && s.defaultStat == null)
    {
      return;
    }

    GenExpr(s.expr);

    Label defaultLabel = ilGenerator.DefineLabel();
    Label endOfSwitchLabel = ilGenerator.DefineLabel();

    var caseLabels = new System.Collections.Generic.Dictionary<Stat, Label>();
    var caseStat = s.caseStat;

    while (caseStat != null)
    {
      Label caseLabel = ilGenerator.DefineLabel();
      caseLabels.Add(caseStat, caseLabel);

      ilGenerator.Emit(OpCodes.Dup);
      ilGenerator.Emit(OpCodes.Ldc_I4, ((CaseStat)caseStat).val);
      ilGenerator.Emit(OpCodes.Beq_S, caseLabel);
      caseStat = caseStat.next;
    }

    if (s.defaultStat != null)
    {
      ilGenerator.Emit(OpCodes.Br_S, defaultLabel);
    }
    else
    {
      ilGenerator.Emit(OpCodes.Br_S, endOfSwitchLabel);
    }

    BreakLabelStack.Push(endOfSwitchLabel);

    caseStat = s.caseStat;
    while (caseStat != null)
    {
      ilGenerator.MarkLabel(caseLabels[caseStat]);
      GenStatList(((CaseStat)caseStat).stat);
      caseStat = caseStat.next;
    }

    if (s.defaultStat != null)
    {
      ilGenerator.MarkLabel(defaultLabel);
      GenStatList(s.defaultStat);
    }

    BreakLabelStack.Pop();
    ilGenerator.MarkLabel(endOfSwitchLabel);
  } // GenSwitchStat

  private static void GenBreakStat() {
    if (BreakLabelStack.Count < 1)
      throw new Exception("break with no loop around");
    ilGenerator.Emit(OpCodes.Br, BreakLabelStack.Peek());
  } // GenBreakStat

  private static void GenInputStat(InputStat stat) {
    // eg.: a = Convert.ToInt32(Console.ReadLine());
    ilGenerator.EmitCall(OpCodes.Call, ReadLine, System.Type.EmptyTypes);
    switch (stat.vo.sy.type.kind) {
      case Type.Kind.boolKind:
        ilGenerator.EmitCall(OpCodes.Call, ConvertToBool, System.Type.EmptyTypes);
        break;
      case Type.Kind.intKind:
        ilGenerator.EmitCall(OpCodes.Call, ConvertToInt, System.Type.EmptyTypes);
        break;
      default:
        throw new Exception("invalid type");
    } // switch
    GenStoreVarOperand(stat.vo);
  } // GenInputStat

  private static void GenOutputStat(OutputStat s) {
    foreach (Object o in s.values) {
      if (o is Expr) {
        Expr e = o as Expr;
        GenExpr(e);
        if (e.type == Type.boolType)
          ilGenerator.EmitCall(OpCodes.Call, WriteBoolToCout, System.Type.EmptyTypes);
        else if (e.type == Type.intType)
          ilGenerator.EmitCall(OpCodes.Call, WriteIntToCout, System.Type.EmptyTypes);
      } else if (o is String) {
        String str = o as String;
        if (str == "\n")
          ilGenerator.EmitCall(OpCodes.Call, WriteEndlToCout, System.Type.EmptyTypes);
        else {
          ilGenerator.Emit(OpCodes.Ldstr, str);
          ilGenerator.EmitCall(OpCodes.Call, WriteStringToCout, System.Type.EmptyTypes);
        } // else
      } else
        throw new Exception("invalid value");
    } // foreach
  } // GenOutputStat

  private static void GenDeleteStat(DeleteStat s) {
    ilGenerator.Emit(OpCodes.Ldnull);
    GenStoreVarOperand(s.vo);
  } // GenDeleteStat

  private static void GenReturnStat(ReturnStat s) {
    if (s.e != null)
      GenExpr(s.e);
    ilGenerator.Emit(OpCodes.Ret);
  } // GenReturnStat

  private static void GenStatList(Stat statList) {
    Stat stat = statList;
    while (stat != null) {
      switch (stat.kind) {
        case Stat.Kind.emptyStatKind:
          break;
        case Stat.Kind.blockStatKind:
          GenBlockStat(stat as BlockStat);
          break;
        case Stat.Kind.incStatKind:
          GenIncStat(stat as IncStat);
          break;
        case Stat.Kind.decStatKind:
          GenDecStat(stat as DecStat);
          break;
        case Stat.Kind.assignStatKind:
          GenAssignStat(stat as AssignStat);
          break;
        case Stat.Kind.callStatKind:
          GenCallStat(stat as CallStat);
          break;
        case Stat.Kind.ifStatKind:
          GenIfStat(stat as IfStat);
          break;
        case Stat.Kind.whileStatKind:
          GenWhileStat(stat as WhileStat);
          break;
        case Stat.Kind.switchStatKind:
          GenSwitchStat(stat as SwitchStat);
          break;
        case Stat.Kind.breakStatKind:
          GenBreakStat();
          break;
        case Stat.Kind.inputStatKind:
          GenInputStat(stat as InputStat);
          break;
        case Stat.Kind.outputStatKind:
          GenOutputStat(stat as OutputStat);
          break;
        case Stat.Kind.returnStatKind:
          GenReturnStat(stat as ReturnStat);
          break;
        case Stat.Kind.deleteStatKind:
          GenDeleteStat(stat as DeleteStat);
          break;
        default:
          throw new Exception("invalid statement kind");
      } // switch
      stat = stat.next;
    } // while
  } // GenStatList

  private static void GenFuncBody(Symbol funcSy) {
    GenStatList(funcSy.statList);
    // add ret opcode for void functions, others must have a return statement
    if (funcSy.type.kind == Type.Kind.voidKind)
      ilGenerator.Emit(OpCodes.Ret);
  } // GenFunctionBody

 
  public static void GenerateAssembly(string path, string moduleName) {
  //-----------------------------------|---------------------------------------
    Console.WriteLine("generating CIL to \"" + path + moduleName + ".exe" + "\" ...");

    FieldBuilders.Clear();
    MethodBuilders.Clear();
    BreakLabelStack.Clear();

    assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
      new AssemblyName(moduleName), AssemblyBuilderAccess.Save);

    String filename = moduleName + ".exe";
    ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(filename);
    typeBuilder = moduleBuilder.DefineType(moduleName,
      TypeAttributes.Abstract | TypeAttributes.Sealed); // --> static class

    // function stubs have to be created in advance (for calls from other functions)
    GenFuncStubs();
    GenGlobVars();
    GenGlobFuncs();

    typeBuilder.CreateType();
    moduleBuilder.CreateGlobalFunctions();
    assemblyBuilder.Save(filename);
  } // GenerateAssembly

  
#if TEST_GENCILBYREFEMIT
  
  public static void Main(String[] args) {
    Console.WriteLine("START: GenCilByRefEmit");
  
    Console.WriteLine("END");
  
     Console.WriteLine();
     Console.WriteLine("type [CR] to continue ...");
     Console.ReadLine();
   } // main
   
#endif // TEST_GENCILBYREFEMIT

} // GenCilByRefEmit

#endif // GENCILBYREFEMIT

// End of GenCilByRefEmit.cs
//=====================================|========================================