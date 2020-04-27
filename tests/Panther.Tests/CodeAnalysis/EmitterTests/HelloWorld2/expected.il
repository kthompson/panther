// IL code: HelloWorld2
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class private auto ansi abstract sealed Program
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method private static
		void main () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 33 (0x21)
		.maxstack 2
		.entrypoint
		.locals (
			[0] string
		)

		IL_0000: call string Program::getName()
		IL_0005: stloc.0
		IL_0006: ldstr "Hello "
		IL_000b: ldloc.0
		IL_000c: call string [System.Private.CoreLib]System.String::Concat(string, string)
		IL_0011: ldstr "!"
		IL_0016: call string [System.Private.CoreLib]System.String::Concat(string, string)
		IL_001b: call void [System.Console]System.Console::WriteLine(string)
		IL_0020: ret
	} // end of method Program::main

	.method private static
		string getName () cil managed
	{
		// Method begins at RVA 0x207d
		// Code size 16 (0x10)
		.maxstack 8

		IL_0000: ldstr "What is your name?"
		IL_0005: call void [System.Console]System.Console::WriteLine(string)
		IL_000a: call string [System.Console]System.Console::ReadLine()
		IL_000f: ret
	} // end of method Program::getName

} // end of class Program


