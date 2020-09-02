// IL code: HelloWorld2
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi abstract sealed $Program
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method public static
		string getName () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 18 (0x12)
		.maxstack 1
		.locals (
			[0] object
		)

		IL_0000: ldstr "What is your name?"
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_000c: call string [Panther.StdLib]Panther.Predef::readLine()
		IL_0011: ret
	} // end of method $Program::getName

	.method public static
		void main () cil managed
	{
		// Method begins at RVA 0x2070
		// Code size 35 (0x23)
		.maxstack 2
		.entrypoint
		.locals (
			[0] string,
			[1] object
		)

		IL_0000: call string $Program::getName()
		IL_0005: stloc.0
		IL_0006: ldstr "Hello "
		IL_000b: ldloc.0
		IL_000c: call string [System.Private.CoreLib]System.String::Concat(string, string)
		IL_0011: ldstr "!"
		IL_0016: call string [System.Private.CoreLib]System.String::Concat(string, string)
		IL_001b: stloc.1
		IL_001c: ldloc.1
		IL_001d: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_0022: ret
	} // end of method $Program::main

} // end of class $Program


