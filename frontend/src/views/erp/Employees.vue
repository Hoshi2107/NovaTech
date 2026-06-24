<template>
  <div class="card card-glass p-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h4 class="fw-bold m-0"><i class="fa-solid fa-users-gear text-cyan me-2"></i>Quản lý nhân sự & Phân vai trò (RBAC)</h4>
      <button v-if="hasPerm('Create_Employee')" class="btn btn-info rounded-pill px-4" data-bs-toggle="modal" data-bs-target="#createEmployeeModal">
        <i class="fa-solid fa-plus me-2"></i>Thêm nhân viên mới
      </button>
    </div>

    <!-- Employee List Table -->
    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border text-info" role="status"></div>
    </div>

    <div class="table-responsive" v-else>
      <table class="table table-dark table-hover mb-0 text-sm align-middle">
        <thead>
          <tr>
            <th>Họ và tên</th>
            <th>Email liên hệ</th>
            <th>Số điện thoại</th>
            <th>Vai trò / Chức vụ</th>
            <th>Trạng thái</th>
            <th class="text-end">Hành động</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="e in employees" :key="e.id">
            <td class="fw-bold text-dark">{{ e.fullName }}</td>
            <td>{{ e.email }}</td>
            <td>{{ e.phone }}</td>
            <td>
              <span v-for="r in e.roles" :key="r" class="badge bg-gradient-cyan text-dark rounded-pill px-2.5 py-1 text-xxs me-1">
                {{ r }}
              </span>
            </td>
            <td>
              <span class="badge bg-success-subtle text-success border border-success border-opacity-25 rounded-pill px-2">{{ e.status }}</span>
            </td>
            <td class="text-end">
              <div class="d-flex justify-content-end gap-2">
                <button v-if="hasPerm('Assign_Role')" class="btn btn-sm btn-outline-info text-xs rounded-pill px-3" 
                        data-bs-toggle="modal" data-bs-target="#assignModal" @click="selectEmployee(e)">
                  Phân vai trò
                </button>
                <button v-if="hasPerm('Delete_Employee')" class="btn btn-sm btn-outline-danger" @click="handleDelete(e.id)">
                  <i class="fa-solid fa-trash-can"></i>
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Assign Role Modal -->
    <div class="modal fade" id="assignModal" tabindex="-1" aria-hidden="true" v-if="selectedEmp">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content bg-white border border-light shadow text-dark">
          <form @submit.prevent="handleUpdateRoles">
            <div class="modal-header border-bottom border-light">
              <h5 class="modal-title fw-bold text-dark">Gán vai trò cho: {{ selectedEmp.fullName }}</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close" ref="closeAssignBtn"></button>
            </div>
            <div class="modal-body text-xs">
              <div class="mb-3">
                <label class="form-label text-xs text-secondary fw-semibold mb-2">Chọn một hoặc nhiều vai trò:</label>
                <div class="d-flex flex-column gap-2 text-dark">
                  <div class="form-check" v-for="r in rolesList" :key="r.roleName">
                    <input class="form-check-input" type="checkbox" :value="r.roleName" v-model="selectedEmp.roles" :id="'role-chk-' + r.roleName">
                    <label class="form-check-label text-sm text-dark" :for="'role-chk-' + r.roleName">
                      {{ r.roleName }}
                    </label>
                  </div>
                </div>
              </div>
            </div>
            <div class="modal-footer border-top border-light">
              <button type="button" class="btn btn-outline-secondary rounded-pill" data-bs-dismiss="modal">Hủy</button>
              <button type="submit" class="btn btn-info rounded-pill px-4 text-white">Lưu lại</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Create Employee Modal -->
    <div class="modal fade" id="createEmployeeModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content bg-white border border-light shadow text-dark">
          <form @submit.prevent="handleCreate">
            <div class="modal-header border-bottom border-light">
              <h5 class="modal-title fw-bold text-dark">Thêm nhân sự mới</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close" ref="closeCreateBtn"></button>
            </div>
            <div class="modal-body text-xs text-dark">
              <div class="mb-3">
                <label class="form-label text-xs text-secondary fw-semibold">Họ và tên</label>
                <input type="text" v-model="newEmp.fullName" class="form-control text-xs" required placeholder="Nguyễn Văn B">
              </div>
              <div class="mb-3">
                <label class="form-label text-xs text-secondary fw-semibold">Email</label>
                <input type="email" v-model="newEmp.email" class="form-control text-xs" required placeholder="nhanvien@novatech.vn">
              </div>
              <div class="mb-3">
                <label class="form-label text-xs text-secondary fw-semibold">Số điện thoại</label>
                <input type="text" v-model="newEmp.phone" class="form-control text-xs" required placeholder="09...">
              </div>
              <div class="mb-3">
                <label class="form-label text-xs text-secondary fw-semibold">Mật khẩu ban đầu</label>
                <input type="password" v-model="newEmp.password" class="form-control text-xs" required placeholder="123">
              </div>
              <div class="mb-3">
                <label class="form-label text-xs text-secondary fw-semibold mb-2">Phân vai trò mặc định</label>
                <div class="d-flex flex-column gap-2 mt-1">
                  <div class="form-check" v-for="r in rolesList" :key="r.roleName">
                    <input class="form-check-input" type="checkbox" :value="r.roleName" v-model="newEmp.roles" :id="'new-role-chk-' + r.roleName">
                    <label class="form-check-label text-sm text-dark" :for="'new-role-chk-' + r.roleName">
                      {{ r.roleName }}
                    </label>
                  </div>
                </div>
              </div>
            </div>
            <div class="modal-footer border-top border-light">
              <button type="button" class="btn btn-outline-secondary rounded-pill" data-bs-dismiss="modal">Đóng</button>
              <button type="submit" class="btn btn-info rounded-pill px-4 text-white">Tạo nhân viên</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { authService } from '../../services/auth'

export default {
  name: 'Employees',
  setup() {
    const employees = ref([])
    const rolesList = ref([])
    const loading = ref(true)

    const newEmp = ref({
      fullName: '',
      email: '',
      phone: '',
      password: '123',
      status: 'Đang làm việc',
      roles: []
    })

    const selectedEmp = ref(null)

    // Modal close refs
    const closeAssignBtn = ref(null)
    const closeCreateBtn = ref(null)

    const hasPerm = (p) => authService.hasPermission(p)

    const fetchEmployees = async () => {
      try {
        loading.value = true
        const response = await axios.get('/api/GetEmployees')
        employees.value = response.data
      } catch (err) {
        console.error('Error fetching employees:', err)
      } finally {
        loading.value = false
      }
    }

    const fetchRoles = async () => {
      try {
        const response = await axios.get('/api/GetRoles')
        rolesList.value = response.data
      } catch (err) {
        console.error('Error fetching roles:', err)
      }
    }

    const handleCreate = async () => {
      try {
        await axios.post('/api/CreateEmployee', newEmp.value)
        fetchEmployees()
        newEmp.value = {
          fullName: '',
          email: '',
          phone: '',
          password: '123',
          status: 'Đang làm việc',
          roles: []
        }
        closeCreateBtn.value.click()
      } catch (err) {
        console.error('Error creating employee:', err)
      }
    }

    const selectEmployee = (e) => {
      selectedEmp.value = { ...e, roles: [...e.roles] }
    }

    const handleUpdateRoles = async () => {
      try {
        await axios.post('/api/EditEmployee', selectedEmp.value)
        fetchEmployees()
        closeAssignBtn.value.click()
      } catch (err) {
        console.error('Error updating employee roles:', err)
      }
    }

    const handleDelete = async (id) => {
      if (!confirm('Xóa nhân viên khỏi hệ thống?')) return
      try {
        await axios.post('/api/DeleteEmployee', { id })
        fetchEmployees()
      } catch (err) {
        console.error('Error deleting employee:', err)
      }
    }

    onMounted(() => {
      fetchEmployees()
      fetchRoles()
    })

    return {
      employees,
      rolesList,
      loading,
      newEmp,
      selectedEmp,
      closeAssignBtn,
      closeCreateBtn,
      hasPerm,
      selectEmployee,
      handleCreate,
      handleUpdateRoles,
      handleDelete
    }
  }
}
</script>
