﻿namespace PropertyChangedAnalyzers
{
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class PropertyDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.INPC010GetAndSetSame,
            Descriptors.INPC015PropertyIsRecursive,
            Descriptors.INPC017BackingFieldNameMisMatch,
            Descriptors.INPC019GetBackingField,
            Descriptors.INPC020PreferExpressionBodyAccessor,
            Descriptors.INPC021SetBackingField);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.PropertyDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is PropertyDeclarationSyntax propertyDeclaration &&
                context.ContainingSymbol is IPropertySymbol property)
            {
                using (var walker = ReturnExpressions(propertyDeclaration))
                {
                    foreach (var returnValue in walker.ReturnValues)
                    {
                        if (IsProperty(returnValue))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.INPC015PropertyIsRecursive, returnValue.GetLocation(), "Getter returns property, infinite recursion"));
                        }
                    }

                    if (walker.ReturnValues.TrySingle(out var single))
                    {
                        if (single.IsEither(SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.IdentifierName) &&
                            MemberPath.TrySingle(single, out var path) &&
                            context.SemanticModel.TryGetSymbol(path, context.CancellationToken, out IFieldSymbol? backingField))
                        {
                            if (!HasMatchingName(backingField, property))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.INPC017BackingFieldNameMisMatch, path.GetLocation()));
                            }

                            if (propertyDeclaration.TryGetSetter(out var setAccessor))
                            {
                                using var mutationWalker = MutationWalker.Borrow(setAccessor, SearchScope.Member, context.SemanticModel, context.CancellationToken);
                                if (mutationWalker.IsEmpty)
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.INPC021SetBackingField, setAccessor.GetLocation()));
                                }
                            }
                        }

                        if (single is LiteralExpressionSyntax &&
                            propertyDeclaration.TryGetSetter(out var set) &&
                            Setter.TryFindSingleMutation(set, context.SemanticModel, context.CancellationToken, out var fieldAccess))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.INPC019GetBackingField,
                                    single.GetLocation(),
                                    additionalLocations: new[] { fieldAccess.GetLocation() }));
                        }
                    }
                }

                if (propertyDeclaration.TryGetSetter(out var setter))
                {
                    if (ShouldBeExpressionBody(setter))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.INPC020PreferExpressionBodyAccessor, setter.GetLocation()));
                    }

                    if (Property.GetsAndSetsSame(propertyDeclaration, context.SemanticModel, context.CancellationToken, out _, out _) == false)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.INPC010GetAndSetSame, propertyDeclaration.Identifier.GetLocation()));
                    }

                    using (var assignmentWalker = AssignmentWalker.Borrow(setter))
                    {
                        if (assignmentWalker.Assignments.TryFirst(x => IsProperty(x.Left) && !x.Parent.IsKind(SyntaxKind.ObjectInitializerExpression), out var recursiveAssignment))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.INPC015PropertyIsRecursive, recursiveAssignment.Left.GetLocation(), "Setter assigns property, infinite recursion"));
                        }
                    }

                    if (propertyDeclaration.TryGetGetter(out var getter))
                    {
                        if (ShouldBeExpressionBody(getter))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.INPC020PreferExpressionBodyAccessor, getter.GetLocation()));
                        }
                    }
                }

                bool IsProperty(ExpressionSyntax expression)
                {
                    if (property.ExplicitInterfaceImplementations.Any())
                    {
                        return false;
                    }

                    return expression switch
                    {
                        IdentifierNameSyntax { Identifier: { ValueText: { } name } } => property.Name == name,
                        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Name: { Identifier: { ValueText: { } name } } } => property.Name == name,
                        _ => false
                    };
                }
            }
        }

        private static ReturnExpressionsWalker ReturnExpressions(PropertyDeclarationSyntax property)
        {
            return property switch
            {
                { ExpressionBody: { Expression: { } } expressionBody } => ReturnExpressionsWalker.Borrow(expressionBody),
                { AccessorList: { } }
                    when property.TryGetGetter(out var getter) => ReturnExpressionsWalker.Borrow(getter),
                _ => ReturnExpressionsWalker.Empty(),
            };
        }

        private static bool HasMatchingName(IFieldSymbol backingField, IPropertySymbol property)
        {
            if (backingField.IsStatic || backingField.IsConst)
            {
                return true;
            }

            if (property.ExplicitInterfaceImplementations.TryFirst(out var explicitProperty))
            {
                return HasMatchingName(backingField, explicitProperty);
            }

            if (backingField.Name.Length < property.Name.Length)
            {
                return false;
            }

            var diff = backingField.Name.Length - property.Name.Length;
            for (var pi = property.Name.Length - 1; pi >= 0; pi--)
            {
                var fi = pi + diff;
                if (pi == 0)
                {
                    if (char.ToLower(property.Name[pi], CultureInfo.InvariantCulture) != backingField.Name[fi])
                    {
                        return false;
                    }

                    return fi switch
                    {
                        0 => true,
                        1 => backingField.Name[0] == '_' ||
                             backingField.Name[0] == '@',
                        _ => false,
                    };
                }

                if (property.Name[pi] != backingField.Name[fi])
                {
                    if (pi == 0 ||
                        !char.IsUpper(property.Name[pi - 1]) ||
                        char.ToUpper(backingField.Name[fi], CultureInfo.InvariantCulture) != property.Name[pi])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool ShouldBeExpressionBody(AccessorDeclarationSyntax accessor)
        {
            return accessor.Body is { Statements: { Count: 1 } statements } &&
                   statements[0].IsEither(SyntaxKind.ReturnStatement, SyntaxKind.ExpressionStatement);
        }
    }
}
