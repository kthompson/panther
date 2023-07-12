// IL code: Nulls
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi sealed $Program
	extends [System.Private.CoreLib]System.Object
{
    .field public static string y
    .field public static string x

	.method public static
		void main () cil managed
	{
		// Method begins at RVA 0x2070
		// Code size 19 (0x13)
		.maxstack 1
		.entrypoint
		.locals init (
			[0] object
		)

		IL_0000: ldnull
		IL_0001: stsfld string $Program::y
		IL_0006: ldstr "test"
		IL_000b: stsfld string $Program::x
		IL_0010: ldnull
		IL_0011: stsfld string $Program::x
		IL_0016: ldsfld string $Program::x
		IL_001b: stloc.0
		IL_001c: ldloc.0
		IL_001d: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0022: ret
	} // end of method $Program::main

} // end of class $Program


