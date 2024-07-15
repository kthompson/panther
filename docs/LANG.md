

# primitive types

* Unsigned ints u8, u16, u32, u64, usize (TODO)
* Signed ints   i8, i16, i32, i64, isize (TODO)
* Floats:   f32, f64 (TODO)
* char
* bool
* string

# expressions

```
if (x == 12) {
    ...
} else {
    ...
}
```

```
val result = if (x == 12) ... else ...
```
structs are value types? or should a trait decide if something is a value type like `Clone` in rust

```
struct Age(u8)
struct Person(name: String, age: Age)
struct Animal(feet: u16) {
}

class Age(u8)
class Person(name: String, age: Age)
class Animal(feet: u16) {
    def speak() = {
        "Hey"
    }
}

val Person(name, Age(age)) = michael

println(name)
println(age)
```

# discriminated union
```
type List<T> {
    Cons(head: T, tail: List),
    Nil
}
```

