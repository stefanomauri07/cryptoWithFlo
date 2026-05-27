# Fix Advisor Layout + API Connectivity

**Date:** 2026-05-27  
**Status:** Implemented

## Root Cause

### Bug 1: Advisor cambia layout
Commit `d39433a` ha creato `advisor.html` come pagina full-viewport con layout completamente diverso da tutte le altre pagine:

| Elemento | Pagine standard | Advisor (prima) |
|---|---|---|
| Body | `overflow-x-hidden` | `overflow-hidden` |
| `<main>` | `lg:pl-64 pt-16 min-h-screen` | `lg:pl-64 bg-gradient-main` |
| Contenuto | `p-margin-desktop space-y-lg` | `#chat-container height:calc(100vh - 64px)` |

### Bug 2: API connectivity fragile
- `config.js` usava `window.__API_URL__` che non veniva mai impostato, fallback a `http://localhost:5000`
- `.env` ha `API_URL` ma solo per backend/docker-compose, non iniettato nel frontend
- `default.conf` nginx non aveva reverse proxy `/api/` → le chiamate andavano direttamente al browser

### Bug 3: Tailwind config incompleto su advisor (root cause layout)
L'advisor aveva un tailwind config minimale (solo 20 colori). Mancavano `spacing`, `fontFamily`, `fontSize`, `borderRadius` → tutte le classi usate da `layout.js` (`font-headline-md`, `text-label-md`, `rounded-lg`, `p-margin-desktop`, ecc.) non funzionavano. Anche con l'HTML corretto, il layout appariva rotto perche' Tailwind non generava il CSS per quelle classi.

## Fix

### advisor.html
- Body: `overflow-hidden` → `overflow-x-hidden`
- `<main>`: aggiunto `pt-16 min-h-screen`, avvolto chat in `p-margin-desktop space-y-lg`
- CSS `#chat-container`: `height:calc(100vh - 64px);margin-top:64px` → `min-height:calc(100vh - 260px)`
- `#chat-input-area` background: `#0a0f1a` → `#161b2b`
- Error message: generico invece del riferimento locale a Ollama
- **Tailwind config**: sostituito config minimale con quello completo (colors, spacing, fontFamily, fontSize, borderRadius — uguale a news.html/compare.html)
- Aggiunto `<div id="toast-container"></div>`

### default.conf
- Aggiunto `location /api/ { proxy_pass http://backend:5000; }` con header standard

### config.js
- `API_BASE` default: `"http://localhost:5000"` → `""` (URL relativi, via proxy)

## Files Modified
- `frontend/advisor.html`
- `frontend/default.conf`
- `frontend/config.js`
- `worklog.md`
