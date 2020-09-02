// IL code: GuessingGame
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi abstract sealed $Program
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method public static
		void main () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 146 (0x92)
		.maxstack 2
		.entrypoint
		.locals (
			[0] int32,
			[1] int32,
			[2] int32,
			[3] object,
			[4] object,
			[5] object,
			[6] object,
			[7] object
		)

		IL_0000: ldc.i4.m1
		IL_0001: stloc.0
		IL_0002: ldc.i4.0
		IL_0003: stloc.1
		IL_0004: ldc.i4.s 27
		IL_0006: stloc.2
		// loop start (head: IL_0007)
			IL_0007: ldloc.0
			IL_0008: ldloc.2
			IL_0009: ceq
			IL_000b: ldc.i4.0
			IL_000c: ceq
			IL_000e: brfalse IL_0091

			IL_0013: ldstr "Guess the answer:"
			IL_0018: stloc.3
			IL_0019: ldloc.3
			IL_001a: call void [Panther.StdLib]Panther.Predef::println(object)
			IL_001f: call string [Panther.StdLib]Panther.Predef::readLine()
			IL_0024: call int32 [System.Private.CoreLib]System.Convert::ToInt32(object)
			IL_0029: stloc.0
			IL_002a: ldloc.1
			IL_002b: ldc.i4.1
			IL_002c: add
			IL_002d: stloc.1
			IL_002e: ldloc.0
			IL_002f: ldloc.2
			IL_0030: cgt
			IL_0032: brfalse.s IL_0044

			IL_0034: ldstr "Lower"
			IL_0039: stloc.s 4
			IL_003b: ldloc.s 4
			IL_003d: call void [Panther.StdLib]Panther.Predef::println(object)
			IL_0042: br.s IL_008c

			IL_0044: ldloc.0
			IL_0045: ldloc.2
			IL_0046: clt
			IL_0048: brfalse.s IL_005a

			IL_004a: ldstr "Higher"
			IL_004f: stloc.s 5
			IL_0051: ldloc.s 5
			IL_0053: call void [Panther.StdLib]Panther.Predef::println(object)
			IL_0058: br.s IL_008c

			IL_005a: ldstr "Correct: "
			IL_005f: ldloc.2
			IL_0060: call string [System.Private.CoreLib]System.Convert::ToString(int32)
			IL_0065: call string [System.Private.CoreLib]System.String::Concat(string, string)
			IL_006a: stloc.s 6
			IL_006c: ldloc.s 6
			IL_006e: call void [Panther.StdLib]Panther.Predef::println(object)
			IL_0073: ldloc.1
			IL_0074: call string [System.Private.CoreLib]System.Convert::ToString(int32)
			IL_0079: ldstr " total guesses"
			IL_007e: call string [System.Private.CoreLib]System.String::Concat(string, string)
			IL_0083: stloc.s 7
			IL_0085: ldloc.s 7
			IL_0087: call void [Panther.StdLib]Panther.Predef::println(object)

			IL_008c: br IL_0007
		// end loop

		IL_0091: ret
	} // end of method $Program::main

} // end of class $Program


