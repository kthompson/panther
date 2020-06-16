using System;

namespace Panther.CodeAnalysis
{
    public interface IBuiltins
    {
        string ReadLine();

        void Print(object message);
        void Println(object message);
    }

    public class Builtins : IBuiltins
    {
        public static readonly IBuiltins Default = new Builtins();

        private Builtins()
        {
        }

        public string ReadLine() => Console.ReadLine();
        public void Print(object message) => Console.Write(message);
        public void Println(object message) => Console.WriteLine(message);
        public void Print(string message) => Console.WriteLine(message);
    }
}