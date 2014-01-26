cleanup.bat

SG minicpp.atg
PGT minicpp.atg
csc /debug *.cs 

MiniCpp.exe testProgramInt.mcpp
MiniCpp.exe testProgramDouble.mcpp
rem testProgramDouble.emit.exe
rem testProgramDouble.text.exe

MiniCpp.exe testProgramSwitch.mcpp
rem testProgramSwitch.emit.exe
rem testProgramSwitch.text.exe
