// IL code: HelloWorld
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
		// Code size 13 (0xd)
		.maxstack 1
		.entrypoint
		.locals (
			[0] string
		)

		IL_0000: ldstr "Hello World!"
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: call void [System.Console]System.Console::WriteLine(string)
		IL_000c: ret
	} // end of method Program::main

} // end of class Program


