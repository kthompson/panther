

# primitive types

* Unsigned ints u8, u16, u32, u64, usize (TODO)
* Signed ints   i8, i16, i32, i64, isize (TODO)
* Floats:   f32, f64 (TODO)
* char (TODO)
* bool

# expressions

if (x == 12) {
    ...
} else {
    ...
}


val result = if (x == 12) ... else ...

structs are value types? or should a trait decide if something is a value type like `Clone` in rust

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

# discriminated union
```
enum List<T> {
    case Cons(head: T, tail: List),
    case Nil
}
```

OR

```
sum List<T> {
    Cons(head: T, tail: List),
    Nil
}
```

# traits

trait Speak {
    def speak(): String
}

instance Speak for Person {
    def speak(): String = $"My name is {this.name}"
}

# lowered features



# sugar that can be lowered

expression(a, b, c) -> expression.Invoke(a, b, c)

lock -> try finally

for comprehension -> flapMap + map chain

val Person(name, Age(age)) = michael
