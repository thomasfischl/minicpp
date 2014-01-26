// SymTab.cs                                                     HDO, 2006-08-28
// ---------
// Symbol table for MiniCpp
//=====================================|========================================

using System;
using System.Collections;
using System.Text;



// === class Type: represenation of MiniCpp types ===


public class Type {

  public enum Kind {undefKind,
                    voidKind,    voidPtrKind,
                    boolKind,    boolPtrKind,
                    intKind,     intPtrKind,
                    doubleKind,  doublePtrKind
                   };

  public static Type undefType;
  public static Type voidType;
  public static Type voidPtrType; // used for (void*)0 only
  public static Type boolType;
  public static Type boolPtrType;
  public static Type charType;
  public static Type charPtrType;
  public static Type intType;
  public static Type intPtrType;
  public static Type doubleType;
  public static Type doublePtrType;

  public static void Init() {
    undefType   = new Type("undef", Type.Kind.undefKind);
    voidType    = new Type("void",  Type.Kind.voidKind);
    voidPtrType = new Type("void*", Type.Kind.voidPtrKind);
    boolType    = new Type("bool",  Type.Kind.boolKind);
    boolPtrType = new Type("bool*", Type.Kind.boolPtrKind);
    intType     = new Type("int",   Type.Kind.intKind);
    intPtrType  = new Type("int*",  Type.Kind.intPtrKind);
    doubleType = new Type("double", Type.Kind.doubleKind);
    doublePtrType = new Type("double*", Type.Kind.doublePtrKind);
  } // Init

  public int    spix;
  public Kind   kind;

  private Type(String name, Kind kind) {
    this.spix = NameList.SpixOf(name);
    this.kind = kind;
  } // Type

  public bool IsBaseType() {
    return (kind == Type.Kind.undefKind   ||
            kind == Type.Kind.boolKind    ||
            kind == Type.Kind.doubleKind ||
            kind == Type.Kind.intKind);
  } // IsBaseType

  public bool IsPtrType() {
    return (kind == Type.Kind.undefKind   ||
            kind == Type.Kind.voidPtrKind ||
            kind == Type.Kind.boolPtrKind ||
            kind == Type.Kind.doublePtrKind ||
            kind == Type.Kind.intPtrKind);
  } // IsPtrType

  public Type PtrTypeOf() {
    switch (kind) {
      case Type.Kind.undefKind: 
        return undefType;
      case Type.Kind.voidKind:
        return voidPtrType;
      case Type.Kind.boolKind: 
        return boolPtrType;
      case Type.Kind.intKind:   
        return intPtrType;
      case Type.Kind.doubleKind:
        return doublePtrType;
      default: 
        Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol, 
                        "no corresponding ptr type");
        return undefType;
    } // switch
  } // PtrTypeOf

  public Type BaseTypeOf() {
    switch (kind) {
      case Type.Kind.undefKind:   
        return undefType;
      case Type.Kind.voidPtrKind:
        return voidType;
      case Type.Kind.boolPtrKind: 
        return boolType;
      case Type.Kind.intPtrKind:  
        return intType;
      case Type.Kind.doublePtrKind:
        return doubleType;
      default: 
        Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol, 
                        "no corresponding base type");
        return undefType;
    } // switch
  } // BaseTypeOf

  public override String ToString() {
    return NameList.NameOf(spix);
  } // ToString

} // Type



// === class Symbol: represenation of MiniCpp symbols ===


public class Symbol {

  public static bool shortSymbolInfo = true;

  public enum Kind {undefKind,
                    constKind, varKind, parKind, funcKind};

  public int    spix;
  public Kind   kind;
  public Type   type;
  public int    level;
  public bool   init;            // init value for variable
  public int    val;             // for constants and initialized variables only
  public double dblVal;
  public int    addr;            // for globals, params, const and local variables

  // for functions only:
  public bool   hadFuncDecl; 
  public Symbol funcDeclParList; // parameter list from declaration
  public bool   defined;         // function has already been defined
  public Symbol symbols;         // parameters and local variables
  public Stat   statList;   

  public Symbol next;

  public Symbol(int spix, Kind kind, Type type, bool asPtrType) {
    this.spix = spix;
    this.kind = kind;
    if (asPtrType)
      type = type.PtrTypeOf();
    this.type = type;
    this.level = SymTab.CurScopeLevel();
  } // Symbol
  
  public void FuncDef() {
    if (kind == Kind.undefKind)
      return;
    if (kind == Kind.funcKind) {
      if (!defined) {
        if (NameList.NameOf(spix) == "main") {
          if (type != Type.undefType && type != Type.voidType)
              Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol,
                              "main func must be void");
          if (symbols != null)
              Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol,
                              "main func must not have parameters");
        } // if
        defined = true;
        if (hadFuncDecl) { // do decl and def parameters match?
          Symbol declPar = funcDeclParList;
          Symbol defPar  = symbols;
          while (declPar != null && defPar != null) {
            if (declPar.kind != Kind.undefKind && defPar.kind != Kind.undefKind &&
                declPar.type != defPar.type) {
              Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol,
                              "mismatch in type of parameters in decl and def");
              return;
            } // if
            declPar = declPar.next;
            defPar  = defPar.next;
          } // while
          if (declPar != null || defPar != null)
            Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol,
                              "mismatch in number of parameters in decl and def");
        } // if
      } else // defined
        Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol,
                        "invalid function redefinition");
    } else // kind != Kind.funcKind
      Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol,
                      "symbol is no function");
  } // FuncDef

  public override String ToString() {
    if (Symbol.shortSymbolInfo)
      return NameList.NameOf(spix);
    StringBuilder sb = new StringBuilder();
    sb.Append(NameList.NameOf(spix) + ": ");
    switch (kind) {
      case Kind.undefKind:  sb.Append("undef"); 
                            break;
      case Kind.constKind:  sb.Append("const");
                            sb.Append(", type = " + type);
                            sb.Append(", val = " + val);
                            sb.Append(", addr = " + addr);
                            break;
      case Kind.varKind:    sb.Append("var");
                            sb.Append(", type = " + type);
                            sb.Append(", init = " + init);
                            sb.Append(", addr = " + addr);
                            break;
      case Kind.parKind:    sb.Append("par");
                            sb.Append(", type = " + type);
                            sb.Append(", addr = " + addr);
                            break;
      case Kind.funcKind:   sb.Append("func");
                            sb.Append(", type = " + type);
                            sb.Append(", addr = " + addr);
                            sb.Append(", defined = " + defined);
                            break;
    } // switch
    return sb.ToString();
  } // ToString
                                                                                                                                                             
} // Symbol



// === class SymbTab: symbol table for MiniCpp symbols ===


public class SymTab {

  private class Scope {

    public int    level;
    public int    nrOfParams, nrOfLocals; 
    public Symbol symbols;
    public Scope  outer;     /*to build a stack of scopes*/

    public Scope(Scope outer) {
      this.level   = curLevel;
      this.nrOfParams = 0;
      this.nrOfLocals = 0;
      this.outer   = outer;
      this.symbols = null;
    } // Scope

  } // Scope


  private static int    curLevel;
  private static Scope  curScope;
  private static Symbol mainFunc;


  public static void Init() {
  //-----------------------------------|----------------------------------------
    Type.Init();
    curLevel = 0;
    curScope = new Scope(null);
    mainFunc = null;
  } // Init


  public static bool MainFuncDefined() {
  //-----------------------------------|----------------------------------------
    return (mainFunc != null) && (mainFunc.defined);
  } // MainFuncDefined


  public static void EnterScope() {
  //-----------------------------------|----------------------------------------
    if (curLevel == 1)
      throw new Exception("scope stack overflow");
    curLevel++;
    curScope = new Scope(curScope); // push new scope on scope stack
  } // EnterScope


  public static Symbol CurSymbols() {
  //-----------------------------------|----------------------------------------
    return curScope.symbols;
  } // CurSymbols

 
  public static int CurScopeLevel() {
  //-----------------------------------|----------------------------------------
    return curScope.level;
  } // CurScopeLevel


  public static void LeaveScope() {
  //-----------------------------------|----------------------------------------
    if (curLevel == 0)
      throw new Exception("scope stack underflow");
    curLevel--;
    curScope = curScope.outer; // pop scope from scope stack
  } // LeaveScope


  public static Symbol Insert(int spix, Symbol.Kind kind, 
                              Type type, bool asPtrType) {
  //-----------------------------------|----------------------------------------
    Symbol prev = null;
    Symbol sy = curScope.symbols;
    while (sy != null) {
      if (sy.spix == spix) {
        Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol, 
                        "multiple definition");
        sy.kind = Symbol.Kind.undefKind;
        return sy;
      } // if
      prev = sy;
      sy = sy.next;
    } // while
    // assert: sy == null
    sy = new Symbol(spix, kind, type, asPtrType);
    if (kind == Symbol.Kind.parKind) {
      sy.addr = curScope.nrOfParams;
      curScope.nrOfParams++;
    } else if (kind == Symbol.Kind.constKind ||
               kind == Symbol.Kind.varKind ) {
      sy.addr = curScope.nrOfLocals;
      curScope.nrOfLocals++;
    } // else
    if (sy.kind == Symbol.Kind.funcKind && NameList.NameOf(spix) == "main")
      mainFunc = sy;
    if (prev == null)
      curScope.symbols = sy;
    else
      prev.next = sy;
    return sy;
  } // Insert


  public static Symbol Lookup(int spix) {
  //-----------------------------------|----------------------------------------
    Scope sc = curScope;
    do {
      Symbol sy = sc.symbols;
      while (sy != null) {
        if (sy.spix == spix)
          return sy;
        sy = sy.next;
      } // while
      sc = sc.outer;
    } while (sc != null);
    return null;
  } // Lookup


  public static Symbol SymbolOf(int spix, params Symbol.Kind[] validKinds) {
  //-----------------------------------|----------------------------------------
    Symbol sy = SymTab.Lookup(spix);
    if (sy == null) {
        Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol, 
                        "undefined symbol");
      return SymTab.Insert(spix, Symbol.Kind.undefKind, Type.undefType, false);
    } // if
    if (sy.kind == Symbol.Kind.undefKind) 
      return sy;
    foreach(Symbol.Kind vk in validKinds)
      if (sy.kind == vk)
        return sy;
    Errors.SemError(MiniCppLex.tokenLine, MiniCppLex.tokenCol, "invalid symbol kind");
    sy.kind = Symbol.Kind.undefKind;
    return sy;
  } // SymbolOf


} // SymTab


// End of SymTab.cs
//=====================================|========================================

/*
namespace Dummy {
  abstract class Symbol {}
  class ConstSymbol: Symbol {}
  class VarSymbol:   Symbol {}
  class ParSymbol:   Symbol {}
  class FuncSymbol:  Symbol {}
} // Dummy
*/

