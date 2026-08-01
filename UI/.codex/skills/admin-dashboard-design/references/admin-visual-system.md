# Admin Visual System

Use shared semantic tokens from `ecommerce-design-system` when available. Until then, define these roles once in the theme rather than hard-coding them across components.

## Color direction

Use a neutral light shell with a restrained blue accent. A suitable starting scale is:

- primary/action: blue 600;
- primary hover/pressed: blue 700;
- selected navigation/row background: blue 50;
- selected border: blue 200;
- focus ring: blue 400 with sufficient offset;
- links: blue 700 on light surfaces;
- page background: cool neutral 50–100;
- surface: white;
- borders: cool neutral 200;
- primary text: cool neutral 900;
- secondary text: cool neutral 600.

Validate real rendered contrast. Do not use the accent scale as success/info interchangeably. Keep success, warning, danger, and info semantic roles distinct.

Avoid gradients, glow, glassmorphism, blue-tinted panels everywhere, and dark-blue full sidebars unless later brand evidence requires one.

## Density and type

- Page heading: restrained 20–24 px, semibold.
- Section heading: 15–17 px, semibold.
- Body/control text: 14 px by default.
- Supporting/table meta: 12–13 px when contrast and readability remain sufficient.
- Desktop compact control height: 32–36 px; primary form fields may use 40 px.
- Mobile/touch controls: preserve at least a comfortable 44 px target area.
- Table rows: normally 48–56 px; increase only for richer two-line content.
- Sidebar/topbar: approximately 240–256 px wide and 52–56 px high as initial proportions, then validate against content.

Use one type family and a small, explicit type scale. Use tabular numerals for money, quantities, and report columns when available.

## Spacing, radius, and elevation

- Build from a 4 px spacing base with common steps such as 4, 8, 12, 16, 20, 24, and 32.
- Use 6–8 px radii for controls and compact items.
- Use 10–12 px radii for major grouped surfaces and overlays.
- Use full pills only for status/tag/filter tokens or intentionally pill-shaped compact controls.
- Use borders for panels and tables; keep page surfaces visually quiet.
- Reserve shadow for menus, popovers, drawers, and dialogs. Avoid large card shadows.

## Emphasis rules

- One primary action per page or bounded form region.
- Secondary actions use neutral buttons; destructive actions appear red only at the decision point.
- Active navigation uses a blue cue plus weight/contrast, not glow or a large filled block.
- Status badges use semantic color and compact geometry; plain metadata stays plain text.
- Icons support recognition but never replace ambiguous labels for primary operations.

## State rules

- Focus: visible on every interactive element and not clipped by overflow.
- Error: field association, concise correction, and error summary for long forms.
- Disabled: readable; communicate reason when lifecycle or permission blocks the action.
- Loading: stable geometry, restrained skeletons, `aria-busy`, and no endless decorative shimmer.
- Selected: detectable without color alone through checkbox/state/icon/border or weight.
- Hover: optional reinforcement, never the only way to discover an action.
