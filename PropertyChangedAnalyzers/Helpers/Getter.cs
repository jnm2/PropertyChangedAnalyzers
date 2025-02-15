namespace PropertyChangedAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class Getter
    {
        internal static bool TrySingleReturned(AccessorDeclarationSyntax getter, [NotNullWhen(true)] out ExpressionSyntax? result)
        {
            if (getter.ExpressionBody is { } getterExpressionBody)
            {
                result = getterExpressionBody.Expression;
                return result != null;
            }

            if (getter.Body is { } body)
            {
                if (body.Statements.Count == 0)
                {
                    result = null;
                    return false;
                }

                if (body.Statements.TrySingle(out var statement))
                {
                    if (statement is ReturnStatementSyntax returnStatement)
                    {
                        result = returnStatement.Expression;
                        return result != null;
                    }
                }

                return ReturnExpressionsWalker.TryGetSingle(getter, out result);
            }

            result = null;
            return false;
        }
    }
}
