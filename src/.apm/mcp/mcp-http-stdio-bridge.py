#!/usr/bin/env python3
"""Bridge stdio MCP clients to a Streamable HTTP MCP server.

Codex currently rejects URL-based MCP server configs unless they use https://.
This bridge lets Codex see a stdio MCP server while keeping the real local MCP
service on plain HTTP for debugging.
"""

from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.request
from typing import Any


HOP_BY_HOP_HEADERS = {
    "connection",
    "keep-alive",
    "proxy-authenticate",
    "proxy-authorization",
    "te",
    "trailer",
    "trailers",
    "transfer-encoding",
    "upgrade",
}


def parse_sse(body: str) -> list[Any]:
    messages: list[Any] = []
    event_lines: list[str] = []

    for raw_line in body.splitlines():
        line = raw_line.rstrip("\r")
        if not line:
            if event_lines:
                payload = "\n".join(event_lines)
                if payload:
                    messages.append(json.loads(payload))
                event_lines = []
            continue

        if line.startswith("data:"):
            event_lines.append(line[5:].lstrip())

    if event_lines:
        payload = "\n".join(event_lines)
        if payload:
            messages.append(json.loads(payload))

    return messages


def http_roundtrip(url: str, message: Any, session_id: str | None) -> tuple[list[Any], str | None]:
    payload = json.dumps(message, separators=(",", ":")).encode("utf-8")
    headers = {
        "Accept": "application/json, text/event-stream",
        "Content-Type": "application/json",
        "Content-Length": str(len(payload)),
    }
    if session_id:
        headers["Mcp-Session-Id"] = session_id

    request = urllib.request.Request(url, data=payload, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(request, timeout=None) as response:
            new_session_id = response.headers.get("Mcp-Session-Id") or session_id
            status = response.status
            content_type = response.headers.get("Content-Type", "")
            body = response.read()
    except urllib.error.HTTPError as error:
        new_session_id = error.headers.get("Mcp-Session-Id") or session_id
        status = error.code
        content_type = error.headers.get("Content-Type", "")
        body = error.read()

    if status in (200, 201):
        text = body.decode("utf-8") if body else ""
        if not text:
            return [], new_session_id
        if "text/event-stream" in content_type:
            return parse_sse(text), new_session_id
        return [json.loads(text)], new_session_id

    if status in (202, 204):
        return [], new_session_id

    detail = body.decode("utf-8", errors="replace") if body else ""
    raise RuntimeError(f"HTTP MCP bridge received {status}: {detail}")


def is_notification(message: Any) -> bool:
    return isinstance(message, dict) and "id" not in message


def write_message(message: Any) -> None:
    sys.stdout.write(json.dumps(message, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def write_error(request_id: Any, message: str) -> None:
    write_message(
        {
            "jsonrpc": "2.0",
            "id": request_id,
            "error": {"code": -32603, "message": message},
        }
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", required=True, help="Streamable HTTP MCP endpoint URL")
    args = parser.parse_args()

    session_id: str | None = None

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        request_id: Any = None
        try:
            message = json.loads(line)
            if isinstance(message, dict):
                request_id = message.get("id")

            responses, session_id = http_roundtrip(args.url, message, session_id)
            if is_notification(message):
                continue

            for response in responses:
                write_message(response)
        except Exception as exc:
            if request_id is not None:
                write_error(request_id, str(exc))
            else:
                print(f"mcp-http-stdio-bridge error: {exc}", file=sys.stderr, flush=True)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
