"""Answers every request the way a Countly server that accepts it does, for the tests that need a server to deliver to."""

import http.server
import sys


class Handler(http.server.BaseHTTPRequestHandler):
    """Reads each request and answers it with HTTP 200 and {"result":"Success"}."""

    def answer(self):
        """Reads the body, if any, and sends the success answer."""
        length = int(self.headers.get("Content-Length") or 0)
        if length:
            self.rfile.read(length)
        body = b'{"result":"Success"}'
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    do_GET = answer
    do_POST = answer

    def log_message(self, *args):
        """Keeps the job log free of a line per request."""


if __name__ == "__main__":
    http.server.ThreadingHTTPServer(("127.0.0.1", int(sys.argv[1])), Handler).serve_forever()
