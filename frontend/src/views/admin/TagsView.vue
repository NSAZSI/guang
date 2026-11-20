<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElTable, ElTableColumn, ElButton, ElDialog, ElInput, ElMessageBox, ElMessage, ElTag } from 'element-plus'
import { adminApi } from '@/api/modules'
import type { TagItem } from '@/types'

const list = ref<TagItem[]>([])
const loading = ref(false)
const dialog = ref(false)
const editId = ref(0)
const name = ref('')

async function load() {
  loading.value = true
  try { list.value = await adminApi.tags() } finally { loading.value = false }
}
onMounted(load)

function open(row?: TagItem) {
  editId.value = row?.id ?? 0
  name.value = row?.name ?? ''
  dialog.value = true
}

async function save() {
  if (!name.value.trim()) { ElMessage.warning('标签名不能为空'); return }
  if (editId.value) await adminApi.tagUpdate(editId.value, name.value.trim())
  else await adminApi.tagCreate(name.value.trim())
  ElMessage.success('保存成功')
  dialog.value = false
  load()
}

async function remove(row: TagItem) {
  await ElMessageBox.confirm(`确定删除标签「${row.name}」？关联新闻将自动解除该标签。`, '二次确认', { type: 'warning' })
  await adminApi.tagRemove(row.id)
  ElMessage.success('已删除')
  load()
}
</script>

<template>
  <div>
    <div class="head">
      <h3>标签管理</h3>
      <el-button type="primary" class="accent-btn" @click="open()">+ 新建标签</el-button>
    </div>
    <div class="card" style="padding: 8px">
      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="70" />
        <el-table-column label="标签" min-width="160">
          <template #default="{ row }"><el-tag effect="plain" size="small">{{ row.name }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="usage" label="引用新闻数" width="110" />
        <el-table-column label="操作" width="150">
          <template #default="{ row }">
            <el-button size="small" link type="primary" @click="open(row)">编辑</el-button>
            <el-button size="small" link type="danger" @click="remove(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog v-model="dialog" :title="editId ? '编辑标签' : '新建标签'" width="380px">
      <el-input v-model="name" placeholder="标签名" @keyup.enter="save" />
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
</style>