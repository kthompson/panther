// IL code: GuessingGame
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
		// Code size 147 (0x93)
		.maxstack 2
		.entrypoint
		.locals (
			[0] int32,
			[1] int32,
			[2] int32,
			[3] string,
			[4] string,
			[5] string,
			[6] string,
			[7] string
		)

		IL_0000: ldc.i4.1
		IL_0001: neg
		IL_0002: stloc.0
		IL_0003: ldc.i4.0
		IL_0004: stloc.1
		IL_0005: ldc.i4.s 27
		IL_0007: stloc.2
		// loop start (head: IL_0008)
			IL_0008: ldloc.0
			IL_0009: ldloc.2
			IL_000a: ceq
			IL_000c: ldc.i4.0
			IL_000d: ceq
			IL_000f: brfalse IL_0092

			IL_0014: ldstr "Guess the answer:"
			IL_0019: stloc.3
			IL_001a: ldloc.3
			IL_001b: call void [System.Console]System.Console::WriteLine(string)
			IL_0020: call string [System.Console]System.Console::ReadLine()
			IL_0025: call int32 [System.Private.CoreLib]System.Convert::ToInt32(object)
			IL_002a: stloc.0
			IL_002b: ldloc.1
			IL_002c: ldc.i4.1
			IL_002d: add
			IL_002e: stloc.1
			IL_002f: ldloc.0
			IL_0030: ldloc.2
			IL_0031: cgt
			IL_0033: brfalse.s IL_0045

			IL_0035: ldstr "Lower"
			IL_003a: stloc.s 4
			IL_003c: ldloc.s 4
			IL_003e: call void [System.Console]System.Console::WriteLine(string)
			IL_0043: br.s IL_008d

			IL_0045: ldloc.0
			IL_0046: ldloc.2
			IL_0047: clt
			IL_0049: brfalse.s IL_005b

			IL_004b: ldstr "Higher"
			IL_0050: stloc.s 5
			IL_0052: ldloc.s 5
			IL_0054: call void [System.Console]System.Console::WriteLine(string)
			IL_0059: br.s IL_008d

			IL_005b: ldstr "Correct: "
			IL_0060: ldloc.2
			IL_0061: call string [System.Private.CoreLib]System.Convert::ToString(int32)
			IL_0066: call string [System.Private.CoreLib]System.String::Concat(string, string)
			IL_006b: stloc.s 6
			IL_006d: ldloc.s 6
			IL_006f: call void [System.Console]System.Console::WriteLine(string)
			IL_0074: ldloc.1
			IL_0075: call string [System.Private.CoreLib]System.Convert::ToString(int32)
			IL_007a: ldstr " total guesses"
			IL_007f: call string [System.Private.CoreLib]System.String::Concat(string, string)
			IL_0084: stloc.s 7
			IL_0086: ldloc.s 7
			IL_0088: call void [System.Console]System.Console::WriteLine(string)

			IL_008d: br IL_0008
		// end loop

		IL_0092: ret
	} // end of method Program::main

} // end of class Program


