// IL code: GlobalStatements
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi abstract sealed $Program
	extends [System.Private.CoreLib]System.Object
{
    // Fields
    .field public static int32 x

	// Methods
	.method public static
		void main () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 25 (0x19)
		.maxstack 1
		.entrypoint
		.locals (
			[0] object
		)

		IL_0000: ldc.i4.s 12
		IL_0002: stsfld int32 $Program::x
		IL_0007: ldsfld int32 $Program::x
		IL_000c: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_0011: stloc.0
		IL_0012: ldloc.0
		IL_0013: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0018: ret
	} // end of method $Program::main

} // end of class $Program


