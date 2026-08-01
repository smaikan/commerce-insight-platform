# Visual Language and AI-Design Smells

## Build the system inventory

Identify before judging:

- content container widths and grids;
- spacing scale;
- type families, sizes, weights, leading, and tracking;
- foreground, background, border, action, muted, and semantic colors;
- radius levels and their intended component roles;
- border and elevation levels;
- control heights, icon sizes, and responsive variants;
- shared primitives and local one-off variants.

Treat repeated existing choices as evidence, not automatically as good design. Prefer explicit tokens over scattered arbitrary values.

## Review hierarchy

- Confirm the first scan answers: where am I, what matters, and what can I do?
- Keep one dominant page heading; reduce headings that overpower the actual task.
- Use weight, size, spacing, position, and contrast together rather than making everything large or colorful.
- Keep secondary information subordinate without making it illegible.
- Put related label/value/action groups together and separate unrelated groups.
- Avoid multiple equally prominent calls to action.

## Review surfaces

A card is justified when it represents a selectable/repeatable entity, isolates an interaction, or separates a meaningful surface layer. It is usually unnecessary for plain prose, a heading group, one metric, or every consecutive section.

Define a small surface vocabulary, for example:

- page/base surface;
- bordered grouped surface;
- raised overlay;
- selected or semantic state.

Do not assign a unique radius and shadow to each component. Pills are for compact tokens, filters, tags, or intentionally pill-shaped controls—not every button, image, panel, and input.

## Review effects

Flag a gradient, blur, glow, glass surface, or animation when:

- it repeats without hierarchy;
- it obscures content or weakens contrast;
- it exists only to make an empty section look designed;
- several effects stack on the same surface;
- it creates inconsistent light sources/elevation;
- it increases rendering cost without helping comprehension;
- it resembles a template convention more than the product identity.

Retain an effect when it has a clear brand or state role and remains restrained. Replace unnecessary effects with structure, typography, color, border, or whitespace.

## Review typography and spacing

- Use a deliberate type scale and avoid near-duplicate sizes.
- Keep body text comfortable and line lengths bounded.
- Reserve uppercase/wide tracking for short labels, not paragraphs or every heading eyebrow.
- Use consistent vertical rhythm; repeated sections should not invent their own padding.
- Reduce hero height when the primary content falls below the first viewport without a product reason.
- Align optical edges, text baselines, icons, prices, controls, and table columns.

## Review color and emphasis

- Limit brand emphasis to a controlled palette.
- Reserve red, amber, green, and similar semantic colors for actual meaning.
- Verify default, hover, focus, selected, disabled, error, and success contrast.
- Avoid several competing accent colors on one page.
- Do not use color, glow, gradient, badge, and shadow simultaneously to express one level of emphasis.

## Detect hollow content

Flag and remove unless verified:

- invented sales/customer/uptime statistics;
- fake testimonials, ratings, or partner logos;
- vague superlatives and slogans that do not help selection or task completion;
- repeated explanatory prose that masks missing functionality;
- demo sections unrelated to the route's purpose;
- placeholder names, prices, stock, or charts presented as real data.
