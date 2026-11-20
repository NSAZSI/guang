<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElTable, ElTableColumn, ElButton, ElDialog, ElInput, ElInputNumber, ElMessageBox, ElMessage } from 'element-plus'
import { adminApi } from '@/api/modules'
import type { Category } from '@/types'

const list = ref<Category[]>([])
const loading = ref(false)
const dialog = ref(false)
const editId = ref(0)
const form = reactive({ name: '', slug: '', sortOrder: 0 })

async function load() {
  loading.value = true
  try { list.value = await adminApi.categories() } finally { loading.value = false }
}
onMounted(load)

function open(row?: Category) {
  editId.value = row?.id ?? 0
  Object.assign(form, { name: row?.name ?? '', slug: row?.slug ?? '', sortOrder: row?.sortOrder ?? list.value.length + 1 })
  dialog.value = true
}

async function save() {
  if (!form.name.trim() || !form.slug.trim()) { ElMessage.warning('名称与 Slug 不能为空'); return }
  if (editId.value) await adminApi.categoryUpdate(editId.value, { ...form })
  else await adminApi.categoryCreate({ ...form })
  ElMessage.success('保存成功')
  dialog.value = false
  load()
}

async function remove(row: Category) {
  await ElMessageBox.confirm(`确定删除分类「${row.name}」？`, '二次确认', { type: 'warning' })
  await adminApi.categoryRemove(row.id)
  ElMessage.success('已删除')
  load()
}
</script>

<template>
  <div>
    <div class="head">
      <h3>分类管理</h3>
      <el-button type="primary" class="accent-btn" @click="open()">+ 新建分类</el-button>
    </div>
    <div class="card" style="padding: 8px">
      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="70" />
        <el-table-column prop="name" label="名称" min-width="140" />
        <el-table-column prop="slug" label="Slug" min-width="120" />
        <el-table-column prop="sortOrder" label="排序" width="80" />
        <el-table-column label="操作" width="150">
          <template #default="{ row }">
            <el-button size="small" link type="primary" @click="open(row)">编辑</el-button>
            <el-button size="small" link type="danger" @click="remove(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog v-model="dialog" :title="editId ? '编辑分类' : '新建分类'" width="420px">
      <div class="dlg-field"><label>名称</label><el-input v-model="form.name" placeholder="如：品牌营销" /></div>
      <div class="dlg-field"><label>Slug（小写字母/数字/短横线）</label><el-input v-model="form.slug" placeholder="如：brand" /></div>
      <div class="dlg-field"><label>排序</label><el-input-number v-model="form.sortOrder" :min="0" /></div>
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
.dlg-field { margin-bottom: 14px; }
.dlg-field label { display: block; font-size: 13px; color: var(--el-text-color-secondary); margin-bottom: 6px; }
</style>