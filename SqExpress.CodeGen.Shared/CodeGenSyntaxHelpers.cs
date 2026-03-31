using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqExpress.CodeGen.Shared
{
    public readonly struct NamedArgument
    {
        public readonly string Name;

        public readonly ExpressionSyntax ArgumentValue;

        public NamedArgument(string name, ExpressionSyntax argumentValue)
        {
            this.Name = name;
            this.ArgumentValue = argumentValue;
        }

        public static implicit operator NamedArgument((string, ExpressionSyntax) tuple)
        {
            return new NamedArgument(tuple.Item1, tuple.Item2);
        }
    }

    internal static class CodeGenSyntaxHelpers
    {
        public static LiteralExpressionSyntax LiteralExpr(bool value)
        {
            return SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        }

        public static LiteralExpressionSyntax LiteralExpr(string value)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
        }

        public static LiteralExpressionSyntax LiteralExpr(int? value)
        {
            if (value.HasValue)
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value.Value));
            }

            return NullLiteral();
        }

        public static LiteralExpressionSyntax NullLiteral()
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        public static ParameterSyntax FuncParameter(string name, string type)
        {
            return SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)).WithType(SyntaxFactory.ParseTypeName(type));
        }

        public static ParameterSyntax FuncParameter(string name)
        {
            return SyntaxFactory.Parameter(SyntaxFactory.Identifier(name));
        }

        public static ArgumentListSyntax ArgumentList(params ExpressionSyntax[] arguments)
        {
            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument)));
        }

        public static ArgumentListSyntax ArgumentList(params NamedArgument[] arguments)
        {
            return SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    arguments.Select(a => SyntaxFactory.Argument(SyntaxFactory.NameColon(a.Name), default, a.ArgumentValue))));
        }

        public static InvocationExpressionSyntax InvokeThis(string method, params ExpressionSyntax[] arguments)
        {
            return arguments.Length < 1
                ? SyntaxFactory.InvocationExpression(MemberAccessThis(method))
                : SyntaxFactory.InvocationExpression(MemberAccessThis(method), ArgumentList(arguments));
        }

        public static InvocationExpressionSyntax InvokeThis(string method, params NamedArgument[] arguments)
        {
            return arguments.Length < 1
                ? SyntaxFactory.InvocationExpression(MemberAccessThis(method))
                : SyntaxFactory.InvocationExpression(MemberAccessThis(method), ArgumentList(arguments));
        }

        public static InvocationExpressionSyntax Invoke(this ExpressionSyntax host, params ExpressionSyntax[] arguments)
        {
            return arguments.Length < 1
                ? SyntaxFactory.InvocationExpression(host)
                : SyntaxFactory.InvocationExpression(host, ArgumentList(arguments));
        }

        public static AssignmentExpressionSyntax AssignmentThis(string property, ExpressionSyntax right)
        {
            return SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                MemberAccess(SyntaxFactory.ThisExpression(), property),
                right);
        }

        public static MemberAccessExpressionSyntax MemberAccessThis(string member)
        {
            return MemberAccess(SyntaxFactory.ThisExpression(), member);
        }

        public static MemberAccessExpressionSyntax MemberAccess(string expression, string member)
        {
            return MemberAccess(SyntaxFactory.IdentifierName(expression), member);
        }

        public static MemberAccessExpressionSyntax MemberAccess(this ExpressionSyntax expression, string member, bool addSuppressNullable = false)
        {
            if (addSuppressNullable)
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, expression),
                    SyntaxFactory.IdentifierName(member));
            }

            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                SyntaxFactory.IdentifierName(member));
        }

        public static MemberAccessExpressionSyntax MemberAccessGeneric(this ExpressionSyntax expression, string member, string g1)
        {
            var genericName = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier(member),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(new[] { (TypeSyntax)SyntaxFactory.IdentifierName(g1) })));

            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                genericName);
        }

        public static SyntaxTokenList Modifiers(SyntaxKind token1)
        {
            return SyntaxFactory.TokenList(SyntaxFactory.Token(token1));
        }

        public static SyntaxTokenList Modifiers(SyntaxKind token1, SyntaxKind token2)
        {
            return SyntaxFactory.TokenList(SyntaxFactory.Token(token1), SyntaxFactory.Token(token2));
        }

        public static SyntaxTokenList Modifiers(SyntaxKind token1, SyntaxKind token2, SyntaxKind token3)
        {
            return SyntaxFactory.TokenList(SyntaxFactory.Token(token1), SyntaxFactory.Token(token2), SyntaxFactory.Token(token3));
        }

        public static T? FindParentOrDefault<T>(this SyntaxNode node) where T : SyntaxNode
        {
            SyntaxNode? parent = node.Parent;
            while (parent != null)
            {
                if (parent is T result)
                {
                    return result;
                }

                parent = parent.Parent;
            }

            return null;
        }
    }
}
