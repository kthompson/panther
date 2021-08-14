# Bugs:
- REPL: page up is broken 

# TODO:

- classes
  - field assignment  
- convert Predef to Panther?
    - needs namespaces
    - needs imports/using
- object
  - fields
- finish using directive to tell us what symbols to import
- support type aliases via Signatures

# Notes

new[SymbolType] is for creating symbols that belong to the parent symbol.
BoundScope will still be for local/imported symbols

# Borrowing semantics

At any given time, you can have either one mutable reference or any number of immutable references.
References must always be valid.


## Name/File extensions

     Panther source file: .pn
     Panther IL or pil: plain IL file with class definitions and
     Panther byte code or pbc
