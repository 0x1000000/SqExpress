using System;
using System.Collections.Generic;

namespace SqExpress.SyntaxTreeOperations
{
    public interface IExprReader<TNode>
    {
        string GetNodeTypeTag(TNode node);

        bool TryGetSubNode(TNode node, string propertyName, out TNode subNode);

        IEnumerable<TNode>? EnumerateList(TNode node, string propertyName);

        bool TryGetGuid(TNode node, string propertyName, out Guid value);
        bool TryGetBoolean(TNode node, string propertyName, out bool value);
        bool TryGetByte(TNode node, string propertyName, out byte value);
        bool TryGetInt16(TNode node, string propertyName, out short value);
        bool TryGetInt32(TNode node, string propertyName, out int value);
        bool TryGetInt64(TNode node, string propertyName, out long value);
        bool TryGetDecimal(TNode node, string propertyName, out decimal value);
        bool TryGetDouble(TNode node, string propertyName, out double value);
        bool TryGetDateTime(TNode node, string propertyName, out DateTime value);
        bool TryGetString(TNode node, string propertyName, out string value);
        bool TryGetByteArray(TNode node, string propertyName, out IReadOnlyList<byte> value);
    }
}