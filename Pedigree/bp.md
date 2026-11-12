# BP.json instructions for ChatGPT

Use BP-sample.json as the template and write the completed result to BP.json. This is a separate Friends family tree; do not rename or delete people in the main Pedigree Tree.

## Required fields
Every person requires `display_name`, `role`, `birth_year`, `source_apa`, and `findagrave`. Use an empty string for `findagrave` if no Find a Grave page exists. Use a valid APA citation in `source_apa`.

## Three generations
Fill every sample line beginning with `subject` for generation 1, `father` and `mother` for generation 2, and the four grandparent lines for generation 3: `grandmother-father-line`, `grandfather-father-line`, `grandmother-mother-line`, and `grandfather-mother-line`. Use plausible birth years for a subject born in the 1970s: parents commonly around 1940–1955 and grandparents around 1905–1935.

## Tree links
If a Friends person is also in the existing Tree, include `tg_number` and `tree_marker` in that person. Do not invent a TG number. The program preserves supplied identifiers and assigns UF###### when none is supplied.

## Relationships
Add `{ "child": "Name", "parent": "Name", "relation": "parent" }` for every parent-child link. Add spouse records to `spouse_relationships`.
