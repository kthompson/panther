// IL code: NonUnitStatements
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi sealed $Program
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method public static
		int32 doThing (
			int32 'value'
		) cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 18 (0x12)
		.maxstack 2
		.locals init (
			[0] object
		)

		IL_0000: ldarg.0
		IL_0001: call string [System.Private.CoreLib]System.Convert::ToString(int32)
		IL_0006: stloc.0
		IL_0007: ldloc.0
		IL_0008: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_000d: ldarg.0
		IL_000e: ldc.i4.s 12
		IL_0010: sub
		IL_0011: ret
	} // end of method $Program::doThing

	.method public static
		void main () cil managed
	{
		// Method begins at RVA 0x2070
		// Code size 19 (0x13)
		.maxstack 1
		.entrypoint
		.locals init (
			[0] int32,
			[1] int32
		)

		IL_0000: ldc.i4.1
		IL_0001: stloc.0
		IL_0002: ldloc.0
		IL_0003: call int32 $Program::doThing(int32)
		IL_0008: pop
		IL_0009: ldc.i4.2
		IL_000a: stloc.1
		IL_000b: ldloc.1
		IL_000c: call int32 $Program::doThing(int32)
		IL_0011: pop
		IL_0012: ret
	} // end of method $Program::main

} // end of class $Program


