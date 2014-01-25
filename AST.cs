// AST.cs                                                        HDO, 2006-08-28
// ------
// Abstract syntax tree for MiniCpp.
//=====================================|========================================

#undef TEST_AST

using System;
using System.Collections;
using System.Text;


public class SrcPos  {

  public int line;
  public int col;

  public SrcPos() {
    this.line = MiniCppLex.tokenLine;
    this.col  = MiniCppLex.tokenCol;
  } // SrcPos

} // SrcPos



// === Expressions ===


public abstract class Expr {

  public enum Kind {litOperandKind, varOperandKind,
                    unaryOperatorKind, binaryOperatorKind,
                    arrIdxOperatorKind, funcCallOperatorKind, newOperatorKind};

  public SrcPos srcPos;
  public Kind   kind;
  public Type   type;
  public Expr   next; /*for actual parameter lists*/

  protected Expr(Kind kind, SrcPos sp) {
    if (sp == null)
      sp = new SrcPos();
    this.srcPos = sp;
    this.kind = kind;
  } // Expr

  public override String ToString() {
    if (next == null)
      return "";
    else
      return ", " + next.ToString();
  } // ToString

} // Expr


// --- Operands ---

public abstract class Operand: Expr {

  protected Operand(Kind kind, SrcPos sp)
  : base(kind, sp) {
  } // Operand

} // Operand


public class LitOperand: Operand {

  public int val;

  public LitOperand(Type type, int val)
  : base(Kind.litOperandKind, null) {
    this.type = type;
    this.val  = val;
  } // ConstOperand

  public override String ToString() {
    if (type == Type.boolType) {
      if (val == 0)
        return "false";
      else
        return "true";
    } else if (type == Type.intType) {
      return /* "(" + type + ")" + */  val.ToString() +
        base.ToString();
    } else if (type == Type.voidPtrType) { // val == 0
      return /* "(" + type + ")" + */ val.ToString() +
        base.ToString();
    } else
      return "???";
  } // ToString

} // LitOperand


public class VarOperand: Operand {

  public Symbol sy;

  public VarOperand(Symbol sy)
  : base(Kind.varOperandKind, null) {
    this.type = sy.type;
    this.sy   = sy;
    if (sy.kind != Symbol.Kind.undefKind && 
        sy.kind != Symbol.Kind.constKind && 
        sy.kind != Symbol.Kind.varKind   && 
        sy.kind != Symbol.Kind.parKind)
      Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol, 
                      "invalid symbol kind"); 
  } // VarOperand

  public override String ToString() {
    return sy.ToString() + 
           base.ToString();
  } // ToString

} // VarOperand


// --- Operators ---

public abstract class Operator: Expr {

  protected Operator(Kind kind, SrcPos sp)
  : base(kind, sp) {
  } // Operator

} // Operator


public class UnaryOperator: Operator {

  public enum Operation {undefOp, notOp, posOp, negOp};

  public Operation op;
  public Expr e;

  public UnaryOperator(SrcPos sp, Operation op, Expr e)
  : base(Expr.Kind.unaryOperatorKind, sp) {
    this.op = op;
    this.e = e;
    this.type = e.type;
    if (op == Operation.undefOp) {
      Errors.SemError(sp.line, sp.col, "invalid operator");
      return;
    } // if
    if (type == Type.undefType)
      return;
    if (op == Operation.notOp) {
      if (type != Type.boolType)
        Errors.SemError(sp.line, sp.col, "expr of type bool expected");
    } else { // op == Operation.posOp || op == Operation.negOp)
      if (type != Type.intType)
        Errors.SemError(sp.line, sp.col, "expr of type int expected");
    } // else
  } // UnaryOperator

  private String OperatorFor(Operation op) {
    switch (op) {
      case Operation.notOp:
        return "not";
      case Operation.posOp:
        return "+";
      case Operation.negOp:
        return "-";
      default:
        return "???";
    } // switch
  } // OperatorFor

  public override String ToString() {
    return OperatorFor(op) + " " + e.ToString() + 
           base.ToString();
  } // ToString

} // UnaryOperator


public class BinaryOperator: Operator {

  public enum Operation {undefOp,
                         orOp, andOp,
                         eqOp, neOp, ltOp, leOp, gtOp, geOp,
                         addOp, subOp, mulOp, divOp, modOp};

  public Operation op;
  public Expr left, right;

  public BinaryOperator(SrcPos sp, Operation op, Expr left, Expr right) 
  :  base(Expr.Kind.binaryOperatorKind, sp) {
    this.op = op;
    this.left = left;
    this.right = right;
    if (op == Operation.undefOp)
      Errors.SemError(sp.line, sp.col, "invalid operator");
    if (left.type == Type.undefType || right.type == Type.undefType) {
      this.type = Type.undefType;
      return;
    } // if
    if (op == Operation.orOp || op == Operation.andOp) {
      if (left.type != Type.boolType || right.type != Type.boolType) {
        Errors.SemError(sp.line, sp.col, "bool operands needed");
        this.type = Type.undefType;
      } else
        this.type = Type.boolType;
    } else if (op == Operation.eqOp || op == Operation.neOp ||
               op == Operation.ltOp || op == Operation.leOp ||
               op == Operation.gtOp || op == Operation.geOp ) {
      if (left.type != right.type) {
        Errors.SemError(sp.line, sp.col, "type mismatch in operands");
        this.type = Type.undefType;
      } else
        this.type = Type.boolType;
    } else { // addOp, subOp, mulOp, divOp, modOp
      if (left.type != Type.intType || right.type != Type.intType) {
        Errors.SemError(sp.line, sp.col, "operands of type integer expected");
        this.type = Type.undefType;
      } else
        this.type = Type.intType;
    } // else
  } // BinaryOperator

  private String OperatorFor(Operation op) {
    switch (op) {
      case Operation.orOp:
        return "||";
      case Operation.andOp:
        return "&&";
      case Operation.eqOp:
        return "==";
      case Operation.neOp:
        return "!=";
      case Operation.ltOp:
        return "<";
      case Operation.leOp:
        return "<=";
      case Operation.gtOp:
        return ">";
      case Operation.geOp:
        return ">=";
      case Operation.addOp:
        return "+";
      case Operation.subOp:
        return "-";
      case Operation.mulOp:
        return "*";
      case Operation.divOp:
        return "/";
      case Operation.modOp:
        return "%";
      default:
        return "???";
    } // switch
  } // OperatorFor

  public override String ToString() {
   return "(" + left.ToString() + ") " + OperatorFor(op) + " (" + right.ToString() + ")" + 
          base.ToString();
  } // ToString

} // BinaryOperator 


public class ArrIdxOperator: Operator {

  public VarOperand arr;
  public Expr   idx;

  public ArrIdxOperator(SrcPos sp, VarOperand arr, Expr idx) 
  :  base(Expr.Kind.arrIdxOperatorKind, sp) {
    this.arr = arr;
    this.idx = idx;
    this.type = Type.undefType;
    if (arr.sy.kind == Symbol.Kind.undefKind) 
      return;
    if (arr.sy.kind != Symbol.Kind.parKind && 
        arr.sy.kind != Symbol.Kind.varKind) {
      Errors.SemError(sp.line, sp.col, "invalid symbol kind");
      return;
    } // if
    if (idx.type == Type.undefType)
      return;
    if (arr.type != Type.boolPtrType && 
        arr.type != Type.intPtrType) {
      Errors.SemError(sp.line, sp.col, "invalid array type");
      return;
    } // if
    if (idx.type != Type.intType) {
      Errors.SemError(sp.line, sp.col, "invalid index type");
      return;
    } // if
    this.type = arr.type.BaseTypeOf();
  } // ArrIdxOperator

  public  override String ToString() {
   return arr.ToString() + "[" + idx.ToString() + "]" + 
          base.ToString();
  } // ToString

} // ArrIdxOperator


public class FuncCallOperator: Operator {

  public Symbol func;
  public Expr   apl;

  public FuncCallOperator(SrcPos sp, Symbol func, Expr apl)
  : base(Expr.Kind.funcCallOperatorKind, sp) {
    this.func = func;
    this.apl  = apl;
    this.type = func.type;
    if (func.kind == Symbol.Kind.undefKind)
      return;
    if (func.kind != Symbol.Kind.funcKind)
      Errors.SemError(sp.line, sp.col, "symbol is no function");
    Symbol fp = func.symbols;
    if (fp == null && !func.defined && func.hadFuncDecl)
      fp = func.funcDeclParList;
    Expr   ap = apl;
    while (fp != null && fp.kind == Symbol.Kind.parKind &&
           ap != null) {
      if (fp.kind != Symbol.Kind.undefKind && ap.type != Type.undefType &&
          fp.type != ap.type) {
        Errors.SemError(sp.line, sp.col, "mismatch in type of parameters");
        return;
      } // if
      fp = fp.next;
      ap = ap.next;
    } // while
    if ( ( (fp == null) || ((fp != null) && (fp.kind != Symbol.Kind.parKind)) ) && 
         (ap == null) )
     {} // both lists are empty
    else
      Errors.SemError(sp.line, sp.col, "mismatch in number of parameters");
  } // FuncCallOperator

  public  override String ToString() {
   return func.ToString() + "("+ (apl == null ? "" : apl.ToString()) + ")" + 
          base.ToString();
  } // ToString

} // FuncCallOperator


public class NewOperator: Operator {

  public Type elemType;
  public Expr noe;

  public NewOperator(SrcPos sp, Type elemType, Expr noe)
  : base(Expr.Kind.newOperatorKind, sp) {
    this.elemType = elemType;
    this.noe      = noe;
    this.type     = Type.undefType;
    if (elemType == Type.undefType || noe.type == Type.undefType)
      return;
    if (elemType == Type.boolPtrType || elemType == Type.boolPtrType) {
      Errors.SemError(sp.line, sp.col, "invalid type");
      return;
    } // if
    if (noe.type != Type.intType) {
      Errors.SemError(sp.line, sp.col, "invalid type");
      return;
    } // if
    this.type = elemType.PtrTypeOf();
  } // NewOperator

  public  override String ToString() {
   return "new " + elemType.ToString() + "[" + noe.ToString() + "]" + 
           base.ToString();
  } // ToString

} // NewOperator



// === Statements ===


public abstract class Stat {

  public enum Kind {emptyStatKind, blockStatKind,
                    incStatKind, decStatKind, assignStatKind, callStatKind,
                    ifStatKind, whileStatKind, breakStatKind,
                    inputStatKind, outputStatKind, deleteStatKind, returnStatKind};

  public SrcPos srcPos;
  public Kind   kind;
  public Stat   next;

  protected Stat(Kind kind, SrcPos sp) {
    if (sp == null)
      sp = new SrcPos();
    this.srcPos = sp;
    this.kind   = kind;
  } // Stat

} // Stat


public class EmptyStat: Stat {

  public EmptyStat(SrcPos sp)
  : base(Stat.Kind.emptyStatKind, sp) {
  } // EmptyStat

} // EmptyStat


public class BlockStat: Stat {

  // local Symbols are added to function scope
  public Stat statList;

  public BlockStat(SrcPos sp, Stat statList)
  : base(Stat.Kind.blockStatKind, sp) {
    this.statList = statList;
  } // BlockStat

} // BlockStat


public class IncStat: Stat {

  public VarOperand vo;

  public IncStat(SrcPos sp, VarOperand vo)
  : base(Stat.Kind.incStatKind, sp) {
    this.vo = vo;
    if (vo.sy.kind != Symbol.Kind.undefKind && 
        vo.sy.kind != Symbol.Kind.varKind)
      Errors.SemError(sp.line, sp.col, "no variable");
    if (vo.sy.type != Type.undefType && 
        vo.sy.type != Type.intType)
      Errors.SemError(sp.line, sp.col, "invalid type");
  } // IncStat

} // IncStat


public class DecStat: Stat {

  public VarOperand vo;

  public DecStat(SrcPos sp, VarOperand vo)
  : base(Stat.Kind.decStatKind, sp) {
    this.vo = vo;
    if (vo.sy.kind != Symbol.Kind.undefKind && 
        vo.sy.kind != Symbol.Kind.varKind)
      Errors.SemError(sp.line, sp.col, "no variable");
    if (vo.sy.type != Type.undefType && 
        vo.sy.type != Type.intType)
      Errors.SemError(sp.line, sp.col, "invalid type");
  } // DecStat

} // DecStat


public class AssignStat: Stat {

  public Expr lhs;
  public Expr rhs;

  public AssignStat(SrcPos sp, Expr lhs, Expr rhs)
  : base(Stat.Kind.assignStatKind, sp) {
    this.lhs = lhs;
    this.rhs = rhs;
    if (lhs is VarOperand) {
      VarOperand vo = lhs as VarOperand;
      if (vo.sy.kind != Symbol.Kind.undefKind && 
          vo.sy.kind != Symbol.Kind.parKind   && 
          vo.sy.kind != Symbol.Kind.varKind)
        Errors.SemError(sp.line, sp.col, "lhs: no variable");
    } else if (lhs is ArrIdxOperator) {
      ; // nothing to check
    } else {
        Errors.SemError(sp.line, sp.col, "lhs: invalid expression");
    } // else
    if (lhs.type == Type.undefType || 
        rhs.type == Type.undefType || 
        lhs.type == rhs.type)
      return;
    if (lhs.type.IsPtrType() &&
        rhs.kind == Expr.Kind.litOperandKind &&
        ((LitOperand)rhs).type.kind == Type.Kind.intKind &&
        ((LitOperand)rhs).val == 0) {
          rhs.type = Type.voidPtrType; // change type of oprand form int to void*
      return;
    } // if

    Errors.SemError(sp.line, sp.col, "type mismatch");
  } // AssignStat

} // AssignStat


public class CallStat: Stat {

  public Symbol func;
  public Expr   apl;

  public CallStat(SrcPos sp, Symbol func, Expr apl)
  : base(Stat.Kind.callStatKind, sp) {
    this.func = func;
    this.apl = apl;
    if (func.kind == Symbol.Kind.undefKind)
      return;
    if (func.kind != Symbol.Kind.funcKind)
      Errors.SemError(sp.line, sp.col, "symbol is no function");
    Symbol fp = func.symbols;
    if (fp == null && !func.defined && func.hadFuncDecl)
      fp = func.funcDeclParList;
    Expr   ap = apl;
    while (fp != null && fp.kind == Symbol.Kind.parKind &&
           ap != null) {
      if (fp.kind != Symbol.Kind.undefKind && ap.type != Type.undefType &&
          fp.type != ap.type) {
        Errors.SemError(sp.line, sp.col, "mismatch in type of parameters");
        return;
      } // if
      fp = fp.next;
      ap = ap.next;
    } // while
    if ( ( (fp == null) || ((fp != null) && (fp.kind != Symbol.Kind.parKind)) ) && 
         (ap == null) )
      {} // both lists are empty
    else
      Errors.SemError(sp.line, sp.col, "mismatch in number of parameters");
  } // CallStat

} // CallStat


public class IfStat: Stat {

  public Expr cond;
  public Stat thenStat;
  public Stat elseStat;

  public IfStat(SrcPos sp, Expr cond, Stat thenStat, Stat elseStat)
  : base(Stat.Kind.ifStatKind, sp) {
    this.cond = cond;
    this.thenStat = thenStat;
    this.elseStat = elseStat;
    if (cond.type != Type.undefType && cond.type != Type.boolType)
      Errors.SemError(sp.line, sp.col, "invalid condition type");
  } // IfStat

} // IfStat


public class WhileStat: Stat {

  public Expr cond;
  public Stat body;

  public WhileStat(SrcPos sp, Expr cond, Stat body)
  : base(Stat.Kind.whileStatKind, sp) {
    this.cond = cond;
    this.body = body;
    if (cond.type != Type.undefType && cond.type != Type.boolType)
      Errors.SemError(sp.line, sp.col, "invalid condition type");
  } // WhileStat

} // WhileStat


public class BreakStat: Stat {

  public BreakStat(SrcPos sp)
  : base(Stat.Kind.breakStatKind, sp) {
  } // BreakStat

} // BreakStat


public class InputStat: Stat {

  public VarOperand vo;

  public InputStat(SrcPos sp, VarOperand vo)
  : base(Stat.Kind.inputStatKind, sp) {
    this.vo = vo;
    if (vo.sy.kind != Symbol.Kind.undefKind && 
        vo.sy.kind != Symbol.Kind.parKind && vo.sy.kind != Symbol.Kind.varKind)
      Errors.SemError(sp.line, sp.col, "invalid symbol kind");
    if (vo.sy.type != Type.undefType && 
        vo.sy.type != Type.boolType && 
        vo.sy.type != Type.intType)
      Errors.SemError(sp.line, sp.col, "invalid type");
  } // InputStat

} // InputStat


public class OutputStat: Stat {

  public ArrayList values; // either Expr, String or special String "\n"

  public OutputStat(SrcPos sp, ArrayList values)
  : base(Stat.Kind.outputStatKind, sp) {
    this.values = values;
    foreach (Object o in values) {
      if (o is Expr) {
        Expr e = o as Expr;
        if (e != null &&
            e.type != Type.undefType && 
            e.type != Type.boolType && e.type != Type.intType )
          Errors.SemError(sp.line, sp.col, "invalid type");
      } else if (o is String) {
        // nothing to check
      } else {
          Errors.SemError(sp.line, sp.col, "invalid value");
      } // else
    } // foreach
  } // OutputSta
} // OutputStat


public class DeleteStat: Stat {

  public VarOperand vo;

  public DeleteStat(SrcPos sp, VarOperand vo)
  : base(Stat.Kind.deleteStatKind, sp) {
    this.vo = vo;
    if (vo.sy.type == Type.undefType)
      return;
    if (!vo.sy.type.IsPtrType())
      Errors.SemError(sp.line, sp.col, "invalid type");
  } // DeleteStat

} // DeleteStat
  

public class ReturnStat: Stat {

  public Expr e;

  public ReturnStat(SrcPos sp, Symbol funcSy, Expr e)
  : base(Stat.Kind.returnStatKind, sp) {
    this.e = e;
    if (funcSy.type == Type.undefType)
      return;
    if (funcSy.type == Type.voidType) {
      if (e != null)
        Errors.SemError(sp.line, sp.col, "invalid return value for void func");
      return;
    } // if
    if (e == null) {
      Errors.SemError(sp.line, sp.col, "missing return value");
      return;
    } // if
    if (funcSy.type != e.type && e.type != Type.undefType)
      Errors.SemError(sp.line, sp.col, "mismatch in return value and func type");
  } // ReturnStat

} // ReturnStat



// === AST: abstract syntax tree ===

public class AST {

 
#if TEST_AST

  public static void Main(String[] args) {
    Console.WriteLine("START: AST");
    
    Console.WriteLine("END");
    Console.WriteLine("type [CR] to continue ...");
    Console.ReadLine();
  } // Main

#endif

} // AST

// End of AST.cs
//=====================================|========================================

