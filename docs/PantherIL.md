# Panther VM Language

## Features

32-bit data bus
16-bit address bus

## Segments

Memory segments:

* Argument
* Local
* Pointer
* This
* That
* Temp
* Static

## Metadata

### Class definition

```
.class _className_
```

### Field definition

Must be before any method declarations

```
.field [.static] _fieldName_
```


### Method definitions

```
.method [.entrypoint] _name_ (p1: _type_, p2: _type_)
```

Examples:
```
.method .entrypoint Main(): void

.method MakePoint (x: int, y: int): Point
```

## Op Codes

### Arguments

* ldarg _i_ - load argument with index _i_ to the stack
* starg _i_ - store value at top of stack to argument _i_

### Locals

* ldloc _i_ - load local with index _i_ to the stack
* stloc _i_ - store value at top of stack to local _i_

### Fields

* ldfld _i_ - take the top of the stack, and load its field with index _i_ to the stack
* stfld _i_ - take two from stack and store value at top of stack to field _i_

* ldsfld _label_
* stsfld _label_

### Constants

* ldc _c_
* ldstr _s_

### Stack

* call Class.method
* pop
* nop
* ret

### Arithmetic

* add
* and
* div
* mul
* neg
* not
* or
* sub
* xor

### Comparison

* ceq
* cgt
* clt

### Branch

* label _label_
* br _label_
* brfalse _label_
* brtrue _label_

### Heap Allocation

* new _nslots_