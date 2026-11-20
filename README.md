# AdPulse · 广告脉搏

广告行业新闻门户，Vue 3 + ASP.NET Core 10 + SQLite 全栈项目。

- **在线演示**：<https://g.dietai.site>
- **后台入口**：<https://g.dietai.site/admin/login>

| 账号 | 密码 | 角色 |
|---|---|---|
| admin | AdPulse@2026 | 管理员（全部权限） |
| editor | Editor@2026 | 编辑（仅管理自己的文章） |

## 功能

- **前台门户**：首页轮播 / 头条 / 分类 / 热榜，新闻列表搜索高亮与筛选分页，新闻详情，行业数据图表页
- **实时快讯**：后台发布新闻后，所有打开的页面通过 SSE 秒级收到顶部快讯，断线自动重连
- **后台管理**：JWT 登录与角色权限、仪表盘、新闻全生命周期（富文本、传图、草稿、发布、置顶、撤回、回收站）、分类 / 标签 / 轮播管理、操作日志
- **AI 小编**：发布后异步生成一句话摘要与行业标签（已接入 DeepSeek）；AI 不可用时自动降级为规则摘要，发布流程不受影响
- **体验**：明暗双主题、电脑与手机全响应式

## 目录

```text
AdPulse-完整交付/
└── adpulse/                     # 项目源码（详细说明见其 README）
    ├── AdPulse项目介绍.html      # 项目介绍文档（下载后用浏览器打开）
    ├── AdPulse项目实施计划.md    # 实施计划
    ├── backend/AdPulse.Api/     # ASP.NET Core 10 后端（Controller → Service → Repository）
    ├── frontend/                # Vue 3 前端
    ├── tests/                   # Playwright E2E + API 验收脚本 + 截图
    ├── deploy/                  # nginx / systemd / 备份脚本 / 部署命令清单
    ├── tools/                   # 封面生成、AI mock 等工具
    └── docs/                    # 实施计划与验证报告
AdPulse-完整交付.zip             # 完整交付压缩包
```

本地运行、服务器部署与 AI 配置方法见 [adpulse/README.md](AdPulse-完整交付/adpulse/README.md)。
提交历史保留了开发全过程，标签 `step-1` ~ `step-7` 对应各阶段验收节点。

> 站内新闻与行业统计数据均为演示样例。
