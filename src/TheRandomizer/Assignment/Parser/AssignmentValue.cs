namespace TheRandomizer.Assignment.Parser;

internal abstract record AssignmentValue;

internal sealed record IntegerValue(Int32 Value) : AssignmentValue;
internal sealed record DecimalValue(Decimal Value) : AssignmentValue;
internal sealed record StringValue(String Value) : AssignmentValue;
internal sealed record BooleanValue(Boolean Value) : AssignmentValue;