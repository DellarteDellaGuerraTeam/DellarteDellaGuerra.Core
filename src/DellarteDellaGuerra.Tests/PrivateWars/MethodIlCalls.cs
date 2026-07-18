using System.Reflection;
using System.Reflection.Emit;

namespace DellarteDellaGuerra.Tests.PrivateWars;

internal static class MethodIlCalls
{
    private static readonly IReadOnlyDictionary<short, OpCode> OpCodesByValue =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opCode => opCode.Value);

    public static int Count(MethodInfo method, MethodInfo calledMethod)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray()
            ?? throw new InvalidOperationException($"{method} has no IL body.");
        var count = 0;

        for (var offset = 0; offset < il.Length;)
        {
            var first = il[offset++];
            var value = first == 0xfe ? (short)(0xfe00 | il[offset++]) : (short)first;
            var opCode = OpCodesByValue[value];

            if (opCode.OperandType is OperandType.InlineMethod)
            {
                var token = BitConverter.ToInt32(il, offset);
                var resolved = method.Module.ResolveMethod(
                    token,
                    method.DeclaringType?.GetGenericArguments(),
                    method.GetGenericArguments());
                if (resolved == calledMethod) count++;
            }

            offset += OperandSize(opCode.OperandType, il, offset);
        }

        return count;
    }

    private static int OperandSize(OperandType operandType, byte[] il, int offset) => operandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineField or
            OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString or
            OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + BitConverter.ToInt32(il, offset) * 4,
        _ => throw new NotSupportedException($"Unsupported IL operand type {operandType}.")
    };
}
