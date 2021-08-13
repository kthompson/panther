// IL code: Classes
.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi abstract sealed Point
	extends [System.Private.CoreLib]System.Object
{
	// Fields
	.field public int32 X
	.field public int32 Y

	// Methods
	.method public
		instance void .ctor (
			int32 X,
			int32 Y
		) cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 15 (0xf)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld int32 Point::X
		IL_0007: ldarg.0
		IL_0008: ldarg.2
		IL_0009: stfld int32 Point::Y
		IL_000e: ret
	} // end of method Point::.ctor

} // end of class Point

.class public auto ansi abstract sealed Extent
	extends [System.Private.CoreLib]System.Object
{
	// Fields
	.field public int32 xmin
	.field public int32 xmax
	.field public int32 ymin
	.field public int32 ymax

	// Methods
	.method public
		instance void .ctor (
			int32 xmin,
			int32 xmax,
			int32 ymin,
			int32 ymax
		) cil managed
	{
		// Method begins at RVA 0x2060
		// Code size 30 (0x1e)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld int32 Extent::xmin
		IL_0007: ldarg.0
		IL_0008: ldarg.2
		IL_0009: stfld int32 Extent::xmax
		IL_000e: ldarg.0
		IL_000f: ldarg.3
		IL_0010: stfld int32 Extent::ymin
		IL_0015: ldarg.0
		IL_0016: ldarg.s ymax
		IL_0018: stfld int32 Extent::ymax
		IL_001d: ret
	} // end of method Extent::.ctor

	.method public static
		int32 height () cil managed
	{
		// Method begins at RVA 0x207f
		// Code size 12 (0xc)
		.maxstack 8

		IL_0000: ldfld int32 Extent::ymax
		IL_0005: ldfld int32 Extent::ymin
		IL_000a: sub
		IL_000b: ret
	} // end of method Extent::height

	.method public static
		int32 width () cil managed
	{
		// Method begins at RVA 0x208c
		// Code size 12 (0xc)
		.maxstack 8

		IL_0000: ldfld int32 Extent::xmax
		IL_0005: ldfld int32 Extent::xmin
		IL_000a: sub
		IL_000b: ret
	} // end of method Extent::width

} // end of class Extent

.class public auto ansi abstract sealed $Program
	extends [System.Private.CoreLib]System.Object
{
	// Methods
	.method public static
		void main () cil managed
	{
		// Method begins at RVA 0x209c
		// Code size 14 (0xe)
		.maxstack 2
		.entrypoint
		.locals (
		[0] int32,
		[1] int32
		)

		IL_0000: ldc.i4.s 10
		IL_0002: stloc.0
		IL_0003: ldc.i4.s 20
		IL_0005: stloc.1
		IL_0006: ldloc.0
		IL_0007: ldloc.1
		IL_0008: call instance void Point::.ctor(int32, int32)
		IL_000d: ret
	} // end of method $Program::main

} // end of class $Program


