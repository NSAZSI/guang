<script setup lang="ts">
import { watch } from 'vue'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Image from '@tiptap/extension-image'
import Placeholder from '@tiptap/extension-placeholder'
import { ElMessage, ElMessageBox } from 'element-plus'
import { adminApi } from '@/api/modules'

const props = defineProps<{ modelValue: string }>()
const emit = defineEmits<{ (e: 'update:modelValue', v: string): void }>()

async function uploadAndInsert(file: File) {
  try {
    const { url } = await adminApi.upload(file)
    editor.value?.chain().focus().setImage({ src: url }).run()
  } catch { /* 拦截器已提示 */ }
}

const editor = useEditor({
  content: props.modelValue,
  extensions: [
    StarterKit,
    Image.configure({ inline: false }),
    Placeholder.configure({ placeholder: '输入新闻正文，支持标题、列表、引用与图片…' })
  ],
  editorProps: {
    handlePaste: (_view, event) => {
      const files = Array.from(event.clipboardData?.files ?? []).filter((f) => f.type.startsWith('image/'))
      if (files.length) { files.forEach(uploadAndInsert); return true }
      return false
    },
    handleDrop: (_view, event) => {
      const files = Array.from((event as DragEvent).dataTransfer?.files ?? []).filter((f) => f.type.startsWith('image/'))
      if (files.length) { event.preventDefault(); files.forEach(uploadAndInsert); return true }
      return false
    }
  },
  onUpdate: ({ editor: e }) => emit('update:modelValue', e.getHTML())
})

watch(() => props.modelValue, (v) => {
  if (editor.value && editor.value.getHTML() !== v) editor.value.commands.setContent(v, { emitUpdate: false })
})

function pickImage() {
  const input = document.createElement('input')
  input.type = 'file'
  input.accept = 'image/jpeg,image/png,image/webp'
  input.onchange = () => { const f = input.files?.[0]; if (f) uploadAndInsert(f) }
  input.click()
}

async function addLink() {
  const { value } = await ElMessageBox.prompt('输入链接地址', '插入链接', { inputPlaceholder: 'https://…', confirmButtonText: '插入', cancelButtonText: '取消' })
  if (value) editor.value?.chain().focus().setLink({ href: value }).run()
}

const btn = (active: boolean) => ['tool', active ? 'on' : '']
</script>

<template>
  <div class="rich-editor" v-if="editor">
    <div class="toolbar">
      <button type="button" :class="btn(editor.isActive('bold'))" title="加粗" @click="editor.chain().focus().toggleBold().run()"><b>B</b></button>
      <button type="button" :class="btn(editor.isActive('italic'))" title="斜体" @click="editor.chain().focus().toggleItalic().run()"><i>I</i></button>
      <button type="button" :class="btn(editor.isActive('heading', { level: 2 }))" title="二级标题" @click="editor.chain().focus().toggleHeading({ level: 2 }).run()">H2</button>
      <button type="button" :class="btn(editor.isActive('heading', { level: 3 }))" title="三级标题" @click="editor.chain().focus().toggleHeading({ level: 3 }).run()">H3</button>
      <button type="button" :class="btn(editor.isActive('bulletList'))" title="无序列表" @click="editor.chain().focus().toggleBulletList().run()">•≡</button>
      <button type="button" :class="btn(editor.isActive('orderedList'))" title="有序列表" @click="editor.chain().focus().toggleOrderedList().run()">1≡</button>
      <button type="button" :class="btn(editor.isActive('blockquote'))" title="引用" @click="editor.chain().focus().toggleBlockquote().run()">❝</button>
      <button type="button" class="tool" title="插入链接" @click="addLink">🔗</button>
      <button type="button" class="tool" title="上传图片" @click="pickImage">🖼</button>
      <span class="sep"></span>
      <button type="button" class="tool" title="撤销" @click="editor.chain().focus().undo().run()">↺</button>
      <button type="button" class="tool" title="重做" @click="editor.chain().focus().redo().run()">↻</button>
    </div>
    <EditorContent :editor="editor" class="content" />
    <div class="hint">图片将上传至服务器（非 base64），粘贴/拖拽图片同样自动上传</div>
  </div>
</template>

<style scoped>
.rich-editor { border: 1px solid var(--el-border-color); border-radius: 10px; overflow: hidden; background: var(--el-bg-color); }
.toolbar { display: flex; flex-wrap: wrap; gap: 4px; padding: 8px; border-bottom: 1px solid var(--el-border-color-light); background: var(--el-fill-color-light); }
.tool { min-width: 32px; height: 30px; padding: 0 8px; border: 1px solid transparent; border-radius: 6px; background: transparent; color: var(--el-text-color-primary); cursor: pointer; font-size: 13px; }
.tool:hover { background: var(--el-fill-color); }
.tool.on { background: rgba(255, 90, 31, 0.15); color: #ff5a1f; border-color: rgba(255, 90, 31, 0.4); }
.sep { width: 1px; background: var(--el-border-color-light); margin: 2px 4px; }
.content :deep(.ProseMirror) { min-height: 320px; padding: 16px 18px; outline: none; line-height: 1.9; font-size: 15px; color: var(--el-text-color-primary); }
.content :deep(.ProseMirror p) { margin: 0 0 0.9em; }
.content :deep(.ProseMirror h2), .content :deep(.ProseMirror h3) { margin: 1.2em 0 0.5em; }
.content :deep(.ProseMirror blockquote) { border-left: 3px solid #ff5a1f; padding-left: 14px; color: var(--el-text-color-secondary); margin: 1em 0; }
.content :deep(.ProseMirror img) { max-width: 100%; border-radius: 8px; }
.content :deep(.ProseMirror p.is-editor-empty:first-child::before) { content: attr(data-placeholder); float: left; color: var(--el-text-color-placeholder); pointer-events: none; height: 0; }
.hint { padding: 6px 12px; font-size: 12px; color: var(--el-text-color-secondary); border-top: 1px dashed var(--el-border-color-light); }
</style>