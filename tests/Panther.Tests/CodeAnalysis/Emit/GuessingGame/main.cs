using System;
using static Panther.Predef;

public static partial class Program
{
    public static void main()
    {
        var guess = -1;
        var guessCount = 0;
        var answer = 27;
        while (guess != answer)
        {
            println("Guess the answer:");
            guess = Convert.ToInt32(readLine());
            guessCount = guessCount + 1;
            if (guess > answer)
            {
                println("Lower");
            }
            else if (guess < answer)
            {
                println("Higher");
            }
            else
            {
                println("Correct: " + Convert.ToString(answer));
                println(Convert.ToString(guessCount) + " total guesses");
            }
            ;
        }
        ;
    }
}
