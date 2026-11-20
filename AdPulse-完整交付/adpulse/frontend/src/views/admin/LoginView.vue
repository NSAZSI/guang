<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElForm, ElFormItem, ElInput, ElButton, ElMessage, type FormInstance } from 'element-plus'
import { useUserStore } from '@/stores/user'

const user = useUserStore()
const route = useRoute()
const router = useRouter()
const formRef = ref<FormInstance>()
const loading = ref(false)
const form = reactive({ userName: '', password: '' })
const rules = {
  userName: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

async function submit() {
  await formRef.value?.validate()
  loading.value = true
  try {
    await user.login(form.userName, form.password)
    ElMessage.success(`欢迎回来，${user.user?.displayName}`)
    router.replace(String(route.query.redirect || '/admin'))
  } catch {
    /* 错误提示由拦截器统一处理 */
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-card">
      <div class="brand"><span class="pulse"></span> AdPulse <em>广告脉搏 · 控制台</em></div>
      <p class="sub">广告行业新闻内容管理系统</p>
      <el-form ref="formRef" :model="form" :rules="rules" size="large" @keyup.enter="submit">
        <el-form-item prop="userName">
          <el-input v-model="form.userName" placeholder="用户名" autocomplete="username" />
        </el-form-item>
        <el-form-item prop="password">
          <el-input v-model="form.password" type="password" placeholder="密码" show-password autocomplete="current-password" />
        </el-form-item>
        <el-button type="primary" class="submit" :loading="loading" @click="submit">登 录</el-button>
      </el-form>
      <div class="demo-box">
        <div class="demo-title">演示账号</div>
        <div>管理员：admin / AdPulse@2026</div>
        <div>编辑：editor / Editor@2026</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-page { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 20px; background: radial-gradient(1000px 500px at 80% -10%, rgba(255, 90, 31, 0.16), transparent), var(--bg); }
.login-card { width: 400px; max-width: 100%; background: var(--card); border: 1px solid var(--line); border-radius: 18px; padding: 36px 34px; box-shadow: var(--shadow); }
.brand { display: flex; align-items: center; gap: 10px; font-size: 22px; font-weight: 800; color: var(--text-strong); }
.brand em { font-style: normal; font-size: 13px; font-weight: 400; color: var(--muted); }
.pulse { width: 10px; height: 10px; border-radius: 50%; background: var(--accent); animation: pulse 1.8s infinite; }
@keyframes pulse { 0% { box-shadow: 0 0 0 0 rgba(255, 90, 31, 0.5); } 70% { box-shadow: 0 0 0 10px rgba(255, 90, 31, 0); } 100% { box-shadow: 0 0 0 0 rgba(255, 90, 31, 0); } }
.sub { color: var(--muted); font-size: 13px; margin: 8px 0 26px; }
.submit { width: 100%; background: var(--accent); border-color: var(--accent); font-weight: 700; letter-spacing: 6px; }
.submit:hover { background: #ff6a35; border-color: #ff6a35; }
.demo-box { margin-top: 22px; padding: 12px 14px; border-radius: 10px; background: var(--accent-soft); font-size: 12px; color: var(--muted); display: flex; flex-direction: column; gap: 4px; }
.demo-title { font-weight: 700; color: var(--accent); }
</style>