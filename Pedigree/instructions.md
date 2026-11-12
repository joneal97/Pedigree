# Pedigree ChatGPT Update Instructions

The exported JSON is a read-only copy of the current family tree. Do not delete or rename existing people.

## How to add people
Add objects to `updates.people`. Each object requires `display_name`, `source_apa`, and `findagrave`. Use an empty string for `findagrave` when no Find a Grave page exists. You may provide `tg_number`; otherwise the program assigns a permanent `UF######` identifier.

## How to update verification
Add `{ "display_name": "Name", "verify": "11/11/2026" }` to `updates.verify_updates`. Existing names and TG identifiers are preserved.

## How to preserve relationships
Use `updates.relationships` with `{ "child": "Child", "parent": "Parent", "relation": "parent" }`. Use `updates.spouse_relationships` with `{ "person1": "Dudley McCabe", "person2": "Linda McCabe" }`. Include parents of newly added people as additional people and links in the same package.

## Example
```json
{
  "updates": {
    "people": [
      { "display_name": "Linda McCabe", "source_apa": "Author, A. (2026). Record title. Archive.", "findagrave": "" },
      { "display_name": "Kerrie McCabe", "source_apa": "Author, A. (2026). Record title. Archive.", "findagrave": "" }
    ],
    "relationships": [
      { "child": "Kerrie McCabe", "parent": "Dudley McCabe", "relation": "parent" },
      { "child": "Kerrie McCabe", "parent": "Linda McCabe", "relation": "parent" }
    ],
    "spouse_relationships": [
      { "person1": "Dudley McCabe", "person2": "Linda McCabe", "relation": "spouse" }
    ],
    "verify_updates": [
      { "display_name": "Sarah McAvoy", "verify": "11/11/2026" }
    ]
  }
}
```
