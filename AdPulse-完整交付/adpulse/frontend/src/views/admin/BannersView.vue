<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElTable, ElTableColumn, ElButton, ElDialog, ElInput, ElInputNumber, ElSelect, ElOption, ElSwitch, ElUpload, ElMessageBox, ElMessage } from 'element-plus'
import { adminApi } from '@/api/modules'
import type { AdminNewsItem, BannerAdmin } from '@/types'

const list = ref<BannerAdmin[]>([])
const newsOptions = ref<AdminNewsItem[]>([])
const loading = ref(false)
const dialog = ref(false)
const editId = ref(0)
const form = reactive({ imageUrl: '', linkNewsId: null as number | null, sortOrder: 0, isEnabled: true })

async function load() {
  loading.value = true
  try { list.value = await adminApi.banners() } finally { loading.value = false }
}
onMounted(async () => {
  load()
  const r = await adminApi.newsList({ status: 'Published', pageSize: 50 })
  newsOptions.value = r.items
})

function open(row?: BannerAdmin) {
  editId.value = row?.id ?? 0
  Object.assign(form, { imageUrl: row?.imageUrl ?? '', linkNewsId: row?.linkNewsId ?? null, sortOrder: row?.sortOrder ?? list.value.length + 1, isEnabled: row?.isEnabled ?? true })
  dialog.value = true
}

async function uploadImage(opt: { file: unknown }) {
  const { url } = await adminApi.upload(opt.file as File)
  form.imageUrl = url
}

async function save() {
  if (!form.imageUrl) { ElMessage.warning('请上传轮播图'); return }
  if (editId.value) await adminApi.bannerUpdate(editId.value, { ...form })
  else await adminApi.bannerCreate({ ...form })
  ElMessage.success('保存成功')
  dialog.value = false
  load()
}

async function toggle(row: BannerAdmin) {
  await adminApi.bannerUpdate(row.id, { imageUrl: row.imageUrl, linkNewsId: row.linkNewsId, sortOrder: row.sortOrder, isEnabled: !row.isEnabled })
  load()
}

async function remove(row: BannerAdmin) {
  await ElMessageBox.confirm('确定删除该轮播图？', '二次确认', { type: 'warning' })
  await adminApi.bannerRemove(row.id)
  ElMessage.success('已删除')
  load()
}
</script>

<template>
  <div>
    <div class="head">
      <h3>轮播图管理</h3>
      <el-button type="primary" class="accent-btn" @click="open()">+ 新建轮播</el-button>
    </div>
    <div class="card" style="padding: 8px">
      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column label="预览" width="150">
          <template #default="{ row }"><img :src="row.imageUrl" class="preview" alt="" /></template>
        </el-table-column>
        <el-table-column prop="linkNewsTitle" label="关联新闻" min-width="220" show-overflow-tooltip />
        <el-table-column prop="sortOrder" label="排序" width="70" />
        <el-table-column label="启用" width="80">
          <template #default="{ row }"><el-switch :model-value="row.isEnabled" @change="toggle(row as BannerAdmin)" /></template>
        </el-table-column>
        <el-table-column label="操作" width="140">
          <template #default="{ row }">
            <el-button size="small" link type="primary" @click="open(row as BannerAdmin)">编辑</el-button>
            <el-button size="small" link type="danger" @click="remove(row as BannerAdmin)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog v-model="dialog" :title="editId ? '编辑轮播' : '新建轮播'" width="480px">
      <div class="dlg-field">
        <label>轮播图（建议 1600×640）</label>
        <el-upload :show-file-list="false" :http-request="uploadImage" accept="image/jpeg,image/png,image/webp">
          <el-button>上传图片</el-button>
        </el-upload>
        <img v-if="form.imageUrl" :src="form.imageUrl" class="preview-lg" alt="" />
      </div>
      <div class="dlg-field">
        <label>关联新闻</label>
        <el-select v-model="form.linkNewsId" clearable filterable placeholder="选择跳转新闻" style="width: 100%">
          <el-option v-for="n in newsOptions" :key="n.id" :label="n.title" :value="n.id" />
        </el-select>
      </div>
      <div class="dlg-field"><label>排序</label><el-input-number v-model="form.sortOrder" :min="0" /></div>
      <div class="dlg-field"><label>启用</label><el-switch v-model="form.isEnabled" /></div>
      <template #footer>
        <el-button @click="dialog = false">取消</el-button>
        <el-button type="primary" class="accent-btn" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; }
.head h3 { margin: 0; color: var(--el-text-color-primary); }
.accent-btn { background: #ff5a1f; border-color: #ff5a1f; }
.preview { width: 120px; height: 48px; object-fit: cover; border-radius: 6px; }
.preview-lg { width: 100%; margin-top: 10px; border-radius: 8px; }
.dlg-field { margin-bottom: 14px; }
.dlg-field label { display: block; font-size: 13px; color: var(--el-text-color-secondary); margin-bottom: 6px; }
</style>