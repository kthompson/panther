// IL code: HelloWorld2
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class private auto ansi abstract sealed Program
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method private static
		string getName () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 20 (0x14)
		.maxstack 1
		.locals (
			[0] string,
			[1] string
		)

		IL_0000: ldstr "What is your name?"
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: call void [System.Console]System.Console::WriteLine(string)
		IL_000c: call string [System.Console]System.Console::ReadLine()
		IL_0011: stloc.1
		IL_0012: ldloc.1
		IL_0013: ret
	} // end of method Program::getName

	.method private static
		void main () cil managed
	{
		// Method begins at RVA 0x2070
		// Code size 37 (0x25)
		.maxstack 2
		.entrypoint
		.locals (
			[0] string,
			[1] string,
			[2] void
		)

		IL_0000: call string Program::getName()
		IL_0005: stloc.0
		IL_0006: ldstr "Hello "
		IL_000b: ldloc.0
		IL_000c: call string [System.Private.CoreLib]System.String::Concat(string, string)
		IL_0011: ldstr "!"
		IL_0016: call string [System.Private.CoreLib]System.String::Concat(string, string)
		IL_001b: stloc.1
		IL_001c: ldloc.1
		IL_001d: call void [System.Console]System.Console::WriteLine(string)
		IL_0022: stloc.2
		IL_0023: ldloc.2
		IL_0024: ret
	} // end of method Program::main

} // end of class Program


