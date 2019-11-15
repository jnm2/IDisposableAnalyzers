﻿namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateAndAssignFieldFix))]
    [Shared]
    internal class CreateAndAssignFieldFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.IDISP001DisposeCreated.Id,
            Descriptors.IDISP004DoNotIgnoreCreated.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == Descriptors.IDISP001DisposeCreated.Id &&
                    node.TryFirstAncestorOrSelf<LocalDeclarationStatementSyntax>(out var localDeclaration) &&
                    localDeclaration is { Declaration: { Type: { } type, Variables: { Count: 1 } variables }, Parent: BlockSyntax { Parent: ConstructorDeclarationSyntax _ } } &&
                    variables[0] is { Initializer: { } } local &&
                    localDeclaration.TryFirstAncestor(out TypeDeclarationSyntax containingType))
                {
                    context.RegisterCodeFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignField(editor, cancellationToken),
                        "Create and assign field.",
                        diagnostic);

                    void CreateAndAssignField(DocumentEditor editor, CancellationToken cancellationToken)
                    {
                        var fieldAccess = editor.AddField(
                            containingType,
                            local.Identifier.ValueText,
                            Accessibility.Private,
                            DeclarationModifiers.ReadOnly,
                            editor.SemanticModel.GetTypeInfoSafe(type, cancellationToken).Type,
                            cancellationToken);

                        editor.ReplaceNode(
                            localDeclaration,
                            (x, g) => g.ExpressionStatement(
                                           g.AssignmentStatement(fieldAccess, local.Initializer.Value))
                                       .WithTriviaFrom(x));
                    }
                }
                else if (diagnostic.Id == Descriptors.IDISP004DoNotIgnoreCreated.Id &&
                         node.TryFirstAncestorOrSelf<ExpressionStatementSyntax>(out var statement) &&
                         statement.TryFirstAncestor<ConstructorDeclarationSyntax>(out var ctor))
                {
                    context.RegisterCodeFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignField(editor, cancellationToken),
                        "Create and assign field.",
                        diagnostic);

                    void CreateAndAssignField(DocumentEditor editor, CancellationToken cancellationToken)
                    {
                        var fieldAccess = editor.AddField(
                            (TypeDeclarationSyntax)ctor.Parent,
                            "disposable",
                            Accessibility.Private,
                            DeclarationModifiers.ReadOnly,
                            IDisposableFactory.SystemIDisposable,
                            cancellationToken);

                        _ = editor.ReplaceNode(
                            statement,
                            x => SyntaxFactory.ExpressionStatement(
                                           SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, fieldAccess, x.Expression))
                                       .WithTriviaFrom(x));
                    }
                }
            }
        }
    }
}
