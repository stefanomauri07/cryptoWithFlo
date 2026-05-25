---
name: CyberPulse High-Tech Tracker
colors:
  surface: '#0e1322'
  surface-dim: '#0e1322'
  surface-bright: '#343949'
  surface-container-lowest: '#090d1d'
  surface-container-low: '#161b2b'
  surface-container: '#1a1f2f'
  surface-container-high: '#25293a'
  surface-container-highest: '#303445'
  on-surface: '#dee1f7'
  on-surface-variant: '#b9cbb9'
  inverse-surface: '#dee1f7'
  inverse-on-surface: '#2b3040'
  outline: '#849585'
  outline-variant: '#3b4b3d'
  surface-tint: '#00e479'
  primary: '#f1ffef'
  on-primary: '#003919'
  primary-container: '#00ff88'
  on-primary-container: '#007139'
  inverse-primary: '#006d37'
  secondary: '#ffb3b2'
  on-secondary: '#680013'
  secondary-container: '#b5032a'
  on-secondary-container: '#ffc2c1'
  tertiary: '#fffaf8'
  on-tertiary: '#3f2e00'
  tertiary-container: '#ffda8d'
  on-tertiary-container: '#7d5d00'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#60ff99'
  primary-fixed-dim: '#00e479'
  on-primary-fixed: '#00210c'
  on-primary-fixed-variant: '#005228'
  secondary-fixed: '#ffdad9'
  secondary-fixed-dim: '#ffb3b2'
  on-secondary-fixed: '#410008'
  on-secondary-fixed-variant: '#920020'
  tertiary-fixed: '#ffdf9e'
  tertiary-fixed-dim: '#fabd00'
  on-tertiary-fixed: '#261a00'
  on-tertiary-fixed-variant: '#5b4300'
  background: '#0e1322'
  on-background: '#dee1f7'
  surface-variant: '#303445'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  headline-sm:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.05em
  mono-data:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 4px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 40px
  gutter: 20px
  margin-mobile: 16px
  margin-desktop: 32px
---

## Brand & Style
The brand personality is high-precision, technical, and sophisticated, catering to serious cryptocurrency traders and enthusiasts. The UI evokes a sense of real-time urgency and data reliability through a dark, focused environment. 

The design style is **Corporate Modern with Glassmorphism touches**. It balances high-density data visualization with a sleek, futuristic aesthetic. We utilize semi-transparent surfaces, subtle backdrop blurs, and sharp accent glows to create a "terminal" feel that is nonetheless premium and accessible. The interface should feel like a high-end financial instrument: cold backgrounds punctuated by vibrant, meaningful color signals.

## Colors
This design system utilizes a deep, multi-layered dark palette to maximize contrast for data points.

- **Backgrounds:** The primary foundation is a deep obsidian. Cards and containers use a slightly lighter slate to create separation.
- **Accents (Semantic):** Color is used functionally. **Neon Green** is reserved strictly for positive price movement and "buy" signals. **Red** indicates losses, downward trends, or "sell" actions. **Amber** serves as a warning state or pending status.
- **Neutrals:** Typography is tiered between pure white for readability and a muted blue-grey for metadata and secondary labels to reduce visual noise in data-heavy views.

## Typography
Inter is chosen for its exceptional legibility in digital interfaces and its neutral, technical tone. 

For a cryptocurrency tracker, **tabular figures (`tnum`)** are essential. All price data and percentage changes should use the `mono-data` setting to ensure numbers align vertically in tables, facilitating quick scanning. 

- **Headlines:** Use tighter letter spacing and semi-bold weights to command attention.
- **Labels:** Small caps or increased letter spacing should be used for table headers and category descriptors to differentiate them from interactive data.
- **Mobile Scaling:** On devices smaller than 768px, `display-lg` should scale down to 32px to prevent horizontal overflow.

## Layout & Spacing
The layout follows a **fluid grid** logic with strict 4px increments. 

- **Desktop:** A 12-column grid with 20px gutters. Content is typically housed in cards that span 3, 4, 6, or 12 columns.
- **Tablet:** 8-column grid with 16px gutters.
- **Mobile:** 4-column grid. Complex data tables should transition to card-based summaries or include horizontal scrolling for key metrics.

Spacing rhythm is tight to allow for high data density, but "breathing room" is maintained through generous 24px internal padding within major dashboard cards.

## Elevation & Depth
Depth is created through **Backdrop Blurs and Tonal Layering** rather than aggressive shadows.

1.  **Level 0 (Base):** The #0f1117 background.
2.  **Level 1 (Cards):** #1e2130 with a subtle 1px border (#2a2d3e).
3.  **Level 2 (Overlays/Modals):** Semi-transparent background (alpha 0.8) with a 20px backdrop blur. This provides the glassmorphism effect.
4.  **Shadows:** Shadows are reserved for floating elements like dropdowns and modals. Use a soft, low-opacity spread: `0 4px 24px rgba(0,0,0,0.3)`.

Interactive elements like buttons should use a subtle inner glow or "drop-shadow" of their own accent color when hovered to simulate a glowing LED effect.

## Shapes
The shape language is "Soft-Tech." We use a hierarchy of corner radii to distinguish between layout containers and interactive controls.

- **Large Containers (Cards, Modals):** 16px (rounded-lg). This softens the high-density data and makes the app feel modern.
- **Controls (Buttons, Inputs, Chips):** 8px. These sharper corners suggest precision and clickability.
- **Graphs/Charts:** Line strokes should use a slight smoothing (radius 2px) on data points to avoid a jagged, "noisy" appearance.

## Components

### Buttons
- **Primary:** Neon Green background, black text, 8px radius. High-visibility for "Trade" or "Connect Wallet."
- **Secondary:** Ghost style. Transparent background with a #2a2d3e border and white text.
- **Destructive:** Red background with white text for "Delete Alert" or "Sell."

### Inputs & Selects
- Background: #0f1117 (inset look).
- Border: 1px solid #2a2d3e, turning Primary Green on focus.
- Placeholder text: Secondary Text color (#8b8fa3).

### Data Cards
- Must include a 16px padding.
- Headers should use `label-md` for titles.
- Use sparklines (miniature charts) for 24h trends, colored based on the price change (Green/Red).

### Chips/Badges
- Small, 4px rounded status indicators. 
- Use low-opacity backgrounds of the accent color (e.g., Green at 15% opacity) with full-opacity text for better legibility against the dark theme.

### Tables
- Row hover state: #2a2d3e at 30% opacity.
- Border-bottom: 1px solid #2a2d3e between rows.
- Align numeric columns to the right.