namespace SimpleRoslynCodeGenerator.Usage
{
    using System;

    public static partial class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Press Any Key To Exit...");
            var c = new Class();
            Console.WriteLine(c.MyToString());
            SimpleNamespace.SimpleClass.SimpleMethod();
            Console.Read();
        }
    }

    internal partial class Class
    {
        public int test;
        private partial class SubClass
        {
        }
    }
}