#!/usr/bin/env python3
"""Tiny no-cache static server for local play and dev:  python3 serve.py  ->  http://127.0.0.1:8770/
(You can also just double-click index.html: the game uses plain <script> tags and needs no server.)"""
import http.server, socketserver, sys, os

class NoCache(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cache-Control', 'no-store, max-age=0')
        super().end_headers()
    def log_message(self, *a):  # quiet
        pass

if __name__ == '__main__':
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8770
    socketserver.TCPServer.allow_reuse_address = True
    with socketserver.TCPServer(('127.0.0.1', port), NoCache) as srv:
        print(f'Kaname feel ladder: http://127.0.0.1:{port}/'); srv.serve_forever()
