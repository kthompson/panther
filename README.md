# Panther

[![Build status](https://github.com/kthompson/panther/actions/workflows/default.yml/badge.svg)](https://github.com/kthompson/panther/actions/workflows/default.yml)
[![codecov](https://codecov.io/gh/kthompson/panther/branch/main/graph/badge.svg?token=VMRWNJXVP1)](https://codecov.io/gh/kthompson/panther)

Panther is a general-purpose, multi-paradigm programming language encompassing strong typing, functional, generic, and object-oriented (class-based) programming disciplines.


# Example

```scala
def main() = {
    var guess = -1
    var guessCount = 0
    var answer = rnd(100)

    while (guess != answer) {
        println("Guess the answer:")
        guess = int(readLine())
        guessCount = guessCount + 1

        if (guess > answer) {
            println("Lower")
        } else if (guess < answer) {
            println("Higher")
        } else {
            println("Correct: " + string(answer))
            println(string(guessCount) + " total guesses")
        }
    }
}
```

# Current language support

- [x] Variables
- [x] Functions
- Control flow
  - [x] if
  - [x] while
  - [x] for
  - [ ] pattern matching
- Builtin Data Types
- [x] Int
- [x] Char
- [x] String
- [x] Boolean
- Algebraic Data Types
  - [x] Product Types
  - [ ] Sum Types

# Road map

- complete basics in order to start writing code in Panther
- rewrite compiler in Panther(self-hosting)
- type aliases (u8 = System.Byte, ...)
- optimize programs before emitting
- floats?