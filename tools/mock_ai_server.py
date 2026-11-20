#!/usr/bin/env python3
"""Mock OpenAI 兼容服务（仅用于步骤 6 本地验收，不是生产组件）。

- 监听 127.0.0.1:5099，实现 POST /v1/chat/completions
- 校验 Authorization: Bearer mock-good-key，错误密钥返回 401（模拟"密钥改错"场景）
- 故意用 ```json 代码块包裹返回内容，验证后端解析容错
- 每次请求版本号 +1，验证"重新生成能换一版"
- 人为延迟 1.5s，让前端"生成中"状态可被观察到
"""
import json
import re
import time
from http.server import BaseHTTPRequestHandler, HTTPServer

GOOD_KEY = "mock-good-key"
VERSION = 0
TAG_POOL = ["程序化广告", "AI营销", "品牌增长", "数字媒介", "智能投放", "内容生态"]


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):
        print(f"[mock-ai] {fmt % args}", flush=True)

    def do_POST(self):
        global VERSION
        if self.path.rstrip("/") != "/v1/chat/completions":
            self.send_error(404)
            return
        auth = self.headers.get("Authorization", "")
        if auth != f"Bearer {GOOD_KEY}":
            body = json.dumps({"error": {"message": "Incorrect API key provided", "type": "invalid_request_error"}}).encode()
            self.send_response(401)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return

        length = int(self.headers.get("Content-Length", 0))
        req = json.loads(self.rfile.read(length) or b"{}")
        user_msg = next((m["content"] for m in req.get("messages", []) if m.get("role") == "user"), "")
        title = re.search(r"标题：(.+)", user_msg)
        title = title.group(1).strip()[:24] if title else "行业新闻"

        time.sleep(1.5)  # 让 Pending 状态可观察
        VERSION += 1
        tags = TAG_POOL[(VERSION - 1) % len(TAG_POOL):] + TAG_POOL[:(VERSION - 1) % len(TAG_POOL)]
        payload = {
            "summary": f"{title}：行业观察显示相关赛道持续升温，模拟AI摘要第{VERSION}版。",
            "tags": tags[:4],
        }
        # 故意包裹 markdown 代码块，验证后端容错清洗
        content = "```json\n" + json.dumps(payload, ensure_ascii=False) + "\n```"
        resp = {
            "id": f"chatcmpl-mock-{VERSION}",
            "object": "chat.completion",
            "model": req.get("model", "mock"),
            "choices": [{"index": 0, "message": {"role": "assistant", "content": content}, "finish_reason": "stop"}],
            "usage": {"prompt_tokens": 100, "completion_tokens": 60, "total_tokens": 160},
        }
        body = json.dumps(resp, ensure_ascii=False).encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


if __name__ == "__main__":
    print("[mock-ai] listening on 127.0.0.1:5099 (OpenAI compatible mock)", flush=True)
    HTTPServer(("127.0.0.1", 5099), Handler).serve_forever()