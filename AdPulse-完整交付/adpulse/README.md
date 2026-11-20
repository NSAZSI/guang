# AdPulse · 广告脉搏

广告行业新闻门户 —— Vue 3 + ASP.NET Core 10 + SQLite 的全栈面试演示作品。
前台看新闻、看行业数据；后台写稿、发布、管理；发布瞬间全站 SSE 实时快讯；每篇新闻自动配 AI 摘要与标签（无密钥自动降级，网站永不因 AI 故障而发不出文章）。

> **诚实声明**：站内 30 条新闻、行业图表与统计数据均为**种子演示样例**，不冒充真实行业统计。
> 项目的真实性在于：架构、流程与工程细节全部真实可运行、可验收。

## 功能总览

| 模块 | 能力 |
|---|---|
| 前台门户 | 首页（轮播/头条/分类Tab/热榜/数字滚动）、新闻列表（搜索高亮/分类/标签筛选/排序/分页）、详情（富文本消毒/AI摘要卡片/上下篇/相关推荐）、行业数据页（4 张 ECharts 图表） |
| 实时快讯 | SSE 单向推送：后台发布 → 所有打开的页面秒级收到顶部快讯，点击直达详情；15s 心跳保活、断线自动重连 |
| 后台管理 | 登录鉴权（JWT + 角色）、仪表盘、新闻 CRUD（TipTap 富文本/服务器传图/草稿/发布/置顶/撤回/回收站还原）、分类/标签/轮播管理、操作日志 |
| AI 小编 | 发布后异步队列生成一句话摘要 + 3~5 个行业标签；OpenAI 兼容协议可换任意供应商；无密钥→规则小编（Fallback），调用失败→重试1次后规则兜底（Failed）并记警告日志 |
| 体验 | 明暗双主题（持久化）、电脑/手机全响应式、路由级代码分割、骨架屏、克制动效 |

## 架构

```text
浏览器 (Vue3 SPA)
   │  同源 /api（生产无 CORS）
   ▼
Nginx ── /            → /var/adpulse/www（try_files 回退 index.html）
   │── /api/sse/      → 反代 5000（proxy_buffering off，SSE 生命线）
   │── /api/          → 反代 5000
   │── /uploads/ /covers/ → 静态文件直出
   ▼
ASP.NET Core 10 (Kestrel, 127.0.0.1:5000)
   Controller → Service → Repository（三层）
   │            ├─ SseHub（Channels 进程内广播）
   │            ├─ AiTaskQueue + BackgroundService（异步 AI，不阻塞发布）
   │            └─ Serilog（控制台 + 滚动文件）
   ▼
SQLite (WAL) ← EF Core 10 Migrations（启动自动迁移 + 幂等种子）
```

## 技术选型与理由

| 技术 | 版本 | 为什么 |
|---|---|---|
| ASP.NET Core | 10 (LTS) | 10 是长期支持版，11 是 STS 不选（[.NET 支持政策](https://dotnet.microsoft.com/zh-cn/platform/support/policy)） |
| EF Core + SQLite | 10.0.x | 演示级流量读多写少，WAL 并发读够用；零运维、备份即拷文件；Repository 隔离数据层，量级上来换 PostgreSQL 只改一层 |
| Vue | 3.5.x | 3.6 仍处 RC 不选（[佐证](https://0.vuetifyjs.com/guide/integration/vapor)）；Composition API + `<script setup>` |
| Vite | 8.x | Rolldown 内核，构建更快（[Vite 8 公告](https://vite.dev/blog/announcing-vite8)） |
| Pinia / Vue Router | 3.x / 4.x | 官方状态管理与路由稳定大版本 |
| Element Plus | 2.14.x | 后台组件库，按文件引入 |
| ECharts | 6.x | 数据页图表，模块化引入 + 路由懒加载 |
| TipTap | 3.x | 后台富文本编辑器，图片走服务器上传不落 base64 |
| JWT Bearer | 10.0.x | 无会话鉴权，角色声明驱动授权 |
| Serilog | 10.x | 结构化日志，滚动文件保留 14 天 |
| SSE | 原生 | 需求是服务器单向通知：普通 HTTP + EventSource 自带重连，比 WebSocket 轻、比轮询快 |

## 本地启动（从零一次成功）

前提：.NET 10 SDK、Node.js 22+、Git。

```bash
git clone <本仓库> && cd adpulse

# 1) 后端（首次启动自动建库 + 迁移 + 播种演示数据）
cd backend/AdPulse.Api
dotnet run
# 应看到：AdPulse API 启动完成，数据库：.../App_Data/adpulse.db
# 验证：浏览器打开 http://localhost:5000/swagger（仅开发环境可见）

# 2) 前端（另开终端）
cd frontend
npm install
npm run dev
# 打开 http://localhost:5173 —— 首页即有演示数据
```

演示账号（**部署到公网后务必改掉**，见 deploy/deploy-commands.md 第 7 节）：

| 账号 | 密码 | 角色 |
|---|---|---|
| admin | AdPulse@2026 | 管理员（全部权限） |
| editor | Editor@2026 | 编辑（仅自己的文章，无回收站/日志/用户管理） |

## 服务器部署（Linux 裸机，不用 Docker）

完整命令清单（每条附"执行后应该看到什么"）见 **[deploy/deploy-commands.md](deploy/deploy-commands.md)**。
配置文件：`deploy/nginx-adpulse.conf`、`deploy/adpulse-api.service`、`deploy/backup-db.sh`。
要点：数据库连接串必须绝对路径；JWT/AI 密钥用环境变量注入；Nginx 的 `/api/sse/` 专属段必须 `proxy_buffering off`。

## AI 摘要配置

后端 `Ai` 配置节（可用环境变量 `Ai__BaseUrl` / `Ai__ApiKey` / `Ai__Model` 注入）：

```json
"Ai": {
  "BaseUrl": "https://ark.cn-beijing.volces.com/api/v3",
  "ApiKey": "<你的密钥，严禁提交进 Git>",
  "Model": "<接入点/模型名>",
  "MaxTokens": 300,
  "TimeoutSeconds": 30
}
```

- 兼容任何 OpenAI 协议端点：豆包方舟、DeepSeek、Dify 等。
- **不配密钥**：自动走规则小编（正文前两句 ≤80 字 + 行业词库高频标签），详情页诚实标注"规则生成"。
- **调用失败/超时**：重试 1 次后降级规则兜底，状态 Failed，Serilog 记警告；发布主流程完全不受影响。
- 本地验收可用 `python3 tools/mock_ai_server.py` 起一个 OpenAI 兼容 mock（端口 5099），
  以 `Ai__BaseUrl=http://127.0.0.1:5099/v1 Ai__ApiKey=mock-good-key` 启动后端即可走通 AI 路径。

## 测试与验收

```bash
# 后端 API 验收（21 项）
python3 tests/api_step2.py

# 前端 E2E（Playwright，需先 npm install 并 npx playwright install chromium）
cd tests
npx playwright test step1.spec.ts step3.spec.ts step4.spec.ts step5.spec.ts
npx playwright test step5-idle.spec.ts        # 10 分钟挂机心跳
npx playwright test step6-nokey.spec.ts       # 无密钥降级路径
# 起 mock + 带密钥重启后端后：
npx playwright test step6-ai.spec.ts step6-badkey.spec.ts
```

分步验收记录见 `docs/AdPulse项目实施计划.md`（已验收步骤以删除线标注）与 `docs/验证报告.md`。

## 目录结构

```text
adpulse/
├── backend/AdPulse.Api/     # 三层架构后端（Controllers/Services/Repositories + Data/Migrations）
├── frontend/                # Vue3 SPA（views/portal 前台 + views/admin 后台）
├── tests/                   # Playwright E2E + Python API 验收 + 截图留证
├── tools/                   # 封面生成器 / MD 划掉脚本 / AI mock 服务
├── deploy/                  # nginx / systemd / keepalive / 备份 / 部署命令清单
└── docs/                    # 实施计划（划掉版）+ 验证报告
```