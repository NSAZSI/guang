#!/usr/bin/env python3
"""生成种子演示封面图（SVG）：30 张新闻封面 + 3 张首页轮播横幅。"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "backend", "AdPulse.Api", "wwwroot", "covers")
os.makedirs(OUT, exist_ok=True)

PALETTES = [
    ("品牌营销", "#FF5A1F", "#3A1206"),
    ("数字广告", "#38BDF8", "#082A44"),
    ("创意案例", "#F59E0B", "#3D2506"),
    ("媒介投放", "#34D399", "#06342A"),
    ("行业动态", "#A5B4CF", "#161D2E"),
]

COVER_TPL = """<svg xmlns="http://www.w3.org/2000/svg" width="800" height="450" viewBox="0 0 800 450">
<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
<stop offset="0" stop-color="#0B0E14"/><stop offset="1" stop-color="{dark}"/></linearGradient>
<radialGradient id="r" cx="0.85" cy="0.15" r="1.1">
<stop offset="0" stop-color="{accent}" stop-opacity="0.55"/><stop offset="0.55" stop-color="{accent}" stop-opacity="0.08"/><stop offset="1" stop-color="{accent}" stop-opacity="0"/>
</radialGradient></defs>
<rect width="800" height="450" fill="url(#g)"/><rect width="800" height="450" fill="url(#r)"/>
<text x="620" y="400" font-family="Arial, sans-serif" font-size="220" font-weight="800" fill="{accent}" opacity="0.16" text-anchor="middle">{num:02d}</text>
<text x="48" y="250" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="52" font-weight="700" fill="#F5F7FA">{cat}</text>
<text x="48" y="300" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="24" fill="#9AA3B8">行业情报 · 演示封面</text>
<rect x="48" y="330" width="120" height="4" fill="{accent}"/>
<rect x="48" y="392" width="8" height="8" fill="{accent}"/>
<text x="66" y="400" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="18" fill="#E8EAF0" opacity="0.7">AdPulse · 广告脉搏</text>
</svg>"""

BANNER_TPL = """<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="640" viewBox="0 0 1600 640">
<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
<stop offset="0" stop-color="#0B0E14"/><stop offset="1" stop-color="{dark}"/></linearGradient>
<radialGradient id="r" cx="0.8" cy="0.2" r="1.2">
<stop offset="0" stop-color="{accent}" stop-opacity="0.6"/><stop offset="0.6" stop-color="{accent}" stop-opacity="0.08"/><stop offset="1" stop-color="{accent}" stop-opacity="0"/>
</radialGradient></defs>
<rect width="1600" height="640" fill="url(#g)"/><rect width="1600" height="640" fill="url(#r)"/>
<!-- 纯背景：标题由页面叠加渲染，图中不放文字，避免手机端裁切与重叠 -->
<circle cx="1180" cy="210" r="260" fill="none" stroke="{accent}" stroke-opacity="0.22" stroke-width="2"/>
<circle cx="1180" cy="210" r="170" fill="none" stroke="{accent}" stroke-opacity="0.14" stroke-width="2"/>
<circle cx="1180" cy="210" r="80" fill="{accent}" fill-opacity="0.10"/>
<path d="M0 520 C 320 440, 640 600, 960 500 S 1440 420, 1600 470 L1600 640 L0 640 Z" fill="{accent}" fill-opacity="0.07"/>
</svg>"""

for i in range(1, 31):
    cat, accent, dark = PALETTES[(i - 1) % 5]
    with open(os.path.join(OUT, f"cover-{i:02d}.svg"), "w", encoding="utf-8") as f:
        f.write(COVER_TPL.format(cat=cat, accent=accent, dark=dark, num=i))

banners = [
    ("品牌营销", "#FF5A1F", "#3A1206", "星驰汽车官宣全球代言人", "首支品牌 TVC 全网上线"),
    ("数字广告", "#38BDF8", "#082A44", "数字广告投放规模增长 18%", "短视频媒介占比首次过半"),
    ("创意案例", "#F59E0B", "#3D2506", "戛纳创意节中国军团", "斩获 12 项大奖"),
]
for i, (cat, accent, dark, l1, l2) in enumerate(banners, 1):
    with open(os.path.join(OUT, f"banner-{i}.svg"), "w", encoding="utf-8") as f:
        f.write(BANNER_TPL.format(cat=cat, accent=accent, dark=dark))

print("covers generated:", len(os.listdir(OUT)))