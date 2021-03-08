namespace SimpleRoslynCodeGenerator.Core
{
    using System.Text;
    using Microsoft.CodeAnalysis;

    [Generator]
    public class SimpleGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var sourceBuilder = new StringBuilder(@"
using System;
namespace SimpleNamespace
{
    public static class SimpleClass
    {
        public static void SimpleMethod() 
        {
            Console.WriteLine(""Roslyn generated code."");
            Console.WriteLine(""The following syntax trees found from root:"");
");
            var syntaxTrees = context.Compilation.SyntaxTrees;
            foreach (var tree in syntaxTrees)
            {
                sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {tree.FilePath}"");");
            }

            sourceBuilder.Append(@"
        }
    }
}");
            context.AddSource("Simple.Generated.cs", sourceBuilder.ToString());

        }
    }

}
