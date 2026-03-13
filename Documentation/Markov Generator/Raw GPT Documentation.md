Below is user-facing documentation you can give to anyone authoring content for your Markov generator. It focuses on **how to use the language**, not the internal implementation.

You can drop this into a README or help page with minimal editing.

---

Assignment Generator Authoring Guide

Overview

The Assignment generator produces text by selecting entries from named lists (“line items”) and evaluating expressions embedded inside square brackets.

Everything outside of square brackets is treated as literal text.

Example:

```
Hello [World]
```

If a list named `World` exists, one item from that list will be selected and inserted.

---

Line Items

A generator definition contains named lists of line items.

Example:

```
Start:
  - Hello [Name]

Name:
  - Alice
  - Bob
```

When generation begins, the generator starts from the `Start` list.

Each line item has:

- Content — the text to render
    
- Weight — optional numeric value controlling how often it is selected
    

Example:

```
Name:
  - Content: Alice
    Weight: 2
  - Content: Bob
    Weight: 1
```

Alice will be chosen twice as often as Bob.

---

Bracket Expressions

Square brackets `[ ... ]` contain expressions that are evaluated during generation.

There are two main forms:

1. List selection shorthand
    

```
[Name]
```

This is equivalent to:

```
[Select(Name)]
```

One item is chosen from the list `Name`.

2. Full expressions
    

Anything more complex inside brackets is treated as an expression:

```
[Concat("Hello ", Select(Name))]
```

---

Variables

Variables store primitive values:

- String
    
- Number (decimal)
    
- Boolean
    

Variables are global across the entire generation process.

Reading a variable

Use `@`:

```
[@var]
```

Assignment (no output)

```
[var := "Hello"]
```

This stores `"Hello"` in `var` but outputs nothing.

Assignment with output

```
[var :> "Hello"]
```

This stores the value and also outputs it.

Example:

```
[var :> Select(Name)]
```

---

Functions

Functions use standard syntax:

```
FunctionName(arg1, arg2, ...)
```

Function names are case-insensitive.

Select

Selects and renders an item from a list.

```
Select(Name)
```

You usually don’t need this because `[Name]` is shorthand.

Union

Combines multiple lists into one pool.

```
Select(Union(Elf, Human))
```

Operator shorthand:

```
Elf & Human
```

OneOf

Randomly chooses one list, then selects from it.

```
Select(OneOf(Elf, Human))
```

Operator shorthand:

```
Elf | Human
```

Concat

Concatenates strings.

```
Concat("Hello ", "World")
```

Pick

Chooses one string from the provided values.

```
Pick("Red", "Green", "Blue")
```

From

Treats a string as a list name.

Useful for dynamic list lookup.

```
Select(From("MaleElf"))
```

KeyOf

Builds a list name by selecting from other lists and concatenating results.

```
Select(From(KeyOf(Gender, Species)))
```

Example result:

```
Gender -> Male
Species -> Elf
KeyOf -> MaleElf
```

---

Operators

The language includes several operator shortcuts.

Union operator

```
Elf & Human
```

Equivalent to:

```
Union(Elf, Human)
```

OneOf operator

```
Elf | Human
```

Equivalent to:

```
OneOf(Elf, Human)
```

Weight operator

```
List * 3
```

Used to increase relative weight (if enabled in your generator configuration).

---

Numbers

Numbers support decimals:

```
10
3.14
0.5
```

---

Strings

Strings use double quotes:

```
"Hello"
"Hello World"
```

Escape sequences:

```
\"  quote
\\  backslash
```

---

Escaping Brackets

To output literal brackets, escape them with a backslash:

```
\[Hello\]
```

Produces:

```
[Hello]
```

---

Examples

Basic selection

```
Start:
  - Hello [Name]

Name:
  - Alice
  - Bob
```

Variables

```
Start:
  - [g :> Select(Gender)] is chosen.
  - Again: [@g]

Gender:
  - Male
  - Female
```

Output example:

```
Male is chosen.
Again: Male
```

Dynamic list selection

```
Start:
  - [Select(From(KeyOf(Gender, Species)))]

Gender:
  - Male
  - Female

Species:
  - Elf
  - Dwarf

MaleElf:
  - Legolas
```

---

Error Behavior

The generator will throw an error if:

- A referenced list does not exist
    
- A list is empty
    
- Total weight of a list is zero
    
- A variable is read before being assigned
    
- Brackets are not closed
    

Errors are intentional to help catch mistakes early.

---

Best Practices

- Use variables when you need consistency across multiple selections
    
- Prefer shorthand `[Name]` for readability
    
- Use `:>` when you want to assign and output in one step
    
- Keep list names simple and descriptive
    
- Use `KeyOf` + `From` for compositional generation
    

---

Quick Reference

Selection:

```
[ListName]
```

Variable read:

```
[@var]
```

Assign:

```
[var := value]
```

Assign + output:

```
[var :> value]
```

Union:

```
A & B
```

OneOf:

```
A | B
```

---

