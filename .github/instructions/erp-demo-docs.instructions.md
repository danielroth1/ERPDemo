---
applyTo:
  - "docs/erp-demo/**"
  - "src/data/projects/ERPDemo/docs/**"
---

# ERP Demo documentation authoring

These instructions apply when writing or editing ERP Demo documentation pages, either in the authoring repository (`docs/erp-demo/`) or in the CV website repository (`src/data/projects/ERPDemo/docs/`).

The files are AsciiDoc (`.adoc`) pages that get published on a React CV portfolio website.

---

## File naming and placement

- Each page is a single `.adoc` file.
  - Authoring repo: `docs/erp-demo/docs/<slug>.adoc`
  - CV website repo: `src/data/projects/ERPDemo/docs/<slug>.adoc`
- The `diagrams/` folder lives alongside the docs (e.g. `docs/erp-demo/docs/diagrams/`).
- The filename (without extension) is the **slug**: lowercase, hyphen-separated words.
- The slug must match the `slug` field of the corresponding entry in `erp-docs.json`.

---

## erp-docs.json

`erp-docs.json` is the single source of truth for page order, titles, and hierarchy.
Keep it in sync whenever pages are added, removed, or reordered.
- Authoring repo: `docs/erp-demo/erp-docs.json`
- CV website repo: `src/data/projects/ERPDemo/erp-docs.json`

```json
[
  { "slug": "introduction", "title": "Introduction", "level": 1 },
  { "slug": "architecture-overview", "title": "Architecture Overview", "level": 1 },
  { "slug": "react-frontend", "title": "React Frontend", "level": 2 }
]
```

Level values: `1` = chapter, `2` = section, `3` = sub-section.
The CV website derives section numbers (e.g. `2.1`, `2.1.3`) and prev/next navigation automatically from this file.

---

## Document structure

Do **not** include a document title (`= Title`) — the page title is supplied by the CV website from `erp-docs.json`.

Start content directly with body text or with `==` section headings:

```asciidoc
== Overview

This section describes the overall architecture.

=== Backend

The backend is built with ASP.NET Core.
```

Use `==` for top-level sections within the page, `===` for sub-sections, `====` for deeper nesting.

Optional document attributes at the top of the file:

```asciidoc
:toc: left
:toclevels: 3
:sectnums:
:icons: font
```

---

## Custom macros

The CV website renderer supports the following macros in addition to standard AsciiDoc.

### Skill badges (inline — inside paragraph text)

Use `skill:Name[]` to render a coloured technology badge inline in any paragraph.
Use the same canonical skill name that appears in `src/data/skills.json`.

```asciidoc
The backend is implemented with skill:ASP.NET Core[] and skill:C#[].
The frontend is a skill:React[] and skill:TypeScript[] SPA.
```

### Buttons to open webpages (block macros — one per line)

All buttons must be on their own line. They open an external URL in a new tab.

```asciidoc
github::[href="https://github.com/org/repo",text="View on GitHub"]
website::[href="https://example.com",text="Live Demo"]
linkedin::[href="https://linkedin.com/in/you",text="LinkedIn"]
download::[href="https://example.com/v1.zip",text="Download v1.0"]

linux::[href="https://example.com/app.AppImage",text="Download for Linux"]
windows::[href="https://example.com/app.zip",text="Download for Windows"]
macos::[href="https://example.com/app.dmg",text="Download for macOS"]

firefox::[href="https://addons.mozilla.org/…",text="Get for Firefox"]
chrome::[href="https://chrome.google.com/webstore/…",text="Get for Chrome"]
```

#### Custom button with any react-icon

```asciidoc
btn::[href="https://…",icon="FaDownload",text="Download Release"]
btn::[href="https://…",icon="FaExternalLinkAlt",text="Open Dashboard"]
```

`icon` accepts any `react-icons/fa` name (e.g. `FaGithub`, `FaExternalLinkAlt`).

#### Contact email button (inline)

```asciidoc
Contact: email:[]
Contact: email:[href="you@example.com",text="Send email"]
```

### Video embeds

```asciidoc
youtube::dQw4w9WgXcQ[title="Demo walkthrough",width=800]

webm::./demo.mp4[max-width=700,start=2,autoplay,auto-loop]
webm::./demo.mp4[max-width=700,controls]
```

`webm` attributes:

| Attribute | Description |
|---|---|
| `max-width` | Pixel cap (default 700) |
| `start` | Start offset in seconds |
| `autoplay` | Autoplay + muted (boolean flag) |
| `auto-loop` | Loop the video (boolean flag) |
| `controls` | Show native controls, overrides autoplay |

### Highlight box

```asciidoc
highlight::[title="Note",text="This feature requires .NET 8.",shadow]
```

All attributes are optional. `shadow` is a boolean flag (no value needed).

To embed skill badges inside a highlight, use the block form with `====` delimiters:

```asciidoc
[highlight,title="Tech Stack",shadow]
====
skill::Azure AKS[]
skill::Kubernetes[]
skill::Docker[]
====
```

For arbitrary HTML content, use a passthrough block:

```asciidoc
++++
<highlight title="Tech stack" shadow>
  Built with <skill>ASP.NET Core</skill> and <skill>React</skill>.
</highlight>
++++
```

### DOT / Graphviz diagrams

Diagrams can be authored as **separate `.dot` files** (recommended) or as inline blocks.

#### External `.dot` file (recommended)

Place the `.dot` file alongside (or in a subdirectory of) your `.adoc` file and reference it with `dotgraph::`:

```asciidoc
dotgraph::./diagrams/system-architecture.dot[caption="System Architecture",max-width=700]
dotgraph::./order-flow.dot[engine=neato]
```

The file is bundled by the CV website's build at compile time. No copy step needed for `.dot` files — only `.adoc` files are copied manually.

File layout example:
```
docs/erp-demo/
  erp-docs.json
  docs/
    architecture-overview.adoc
    diagrams/
      system-architecture.dot
      order-flow.dot
```

#### Inline DOT block

For quick or one-off diagrams, embed the DOT source directly in the `.adoc` file:

```asciidoc
[graphviz,caption="Order flow",max-width=700,engine=dot]
....
digraph orders {
  rankdir=TB;
  node [shape=box];
  Created -> Processing -> Shipped -> Delivered;
  Processing -> Cancelled;
}
....
```

Use `----` instead of `....` as delimiter — both work.

#### Available attributes (both forms)

| Attribute | Description |
|---|---|
| `caption` | Figure caption shown below the diagram |
| `max-width` | Maximum pixel width, e.g. `700` |
| `align` | `left`, `right`, or default center |
| `engine` | Layout engine: `dot` (default), `neato`, `fdp`, `sfdp`, `circo`, `twopi` |

---

## Passthrough blocks

Use `++++` passthrough blocks to write raw HTML when macros are not sufficient:

```asciidoc
++++
<skill>Kubernetes</skill>
<skill>Docker</skill>
<skill>Entity Framework Core</skill>
++++
```

---

## Standard AsciiDoc features

### Code blocks

```asciidoc
[source,csharp]
----
public class OrderService
{
    public Task<Order> GetByIdAsync(Guid id) => _repo.FindAsync(id);
}
----

[source,sql]
----
SELECT * FROM orders WHERE status = 'pending';
----
```

### Admonitions

```asciidoc
NOTE: Requires .NET 8 or later.

TIP: Use the `--watch` flag during development for live reload.

IMPORTANT: Run database migrations before deploying a new version.

WARNING: This operation will delete all seed data.

CAUTION: Not supported on Windows without WSL.
```

### Tables

```asciidoc
[cols="1,1,3",options="header"]
|===
| Field | Type | Description

| id
| UUID
| Primary key, auto-generated

| created_at
| timestamp
| Set automatically on insert
|===
```

### Images

```asciidoc
image::./screenshot.png[alt="Dashboard screenshot",width=800]
```

Place image files alongside the `.adoc` file in `docs/erp-demo/docs/`.

### Blockquotes

```asciidoc
[quote,Author Name]
____
The quote text goes here.
____
```

### Description lists

```asciidoc
term one::
  Definition of term one.

term two::
  Definition of term two.
```

---

## Adding a new page

1. Create `docs/erp-demo/docs/<slug>.adoc`.
2. Add an entry at the desired position in `docs/erp-demo/erp-docs.json`:
   ```json
   { "slug": "your-slug", "title": "Your Page Title", "level": 2 }
   ```
3. Copy the `.adoc` file to `src/data/projects/ERPDemo/docs/` in the CV website repository.
4. Copy `docs/erp-demo/erp-docs.json` to the CV website as well (it replaces the one there).

No code changes are needed in the CV website.
