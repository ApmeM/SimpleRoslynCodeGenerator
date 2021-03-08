# SimpleRoslynCodeGenerator
Example of how to build code generator base on roslyn

Repository contains 2 examples of code generatiors:
- [Simple](https://github.com/ApmeM/SimpleRoslynCodeGenerator/blob/main/SimpleRoslynCodeGenerator.Core/SimpleGenerator.cs) - Example based on string and some simple iteration through SyntaxTree. Can be easily used in case you need completely new functionality that is created in parallel to code you extend.
- [Advanced](https://github.com/ApmeM/SimpleRoslynCodeGenerator/blob/main/SimpleRoslynCodeGenerator.Core/AdvancedGenerator.cs) - Example based on syntax tree visitors pattern using roslyn ISyntaxReceiver and CSharpSyntaxRewriter that allow to change classes that are already exists in consumer code.

Usage example located in [Program.cs](https://github.com/ApmeM/SimpleRoslynCodeGenerator/blob/main/SimpleRoslynCodeGenerator.Usage/Program.cs) file. Visual studio can mark generated methods as unknown, but compilation will be succeeded.
