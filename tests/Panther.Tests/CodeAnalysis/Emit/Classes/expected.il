.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi sealed Point
	extends [System.Private.CoreLib]System.Object
{
	.field public int32 X
	.field public int32 Y

	.method public hidebysig specialname rtspecialname
		instance void .ctor (
			int32 X,
			int32 Y
		) cil managed
	{
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

.class public auto ansi sealed Extent
	extends [System.Private.CoreLib]System.Object
{
	.field public int32 xmin
	.field public int32 xmax
	.field public int32 ymin
	.field public int32 ymax

	.method public hidebysig specialname rtspecialname
		instance void .ctor (
			int32 xmin,
			int32 xmax,
			int32 ymin,
			int32 ymax
		) cil managed
	{
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

	.method public
		instance int32 height () cil managed
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldfld int32 Extent::ymax
		IL_0006: ldarg.0
		IL_0007: ldfld int32 Extent::ymin
		IL_000c: sub
		IL_000d: ret
	} // end of method Extent::height

	.method public
		instance int32 width () cil managed
	{
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldfld int32 Extent::xmax
		IL_0006: ldarg.0
		IL_0007: ldfld int32 Extent::xmin
		IL_000c: sub
		IL_000d: ret
	} // end of method Extent::width

} // end of class Extent

.class public auto ansi sealed $Program
	extends [System.Private.CoreLib]System.Object
{
	.method public static
		void main () cil managed
	{
		.maxstack 2
		.entrypoint
		.locals init (
		[0] class Point,
		[1] object
		)

		IL_0000: ldc.i4.s 10
		IL_0002: ldc.i4.s 20
		IL_0004: newobj instance void Point::.ctor(int32, int32)
		IL_0009: stloc.0
		IL_000a: ldloc.0
		IL_000b: ldfld int32 Point::X
		IL_0010: box [System.Private.CoreLib]System.Int32
		IL_0015: stloc.1
		IL_0016: ldloc.1
		IL_0017: call void [Panther.StdLib]Panther.Predef::println(object)
		IL_001c: ret
	} // end of method $Program::main

} // end of class $Program


