using DomExpression = OCompiler.Analyze.SemanticsV2.Dom.Expression.Expression;

namespace OCompiler.Analyze.SemanticsV2.Dom.Statement.SingleLine;

internal class ReturnStatement : Statement
{
    public DomExpression? Expression;
    public bool IsVoidMethodReturn => Expression == null;

    public ReturnStatement(DomExpression? expression = null)
    {
        Expression = expression;
    }
}