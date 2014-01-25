// CIL.frm:                                                      HDO, 2006-08-28
// -------
//
// Frame file for generation of CIL in form of text to be assembled later on.
//=====================================|========================================

.assembly extern mscorlib {
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 2:0:0:0
} // .assembly

.assembly $MODULE$ {
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::
    .ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::
    .ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
} // .assembly 

.module $MODULE$.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003     
.corflags 0x00000001  


// === class BasicIO ===

.class private auto ansi beforefieldinit BasicIO
       extends [mscorlib]System.Object {
       
  .method public hidebysig static void ReadFromCin([out] bool& b) cil managed {
    .maxstack  2
    .locals init (string V_0)
    call       string [mscorlib]System.Console::ReadLine()
    stloc.0
    ldarg.0
    ldloc.0
    call       bool [mscorlib]System.Convert::ToBoolean(string)
    stind.i1
    ret
  } // .method

  .method public hidebysig static void ReadFromCin([out] int32& i) cil managed {
    .maxstack  2
    .locals init (string V_0)
    call       string [mscorlib]System.Console::ReadLine()
    stloc.0
    ldarg.0
    ldloc.0
    call       int32 [mscorlib]System.Convert::ToInt32(string)
    stind.i4
    ret
  } // .method

  .method public hidebysig static void WriteToCout(bool b) cil managed {
    .maxstack  8
    ldarg.0
    call       void [mscorlib]System.Console::Write(bool)
    ret
  } // .method

  .method public hidebysig static void WriteToCout(int32 i) cil managed {
    .maxstack  8
    ldarg.0
    call       void [mscorlib]System.Console::Write(int32)
    ret
  } // .method
  
  .method public hidebysig static void WriteToCout(string s) cil managed {
    .maxstack  8
    ldarg.0
    call       void [mscorlib]System.Console::Write(string)
    ret
  } // .method

  .method public hidebysig static void WriteEndlToCout() cil managed {
    .maxstack  8
    call       void [mscorlib]System.Console::WriteLine()
    ret
  } // .method

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // .method

} // .class


// === class $MODULE$ ===

// -- class structure declaration ---
.class public auto ansi beforefieldinit $MODULE$
       extends [mscorlib]System.Object {
} // .class

// --- class member declaration ---
.class public auto ansi beforefieldinit $MODULE$
       extends [mscorlib]System.Object {

// --- begin of globals ---       
$GLOBALS$
// --- end   of globals ----       

// --- begin of methods ---
$METHODS$
// --- end   of methods ---

  .method public hidebysig static void Main() cil managed {
    .entrypoint
// --- begin of main body ---
$MAINBODY$
// --- end   of main body ---
  } // .method
  
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // .method
  
  .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed {
// -- begin of cctor body ---
$CCTORBODY$
// --- end of cctor body ---
    } // .method

} // .class

// End of CIL.frm
//=====================================|========================================