# Google Stitch — Design Prompt for Crypto Tracker

> Copy the content below into Google Stitch (https://stitch.withgoogle.com/) to generate the UI design mockup.

---

Create a modern, sleek UI mockup for a Cryptocurrency Tracker web application.
Dark theme, rounded corners, smooth design, user-friendly dashboard.

---

## Overall Theme

- Dark background (#0f1117 or #1a1d2e), clean and professional
- Accent colors: neon green (#00ff88) for positive/up, red (#ff4757) for negative/down
- Cards/panels: slightly lighter dark (#1e2130) with subtle border (#2a2d3e)
- All elements: rounded corners (border-radius: 12-16px for cards, 8px for buttons/inputs)
- Soft shadows (box-shadow with low opacity)
- Font: Inter or SF Pro Display, clean sans-serif

---

## Page 1: Dashboard (main view)

### Top: Health Bar

- Thin strip across the top of the page
- Left: app name "CryptoTracker" in bold white
- Right: green dot + "System OK" or red dot + "DB Error"
- Next to it: "Prices updated 15s ago" in light gray

### Section 1: Price Table (left side, ~60% width)

- Card with title "Live Prices"
- Table with columns: Icon | Name | Symbol | Price (USD) | Price (EUR) | 24h Change
- Each row: crypto icon (small circle with BTC/ETH etc.), name in white, symbol in gray
- Price columns: aligned right, monospace font
- 24h Change: green text + ▲ arrow if positive, red text + ▼ arrow if negative
- Subtle row hover effect (slight background highlight)
- Auto-refresh indicator: small spinning icon next to title

### Section 2: Historical Chart (right side, ~40% width)

- Card with title "Price Chart"
- Dropdown selector at top: choose crypto (Bitcoin, Ethereum, etc.)
- Period buttons: 7D | 30D | 90D (pill-shaped toggles, active one highlighted in accent color)
- Line chart below with:
  - Smooth curve (tensioned bezier)
  - Gradient fill under the line (transparent color fading to zero)
  - Grid lines subtle, almost invisible
  - Tooltip on hover: date + exact price
  - Y-axis: price in USD, formatted
  - X-axis: dates, auto-formatted

### Section 3: Alerts Panel (bottom, full width)

- Card with title "Price Alerts"
- Left side: Form to create new alert
  - Dropdown: select crypto
  - Dropdown: condition (Price Above / Price Below)
  - Input: threshold price in USD (number input with $ prefix)
  - Input: webhook URL
  - Button: "Create Alert" (green/white)
- Right side: List of existing alerts
  - Each alert as a row/card:
    - Crypto icon + name
    - Condition text ("Above $50,000" or "Below $2,000")
    - Status badge: "Active" (yellow/amber pill) or "Triggered" (green pill)
    - Delete button (trash icon, red on hover)
    - Created date in light gray

---

## Page 2: Crypto Detail (single crypto view)

- Back button top-left: ← Back to Dashboard
- Hero section: large crypto icon, name, symbol, current price in big bold text
- 24h change: large colored text with arrow
- Full-width line chart (same style as dashboard but larger, 100% width)
- Below chart: stats grid (Market Cap, Volume 24h, All-Time High, Circulating Supply) — placeholder values
- Period selector: 7D | 30D | 90D | 1Y

---

## Page 3: Alert History

- Card: list of all triggered alerts (historical)
- Similar to alerts panel but read-only, showing triggered date and webhook response status
- Filter: All | Active | Triggered

---

## Mobile Responsive considerations

- On mobile: sections stack vertically
- Table becomes card-based list
- Chart adapts to full width
- Alerts form stacks above list

---

## Interactions & Animations

- Price values fade/transition smoothly when they update (opacity + transform)
- Table rows: subtle slide-in on page load
- Buttons: slight scale-up on hover (transform: scale(1.02))
- Cards: subtle hover lift (translateY(-2px) + shadow increase)
- Chart line animates on draw
- Status dots pulse gently when "connected"
- Loading states: skeleton placeholders (gray animated shimmer bars)

---

## Design tokens (CSS-like)

| Token | Value |
|-------|-------|
| Background primary | `#0f1117` |
| Background card | `#1e2130` |
| Border | `#2a2d3e` |
| Text primary | `#ffffff` |
| Text secondary | `#8b8fa3` |
| Accent green | `#00ff88` |
| Accent red | `#ff4757` |
| Accent amber | `#ffc107` |
| Font family | `'Inter', sans-serif` |
| Border radius cards | `16px` |
| Border radius inputs | `8px` |
| Shadow | `0 4px 24px rgba(0,0,0,0.3)` |
