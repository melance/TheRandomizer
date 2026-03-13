
# Core Operations

## Resolve(ItemListName name) → ItemList

## Render(String template) → String

# Functions

## Select(ItemListName name) → String
- Resolves `name` to an `ItemList`    
- Selects a single `Item` using `Weight`    
- Renders the selected `Item.Content` as a template    
- Returns the rendered string

## Select(ItemList list) → String
- Selects a single `Item` using `Weight`    
- Renders `Item.Content` as a template    
- Returns the rendered string

## Union(ItemListName a, ItemListName b, ...) → ItemList
- Resolves each name to an ItemList    
- Returns a single ItemList containing all Items from all inputs    
- Preserves per-item weights
## OneOf(ItemListName a, ItemListName b, ...) → ItemList
- Selects one of the named lists (each name equally likely, unless you later add weighting)    
- Returns the selected ItemList (items unchanged)

## Pick(String a, String b, ...) → String
- Chooses one string uniformly    
- Returns it (no template rendering unless you explicitly pass it through Render/Select elsewhere)

## Concat(String a, String b, ...) → String
- Concatenates strings into one string

## From(String key) → ItemList
- Treats `key` as an ItemListName and resolves it to an ItemList    
- If missing: returns empty list (recommended) or throws (strict mode)

## KeyOf(ItemListName a, ItemListName b, ...) → String
- Returns `Concat(Select(a), Select(b), ...)`    
- I.e., select rendered strings from each named list and concatenate them into a composite key