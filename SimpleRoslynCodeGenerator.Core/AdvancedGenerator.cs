#pragma warning disable RS2008 // Enable analyzer release tracking

namespace SimpleRoslynCodeGenerator.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


    [Generator]
    public class AdvancedGenerator : ISourceGenerator
    {

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ExampleSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (ExampleSyntaxReceiver)context.SyntaxReceiver ?? throw new Exception("SyntaxReceiver is null");
            foreach (var unit in receiver.Units)
            {
                var model = context.Compilation.GetSemanticModel(unit.SyntaxTree);
                var name = Path.GetFileNameWithoutExtension(unit.SyntaxTree.FilePath) + $".Generated.{Guid.NewGuid()}.cs";
                var content = GetSourceContent(unit, model);
                if (content != null)
                {
                    context.AddSource(name, content.NormalizeWhitespace().ToString());
                }
            }
        }

        private static CompilationUnitSyntax GetSourceContent(CompilationUnitSyntax unit, SemanticModel model)
        {
            unit = (CompilationUnitSyntax)new AnnotationInitializer(model).Visit(unit);
            unit = (CompilationUnitSyntax)new PartialClassGenerator().Visit(unit);
            unit = (CompilationUnitSyntax)new PartialClassContentGenerator().Visit(unit);
            if (unit.DescendantNodes().OfType<TypeDeclarationSyntax>().Any()) return unit;
            return null;
        }

        private class ExampleSyntaxReceiver : ISyntaxReceiver
        {

            public List<CompilationUnitSyntax> Units { get; } = new List<CompilationUnitSyntax>();

            void ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode node)
            {
                if (node is CompilationUnitSyntax unit) Units.Add(unit);
            }

        }

        private class AnnotationInitializer : CSharpSyntaxRewriter
        {

            private SemanticModel Model { get; set; }

            public AnnotationInitializer(SemanticModel model)
            {
                Model = model;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var type = Model.GetDeclaredSymbol(node); // Get symbol of ORIGINAL node
                var members = type.GetMembers().Where(i => i.Kind != SymbolKind.NamedType)
                    .Where(i => i.CanBeReferencedByName)
                    .Where(i => !i.IsImplicitlyDeclared).ToArray();
                node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node); // Pass ORIGINAL node
                return node.WithAdditionalAnnotations(GetAnnotations(type, members));
            }

            private static IEnumerable<SyntaxAnnotation> GetAnnotations(INamedTypeSymbol type, ISymbol[] members)
            {
                yield return new SyntaxAnnotation("Type", type.Name);
                foreach (var member in members)
                {
                    yield return new SyntaxAnnotation("Type.Member", member.Name);
                }
            }
        }

        private class PartialClassGenerator : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                node = CompilationUnit()
                    .WithExterns(node.Externs)
                    .WithUsings(node.Usings)
                    .AddMembers(node.Members.OfType<ClassDeclarationSyntax>().Where(IsPartial).ToArray())
                    .AddMembers(node.Members.OfType<NamespaceDeclarationSyntax>().ToArray());
                return base.VisitCompilationUnit(node);
            }

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                node = NamespaceDeclaration(node.Name)
                    .WithExterns(node.Externs)
                    .WithUsings(node.Usings)
                    .AddMembers(node.Members.OfType<ClassDeclarationSyntax>().Where(IsPartial).ToArray());
                return base.VisitNamespaceDeclaration(node);
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var n = ClassDeclaration(node.Identifier)
                                .WithModifiers(node.Modifiers)
                                .WithTypeParameterList(node.TypeParameterList)
                                .AddMembers(node.Members.OfType<ClassDeclarationSyntax>().Where(IsPartial).ToArray());
                node = CopyAnnotationsFrom(n, node);
                return base.VisitClassDeclaration(node);
            }

            private static T CopyAnnotationsFrom<T>(T node, SyntaxNode other) where T : SyntaxNode
            {
                return other.CopyAnnotationsTo(node);
            }

            private static bool IsPartial(ClassDeclarationSyntax node)
            {
                return node.Modifiers.Any(i => i.Kind() == SyntaxKind.PartialKeyword);
            }
        }

        private class PartialClassContentGenerator : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (node.Modifiers.Any(a => a.IsKind(SyntaxKind.StaticKeyword)))
                {
                    return base.VisitClassDeclaration(node);
                }

                var type = node.GetAnnotations("Type").Single().Data;
                var members = node.GetAnnotations("Type.Member").Select(i => i.Data).ToArray();
                node = node.AddMembers(GetSyntax_ToString(type, members));

                return base.VisitClassDeclaration(node);
            }

            private static MethodDeclarationSyntax GetSyntax_ToString(string type, string[] members)
            {
                var builder = new StringBuilder();
                builder.AppendLine("public string MyToString() {");
                {
                    builder.AppendFormat("return \"{0}\";", GetString(type, members)).AppendLine();
                }
                builder.AppendLine("}");
                return (MethodDeclarationSyntax)ParseMemberDeclaration(builder.ToString());
            }

            private static string GetString(string type, string[] members)
            {
                var text = new StringBuilder();
                text.AppendFormat("Type: {0}", type);
                if (members.Any())
                {
                    text.Append(", ");
                    text.AppendFormat("Members - {0}", string.Join(", ", members));
                }
                return text.ToString();
            }
        }
    }
}