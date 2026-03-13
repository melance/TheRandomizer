# Overview

The Assignment generator produces text by selecting entries from named lists (“line items”) and evaluating expressions embedded inside square brackets.

Everything outside of square brackets is treated as literal text.

Example:

```
Hello [World]
```

If a list named `World` exists, one item from that list will be selected and inserted.

# Line Item
A single line item contains two pieces of data:

1. An option weight used when selecting a line item from a group
2. Text that is returned after evaluation

Example:

```json
{
	"Weight": "20",
	"Content": "Hello [Location]!"
}
```

# Line Item List
A group of line items, generally select from during generation.

Example:

```json
"Location":
[
	{
		"Weight": 2,
		"Content": "World"
	},
	{
		"Content": "Universe"
	}
]
```

# Expression
An expression is found in the content of a Line Item and is wrapped in square brackets.

## List Expression
The list expression is a shorthand that allows quickly selecting from a Line Item List. It consists of the name of the Line Item List to pull from in square brackets.

Example:

```
[Location]
```

In a Line Item content, this expression will be replaced by a randomly selected Line Item from the Location Line Item List.

## Functions
The expression processor is built around functions. The standard syntax for functions is `function(arg1,arg2,...)` There are a number of functions available:

### Select
Selects an item from a Line Item List and renders it to the output.

Signature:
```
Select(Name)
```

If this is the only function in an expression it can be eliminated as `[Name]` is shorthand for `[Select(Name)]`.

### Concat
Concatenates two string into a single string.

Signature:
```
Concat(String, String, ...)
```

Example:
```
Concat("Hello", " ", "World")

Results → "Hello World"
```

### From
Treats a string as a List Name. Useful for dynamic list lookup.

Signature:
```
From(String)
```

Example:
```
Select(From("species"))
```

### KeyOf
Builds a list name by selecting from other lists and concatenating the results.

Signature:
```
KeyOf(ItemListName, ItemListName, ...)
```

Example:
```
Select(From(KeyOf(Gender,Species)))

Result:

Gender → Male
Species → Elf
KeyOf → MaleElf
```

### OneOf
Randomly chooses one list, then selects from it.

Signature:
```
OneOf(ItemListName, ItemListName, ...)
```

Example:
```
Select(OneOf(Elf,Human))
```

### Pick
Chooses one string from the provided values.

Signature:
```
Pick(String,String,...)
```

Example:
```
Pick("Red","Green","Blue")
```

### Union
Combines multiple lists into a single list.

Signature:
```
Union(ItemListName, ItemListName, ...)
```

```
Union(Elf,Human)
```

### Weight
Adds weight to a line item list or line item list dictionary.

Signature:
```
Weight(ItemListName,Number)
```

## Operators
There are several operators that act as shorthand for functions.

### Union Operator : &
```
Elf & Human
```

Equivalent to:

```
Union(Elf,Human)
```

### OneOf Operator : |
```
Elf | Human
```

Equivalent to:
```
OneOf(Elf,Human)
```

### KeyOf Operator : +
```
Species + Gender
```

Equivalent to:
```
KeyOf(Species,Gender)
```

## Variables
In addition to functions, the processor provides the ability to store values in variables. These variables can then be used later in the expression or in other expressions, including those found in other Line Items.

## Names
Variable names must start with a letter and can include letters, numbers, underscores, and periods.  Note that two periods cannot appear subsequent to one another.

### Types
Variables can only contain primitive types. Those types are:

- String
- Number (Integer)
- Boolean

### Assignment
Assigning a value to a variable can be done in one of the two following ways:

1. `var := value` : this will assign value to the variable `var` and emits nothing.
2. `var :> value` : this will assign value to the variable `var` and emits value.

### Reference
Referencing a variable is done by prefacing the name of the variable with the @ symbol.

Example:
```
[@var]
```

