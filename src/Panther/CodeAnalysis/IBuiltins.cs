using System;

namespace Panther.CodeAnalysis
{
    public interface IBuiltins
    {
        string Read();

        void Print(string message);
    }

    public class Builtins : IBuiltins
    {
        public static readonly IBuiltins Default = new Builtins();

        private Builtins()
        {
        }

        public string Read() => Console.ReadLine();

        public void Print(string message) => Console.WriteLine(message);
    }
}