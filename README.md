# Panther

[![Build status](https://img.shields.io/azure-devops/build/automaters/Panther/8)](https://automaters.visualstudio.com/Panther/_build?definitionId=8)
[![Coverage](https://img.shields.io/azure-devops/coverage/automaters/Panther/8)](https://automaters.visualstudio.com/Panther/_build?definitionId=8)
[![Tests](https://img.shields.io/azure-devops/tests/automaters/Panther/8)](https://automaters.visualstudio.com/Panther/_build?definitionId=8)

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
- replace evaluator with emitter?
- optimize programs before emitting
- floats?