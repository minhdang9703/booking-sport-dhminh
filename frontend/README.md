# Booking Sport Frontend

React SPA scaffold for the Booking Sport project.

## Stack

- Vite
- React
- TypeScript
- React Router
- Tailwind CSS

## Setup

```bash
npm.cmd install
```

Copy environment values when local overrides are needed:

```bash
cp .env.example .env
```

Default API URL:

```env
VITE_API_BASE_URL=http://localhost:5000
```

## Development

```bash
npm.cmd run dev
```

PowerShell may block `npm.ps1` depending on local execution policy. Use `npm.cmd` on Windows to avoid that issue.

## Build

```bash
npm.cmd run build
```

## Routes

- `/`
- `/login`
- `/register`
- `/admin`
