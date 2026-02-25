namespace TheRandomizer.Assignment.Parser;

public enum TokenTypes
{
    EOF,                    // End of File
    Identifier,             // A LineItem Identifier
    Number,                 // A number literal
    String,                 // A string literal
    Boolean,                // A boolean value
    Variable,               // A variable @name
    OpenParenthesis,        // (
    CloseParenthesis,       // )
    Comma,                  // ,
    Union,                  // &
    OneOf,                  // |
    KeyOf,                  // +
    Repeat,                 // ^
    Weight,                 // *
    Assignment,             // :=
    AssignAndEmit,          // :>
    EqualTo,                // ==
    NotEqualTo,             // !=
    LessThan,               // <
    LessThanOrEqual,        // <=
    GreaterThan,            // >
    GreaterThanOrEqual,     // >=
    Negate,                 // !
    And,                    // &&
    Or,                     // ||
}
