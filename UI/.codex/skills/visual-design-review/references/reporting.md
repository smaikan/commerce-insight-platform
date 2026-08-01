# Visual Review Reporting

## Finding format

```markdown
### [VD-01] User-impact title

- Severity: High | Medium | Low
- Confidence: Confirmed | Likely | Provisional
- Route/state/viewport:
- Category: Hierarchy | Spacing | Alignment | Typography | Color | Surface | Effects | Density | Responsive | State | Content | Performance
- Evidence:
- Impact:
- System/reference rule:
- Smallest coherent correction:
- Verification:
```

Use severity by user impact:

- High: blocks or obscures a primary task, breaks a major viewport, hides critical commerce state, or causes serious contrast/performance/accessibility regression.
- Medium: materially harms scanning, comprehension, consistency, responsive use, or recovery but has a workable path.
- Low: localized polish or small inconsistency with limited task impact.

Do not inflate severity because a style is unfashionable.

## Initial review summary

Present before editing:

- inspected routes, states, and viewports;
- reference/design-system source;
- highest-impact root causes;
- prioritized findings;
- proposed correction clusters;
- unknown or unverified areas;
- expected accessibility/performance risk.

Avoid a long list of repeated symptoms. If twelve cards share the same excessive shadow, report the system-level cause and list affected components.

## After-change summary

Present:

- changes grouped by resolved finding;
- before/after screenshot paths;
- viewports and states rechecked;
- intentional reference deviations and why;
- accessibility/performance checks run;
- console/network or functional regressions found;
- unresolved findings and residual risk.

Use measurable descriptions when possible: heading scale reduced, container aligned, one radius token replaced three variants, above-the-fold product content moved into the initial viewport. Avoid unsupported claims such as “modern,” “premium,” or “100% pixel perfect.”

## Decision rules

- Report before modifying unless the user explicitly asks for an immediate scoped fix and the defect is already evidenced.
- Preserve business behavior and real content while changing presentation.
- Ask for clarification only when competing reference interpretations would materially change the result.
- Do not redesign unrelated pages merely to make the reviewed page consistent; identify broader system work separately.
- Do not add dependencies, fonts, icon sets, illustration packs, or animation libraries without evidence and permission within the requested scope.
