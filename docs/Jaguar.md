# Jaguar VM Language

## Segments

Memory segments:

* Argument
* Local
* Pointer
* This
* That
* Temp
* Static

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

* pop
* nop
* ret
* function _label_ _nlocals_
* call _label_ _narguments_

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