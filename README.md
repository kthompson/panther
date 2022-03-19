# Panther

[![Build status](https://github.com/kthompson/panther/actions/workflows/default.yml/badge.svg)](https://github.com/kthompson/panther/actions/workflows/default.yml)
[![codecov](https://codecov.io/gh/kthompson/panther/branch/main/graph/badge.svg?token=VMRWNJXVP1)](https://codecov.io/gh/kthompson/panther)

Panther is a general-purpose, multi-paradigm programming language encompassing strong typing, functional, generic, and object-oriented (class-based) programming disciplines.


# Example

```scala
def main() = {
    var guess = -1
    var guessCount = 0
    var answer = rnd()

    while (guess != answer) {
        println("Guess the answer:")
        guess = int(read())
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

# Road map

- type aliases (u8 = System.Byte, ...)
- optimize programs before emitting
- floats?