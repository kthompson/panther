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
	.method public static
		int32 height () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 12 (0xc)
		.maxstack 8

		IL_0000: ldsfld int32 Extent::ymax
		IL_0005: ldsfld int32 Extent::ymin
		IL_000a: sub
		IL_000b: ret
	} // end of method Extent::height

	.method public static
		int32 width () cil managed
	{
		// Method begins at RVA 0x205d
		// Code size 12 (0xc)
		.maxstack 8

		IL_0000: ldsfld int32 Extent::xmax
		IL_0005: ldsfld int32 Extent::xmin
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
		// Method begins at RVA 0x206a
		// Code size 1 (0x1)
		.maxstack 8
		.entrypoint

		IL_0000: ret
	} // end of method $Program::main

} // end of class $Program


