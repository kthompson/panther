﻿// IL code: HelloWorld3
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi sealed HelloWorld
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method public static
		void main () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 13 (0xd)
		.maxstack 1
		.entrypoint
		.locals init (
			[0] object
		)

		IL_0000: ldstr "Hello World!"
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_000c: ret
	} // end of method HelloWorld::main

} // end of class HelloWorld


