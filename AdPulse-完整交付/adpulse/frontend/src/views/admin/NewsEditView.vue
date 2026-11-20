<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElInput, ElSelect, ElOption, ElSwitch, ElButton, ElUpload, ElMessage, ElTag, ElProgress } from 'element-plus'
import { adminApi, portalApi } from '@/api/modules'
import type { Category, TagItem } from '@/types'
import RichEditor from '@/components/RichEditor.vue'
import { aiStatusLabel } from '@/utils/news'

const SUMMARY_MAX = 300
const AI_TIMEOUT_S = 90

const route = useRoute()
const router = useRouter()
// 新建草稿保存后会拿到 id 并原地切换为编辑模式，因此用本地状态而不是只读路由参数
const newsId = ref(Number(route.params.id) || 0)
const status = ref<'Draft' | 'Published' | 'Recycled' | ''>('')
const categories = ref<Category[]>([])
const tags = ref<TagItem[]>([])
const saving = ref(false)
const aiStatus = ref('None')
const aiSummary = ref<string | null>(null)

// AI 生成进度
const aiRunning = ref(false)
const aiStage = ref('')
const aiElapsed = ref(0)
const aiProgress = computed(() => Math.min(95, Math.round((1 - Math.exp(-aiElapsed.value / 6)) * 100)))
let aiPoll: ReturnType<typeof setInterval> | undefined
let aiTick: ReturnType<typeof setInterval> | undefined
let aiPolling = false

const form = reactive({
  title: '', summary: '', content: '', coverUrl: '', categoryId: 0,
  tagIds: [] as number[], isTop: false, isHeadline: false
})

async function loadDetail() {
  const d = await adminApi.newsDetail(newsId.value)
  Object.assign(form, { title: d.title, summary: d.summary, content: d.content, coverUrl: d.coverUrl, categoryId: d.categoryId, tagIds: d.tagIds, isTop: d.isTop, isHeadline: d.isHeadline })
  status.value = d.status as typeof status.value
  aiStatus.value = d.aiStatus
  aiSummary.value = d.aiSummary
  return d
}

onMounted(async () => {
  const [cats, ts] = await Promise.all([portalApi.categories(), adminApi.tags()])
  categories.value = cats
  tags.value = ts
  if (newsId.value) {
    const d = await loadDetail()
    if (d.aiStatus === 'Pending') watchAi()   // 进入页面时若仍在生成，继续显示进度
  }
})

async function uploadCover(opt: { file: unknown }) {
  const { url } = await adminApi.upload(opt.file as File)
  form.coverUrl = url
}

const isEmptyContent = () => !form.content || form.content === '<p></p>'

/** 存草稿只需标题、正文、分类；发布时再要求摘要与封面 */
function validate(publish: boolean): boolean {
  if (!form.title.trim()) { ElMessage.warning('标题不能为空'); return false }
  if (isEmptyContent()) { ElMessage.warning('正文不能为空'); return false }
  if (!form.categoryId) { ElMessage.warning('请选择分类'); return false }
  if (publish && !form.summary.trim()) { ElMessage.warning('发布前请填写摘要（可用下方 AI 生成后一键填入）'); return false }
  if (publish && !form.coverUrl) { ElMessage.warning('发布前请上传封面图'); return false }
  return true
}

/** 保存到服务器；返回是否成功。草稿保存后留在本页，新建的会原地切换为编辑模式 */
async function persist(publish: boolean): Promise<boolean> {
  if (!validate(publish)) return false
  saving.value = true
  try {
    const payload = { ...form, title: form.title.trim(), summary: form.summary.trim(), publish }
    if (newsId.value) {
      await adminApi.newsUpdate(newsId.value, payload)
    } else {
      const { id } = await adminApi.newsCreate(payload)
      newsId.value = id
      await router.replace({ name: 'admin-news-edit', params: { id } })
    }
    status.value = publish ? 'Published' : 'Draft'
    return true
  } catch {
    return false   // 错误提示由 http 拦截器统一弹出
  } finally { saving.value = false }
}

async function save(publish: boolean) {
  if (!(await persist(publish))) return
  if (publish) {
    ElMessage.success('已发布，AI 摘要将在后台自动生成')
    router.push('/admin/news')
  } else {
    ElMessage.success('草稿已保存')
  }
}

/** 生成 AI 摘要：先按当前状态保存最新内容（AI 读取的是已保存的正文），再投递任务并显示进度 */
async function generateAi() {
  if (aiRunning.value) return
  if (!form.title.trim() || isEmptyContent()) { ElMessage.warning('请先填写标题和正文，AI 才能生成摘要'); return }
  aiRunning.value = true
  aiElapsed.value = 0
  aiStage.value = '正在保存最新内容…'
  const ok = await persist(status.value === 'Published')
  if (!ok) { aiRunning.value = false; return }
  try {
    aiStage.value = '已提交，等待 AI 处理…'
    await adminApi.aiSummary(newsId.value)
    aiStatus.value = 'Pending'
    watchAi()
  } catch {
    stopAi()
  }
}

function watchAi() {
  aiRunning.value = true
  if (!aiStage.value) aiStage.value = 'AI 正在阅读正文并生成摘要…'
  clearInterval(aiPoll); clearInterval(aiTick)
  aiTick = setInterval(() => {
    aiElapsed.value++
    if (aiElapsed.value === 2) aiStage.value = 'AI 正在阅读正文并生成摘要…'
    if (aiElapsed.value === 12) aiStage.value = '内容较长，AI 仍在生成，请稍候…'
  }, 1000)
  aiPoll = setInterval(async () => {
    if (aiPolling) return
    if (aiElapsed.value > AI_TIMEOUT_S) {
      stopAi()
      ElMessage.warning('AI 生成超时，请稍后重试')
      return
    }
    aiPolling = true
    let d
    try { d = await adminApi.newsDetail(newsId.value) } finally { aiPolling = false }
    if (d.aiStatus === 'Pending' || !aiRunning.value) return
    aiStatus.value = d.aiStatus
    aiSummary.value = d.aiSummary
    // AI 标签已由后端并入该新闻：同步到表单，避免随后保存时被覆盖
    tags.value = await adminApi.tags()
    form.tagIds = [...new Set([...form.tagIds, ...d.tagIds])]
    stopAi()
    if (d.aiStatus === 'Done') ElMessage.success(`AI 摘要已生成（${aiElapsed.value} 秒），可一键填入摘要栏`)
    else if (d.aiStatus === 'Fallback') ElMessage.info('未配置 AI 密钥，已用规则引擎生成摘要')
    else ElMessage.warning('AI 服务暂不可用，已用规则摘要兜底')
  }, 1000)
}

function stopAi() {
  clearInterval(aiPoll); clearInterval(aiTick)
  aiRunning.value = false
  aiStage.value = ''
}

/** 把 AI 摘要写入“摘要”栏：replace 覆盖旧摘要，append 追加在原摘要后面 */
function applyAi(mode: 'replace' | 'append') {
  const ai = (aiSummary.value ?? '').trim()
  if (!ai) return
  if (mode === 'replace') {
    form.summary = ai
  } else {
    const base = form.summary.trim()
    const merged = base ? `${base}${/[。！？.!?]$/.test(base) ? '' : '。'}${ai}` : ai
    if (merged.length > SUMMARY_MAX) {
      ElMessage.warning(`追加后共 ${merged.length} 字，超过 ${SUMMARY_MAX} 字上限，请改用“完整填入”或先精简原摘要`)
      return
    }
    form.summary = merged
  }
  ElMessage.success(mode === 'replace' ? '已填入摘要栏（记得保存）' : '已追加到摘要末尾（记得保存）')
}

onBeforeUnmount(stopAi)
</script>

<template>
  <div class="edit-page">
    <div class="card form-card">
      <h2 class="page-title">{{ newsId ? '编辑新闻' : '写新闻' }}<el-tag v-if="status === 'Draft'" size="small" type="info" class="st-tag">草稿</el-tag><el-tag v-else-if="status === 'Published'" size="small" type="success" class="st-tag">已发布</el-tag></h2>

      <div class="field">
        <label>标题 *</label>
        <el-input v-model="form.title" maxlength="200" show-word-limit placeholder="输入新闻标题" size="large" />
      </div>

      <div class="field">
        <label>摘要 <span class="req-note">（发布时必填）</span></label>
        <el-input v-model="form.summary" type="textarea" :rows="2" :maxlength="SUMMARY_MAX" show-word-limit placeholder="一句话摘要，显示在新闻列表；也可以用下方 AI 生成后一键填入" />

        <!-- AI 摘要助手 -->
        <div class="ai-box">
          <div class="ai-bar">
            <span class="ai-title">✦ AI 摘要助手</span>
            <el-tag size="small" effect="plain">{{ aiRunning ? '生成中' : aiStatusLabel(aiStatus) }}</el-tag>
            <el-button size="small" class="ai-btn" :loading="aiRunning" :disabled="saving" @click="generateAi">
              {{ aiSummary ? '重新生成' : '生成 AI 摘要' }}
            </el-button>
          </div>

          <div v-if="aiRunning" class="ai-progress">
            <el-progress :percentage="aiProgress" :stroke-width="6" :show-text="false" color="#ff5a1f" />
            <div class="ai-stage muted">{{ aiStage }} <span class="ai-sec">已用时 {{ aiElapsed }} 秒</span></div>
          </div>

          <div v-else-if="aiSummary" class="ai-result">
            <p>{{ aiSummary }}</p>
            <div class="ai-actions">
              <span class="muted">{{ aiSummary.length }} 字</span>
              <el-button size="small" type="primary" class="accent-btn" @click="applyAi('replace')">完整填入</el-button>
              <el-button size="small" @click="applyAi('append')">后面追加</el-button>
            </div>
          </div>

          <p v-else class="muted ai-tip">根据标题和正文生成一句话摘要（60 字以内）和行业标签。生成前会自动保存当前内容（新稿件保存为草稿）。</p>
        </div>
      </div>

      <div class="row2">
        <div class="field">
          <label>分类 *</label>
          <el-select v-model="form.categoryId" placeholder="选择分类" style="width: 100%">
            <el-option v-for="c in categories" :key="c.id" :label="c.name" :value="c.id" />
          </el-select>
        </div>
        <div class="field">
          <label>标签（可多选）</label>
          <el-select v-model="form.tagIds" multiple placeholder="选择标签" style="width: 100%">
            <el-option v-for="t in tags" :key="t.id" :label="t.name" :value="t.id" />
          </el-select>
        </div>
      </div>

      <div class="field">
        <label>封面图 <span class="req-note">（发布时必填）</span></label>
        <div class="cover-row">
          <el-upload :show-file-list="false" :http-request="uploadCover" accept="image/jpeg,image/png,image/webp">
            <el-button>上传封面</el-button>
          </el-upload>
          <img v-if="form.coverUrl" :src="form.coverUrl" class="cover-preview" alt="封面预览" />
          <span v-else class="muted">支持 jpg / png / webp，≤5MB</span>
        </div>
      </div>

      <div class="field">
        <label>正文 *</label>
        <RichEditor v-model="form.content" />
      </div>

      <div class="row2 switches">
        <div class="sw"><span>置顶</span><el-switch v-model="form.isTop" /></div>
        <div class="sw"><span>设为头条（首页轮播候选）</span><el-switch v-model="form.isHeadline" /></div>
      </div>

      <div class="actions">
        <el-button size="large" :loading="saving" @click="save(false)">存草稿</el-button>
        <el-button size="large" type="primary" class="accent-btn" :loading="saving" @click="save(true)">发 布</el-button>
        <el-button size="large" text @click="router.back()">返回</el-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.form-card { padding: 24px; max-width: 980px; }
.page-title { margin: 0 0 20px; font-size: 20px; color: var(--el-text-color-primary); }
.field { margin-bottom: 18px; }
.field label { display: block; font-size: 13px; font-weight: 600; color: var(--el-text-color-secondary); margin-bottom: 8px; }
.row2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.cover-row { display: flex; align-items: center; gap: 14px; }
.cover-preview { width: 160px; height: 90px; object-fit: cover; border-radius: 8px; border: 1px solid var(--el-border-color); }
.muted { color: var(--el-text-color-secondary); font-size: 13px; }
.switches { align-items: center; margin: 6px 0 20px; }
.sw { display: flex; align-items: center; gap: 10px; font-size: 14px; color: var(--el-text-color-primary); }
.req-note { font-weight: 400; }
.st-tag { margin-left: 10px; vertical-align: middle; }
.ai-box { margin-top: 10px; padding: 12px 14px; border: 1px dashed var(--el-border-color); border-left: 3px solid #ff5a1f; border-radius: 0 8px 8px 0; background: var(--el-fill-color-lighter); }
.ai-bar { display: flex; align-items: center; gap: 10px; }
.ai-title { font-size: 13px; font-weight: 700; color: #ff5a1f; white-space: nowrap; }
.ai-btn { margin-left: auto; }
.ai-progress { margin-top: 10px; }
.ai-stage { margin-top: 6px; font-size: 12px; display: flex; justify-content: space-between; gap: 10px; }
.ai-sec { flex: none; font-variant-numeric: tabular-nums; }
.ai-result p { margin: 10px 0 8px; font-size: 14px; line-height: 1.7; color: var(--el-text-color-primary); }
.ai-actions { display: flex; align-items: center; gap: 8px; justify-content: flex-end; }
.ai-actions .muted { margin-right: auto; font-size: 12px; }
.ai-tip { margin: 8px 0 0; font-size: 12px; }
.actions { display: flex; gap: 12px; border-top: 1px solid var(--el-border-color-light); padding-top: 20px; }
.accent-btn { background: #ff5a1f; border-color: #ff5a1f; }
@media (max-width: 767px) {
  .form-card { padding: 16px; }
  .row2 { grid-template-columns: 1fr; }
}
</style>