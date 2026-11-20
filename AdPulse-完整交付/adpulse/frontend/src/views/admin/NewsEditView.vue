<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElInput, ElSelect, ElOption, ElSwitch, ElButton, ElUpload, ElMessage, ElTag } from 'element-plus'
import { adminApi, portalApi } from '@/api/modules'
import type { Category, TagItem } from '@/types'
import RichEditor from '@/components/RichEditor.vue'
import { aiStatusLabel } from '@/utils/news'

const route = useRoute()
const router = useRouter()
const editId = computed(() => Number(route.params.id) || 0)
const categories = ref<Category[]>([])
const tags = ref<TagItem[]>([])
const saving = ref(false)
const aiStatus = ref('None')

const form = reactive({
  title: '', summary: '', content: '', coverUrl: '', categoryId: 0,
  tagIds: [] as number[], isTop: false, isHeadline: false
})

onMounted(async () => {
  const [cats, ts] = await Promise.all([portalApi.categories(), adminApi.tags()])
  categories.value = cats
  tags.value = ts
  if (editId.value) {
    const d = await adminApi.newsDetail(editId.value)
    Object.assign(form, { title: d.title, summary: d.summary, content: d.content, coverUrl: d.coverUrl, categoryId: d.categoryId, tagIds: d.tagIds, isTop: d.isTop, isHeadline: d.isHeadline })
    aiStatus.value = d.aiStatus
  }
})

async function uploadCover(opt: { file: unknown }) {
  const { url } = await adminApi.upload(opt.file as File)
  form.coverUrl = url
}

function validate(): boolean {
  if (!form.title.trim()) { ElMessage.warning('标题不能为空'); return false }
  if (!form.summary.trim()) { ElMessage.warning('摘要不能为空'); return false }
  if (!form.content || form.content === '<p></p>') { ElMessage.warning('正文不能为空'); return false }
  if (!form.coverUrl) { ElMessage.warning('请上传封面图'); return false }
  if (!form.categoryId) { ElMessage.warning('请选择分类'); return false }
  return true
}

async function save(publish: boolean) {
  if (!validate()) return
  saving.value = true
  try {
    const payload = { ...form, title: form.title.trim(), summary: form.summary.trim(), publish }
    if (editId.value) {
      await adminApi.newsUpdate(editId.value, payload)
    } else {
      await adminApi.newsCreate(payload)
    }
    ElMessage.success(publish ? '已发布' : '草稿已保存')
    router.push('/admin/news')
  } finally { saving.value = false }
}

let aiPoll: ReturnType<typeof setInterval> | undefined
async function regenAi() {
  if (!editId.value) { ElMessage.info('请先保存新闻后再使用 AI 摘要'); return }
  await adminApi.aiSummary(editId.value)
  ElMessage.success('已触发 AI 重新生成')
  aiStatus.value = 'Pending'
  clearInterval(aiPoll)
  let n = 0
  aiPoll = setInterval(async () => {
    if (!editId.value || ++n > 20) { clearInterval(aiPoll); return }
    const d = await adminApi.newsDetail(editId.value)
    aiStatus.value = d.aiStatus
    if (d.aiStatus !== 'Pending') clearInterval(aiPoll)
  }, 3000)
}
onBeforeUnmount(() => clearInterval(aiPoll))
</script>

<template>
  <div class="edit-page">
    <div class="card form-card">
      <h2 class="page-title">{{ editId ? '编辑新闻' : '写新闻' }}</h2>

      <div class="field">
        <label>标题 *</label>
        <el-input v-model="form.title" maxlength="200" show-word-limit placeholder="输入新闻标题" size="large" />
      </div>

      <div class="field">
        <label>摘要 *</label>
        <el-input v-model="form.summary" type="textarea" :rows="2" maxlength="300" show-word-limit placeholder="一句话摘要，将显示在列表与 AI 摘要卡片兜底位置" />
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
        <label>封面图 *</label>
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
        <div class="sw ai-sw">
          <span>AI 状态：<el-tag size="small" effect="plain">{{ aiStatusLabel(aiStatus) }}</el-tag></span>
          <el-button size="small" @click="regenAi">重新生成 AI 摘要</el-button>
        </div>
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
.ai-sw { grid-column: 1 / -1; justify-content: space-between; }
.actions { display: flex; gap: 12px; border-top: 1px solid var(--el-border-color-light); padding-top: 20px; }
.accent-btn { background: #ff5a1f; border-color: #ff5a1f; }
@media (max-width: 767px) {
  .form-card { padding: 16px; }
  .row2 { grid-template-columns: 1fr; }
}
</style>