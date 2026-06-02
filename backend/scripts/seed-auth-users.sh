#!/usr/bin/env sh
set -eu

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
BACKEND_DIR="$(dirname "$SCRIPT_DIR")"
SQL_PATH="$SCRIPT_DIR/seed-auth-users.sql"

CONNECTION_STRING="${ConnectionStrings__DefaultConnection:-}"

if [ -z "$CONNECTION_STRING" ]; then
  APPSETTINGS_PATH="$BACKEND_DIR/BookingSport.Api/appsettings.Development.json"

  if [ ! -f "$APPSETTINGS_PATH" ]; then
    echo "Connection string was not provided and appsettings.Development.json was not found." >&2
    exit 1
  fi

  CONNECTION_STRING="$(sed -n 's/.*"DefaultConnection"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$APPSETTINGS_PATH" | head -n 1)"
fi

if [ -z "$CONNECTION_STRING" ]; then
  echo "Connection string is empty." >&2
  exit 1
fi

get_connection_value() {
  key="$1"
  printf '%s' "$CONNECTION_STRING" |
    tr ';' '\n' |
    awk -F= -v wanted="$key" '
      {
        gsub(/^[ \t]+|[ \t]+$/, "", $1)
        value = substr($0, index($0, "=") + 1)
        gsub(/^[ \t]+|[ \t]+$/, "", value)
        if ($1 == wanted) {
          print value
          exit
        }
      }
    '
}

DB_HOST="$(get_connection_value Host)"
DB_PORT="$(get_connection_value Port)"
DB_NAME="$(get_connection_value Database)"
DB_USER="$(get_connection_value Username)"
DB_PASSWORD="$(get_connection_value Password)"

if [ -z "$DB_HOST" ] || [ -z "$DB_PORT" ] || [ -z "$DB_NAME" ] || [ -z "$DB_USER" ] || [ -z "$DB_PASSWORD" ]; then
  echo "Connection string must include Host, Port, Database, Username, and Password." >&2
  exit 1
fi

if ! command -v psql >/dev/null 2>&1; then
  echo "psql was not found. Add PostgreSQL bin directory to PATH." >&2
  exit 1
fi

PGPASSWORD="$DB_PASSWORD" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -v ON_ERROR_STOP=1 \
  -f "$SQL_PATH"

echo "Seeded auth users:"
echo "  Customer: user01 / user123"
echo "  Admin:    admin / admin123"
