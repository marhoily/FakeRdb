﻿namespace FakeRdb;

public static class X
{
    public static IExpression Convert(this IR.IExpression arg)
    {
        return arg switch
        {
            IR.BinaryExp binaryExp => new BinaryExpression(binaryExp.Op, Convert(binaryExp.Left), Convert(binaryExp.Right), binaryExp.Alias),
            IR.BindExp bindExp => new ValueExpression(bindExp.Value, bindExp.Value.GetTypeAffinity(), bindExp.ParameterName),
            IR.AggregateExp callExp => new AggregateFunctionCallExpression(callExp.Function, callExp.Args.Select(Convert).ToArray(), callExp.OriginalText),
            IR.ScalarExp callExp => new ScalarFunctionCallExpression(callExp.Function, callExp.Args.Select(Convert).ToArray(), callExp.OriginalText),
            IR.ColumnExp columnExp => new ProjectionExpression(columnExp.Value),
            IR.InExp inExp => new InExpression(inExp.Needle.Convert(), inExp.Haystack),
            IR.LiteralExp literalExp => new ValueExpression(literalExp.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }


}