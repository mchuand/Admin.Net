<template>
	<div class="delegation-container">
		<el-card class="full-table" shadow="hover">
			<template #header>
				<div style="display: flex; justify-content: space-between; align-items: center;">
					<span>委托管理（用户身后）</span>
					<el-button type="primary" icon="ele-Plus" @click="openAdd">新增委托</el-button>
				</div>
			</template>
			<el-alert title="设置委托后，在委托时间段内，被委托人将代替您处理审批任务" type="info" :closable="false" style="margin-bottom: 15px" />
			<el-table :data="state.tableData" style="width: 100%" v-loading="state.loading" row-key="id" border>
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="targetUserName" label="被委托人" width="120" />
				<el-table-column prop="workflowName" label="指定流程" width="150">
					<template #default="scope">{{ scope.row.workflowName || '所有流程' }}</template>
				</el-table-column>
				<el-table-column prop="startTime" label="开始时间" width="160" align="center" />
				<el-table-column prop="endTime" label="结束时间" width="160" align="center" />
				<el-table-column prop="status" label="状态" width="100" align="center">
					<template #default="scope">
						<el-switch v-model="scope.row.status" :active-value="1" :inactive-value="0" @change="toggleStatus(scope.row)" />
					</template>
				</el-table-column>
				<el-table-column prop="remark" label="备注" show-overflow-tooltip />
				<el-table-column label="操作" width="150" align="center" fixed="right">
					<template #default="scope">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="openEdit(scope.row)">编辑</el-button>
						<el-button icon="ele-Delete" size="small" text type="danger" @click="handleDelete(scope.row)">删除</el-button>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination
				v-model:page-size="state.tableParams.pageSize"
				v-model:currentPage="state.tableParams.page"
				:page-sizes="[10, 20, 50]"
				:total="state.tableParams.total"
				@current-change="handleCurrentChange"
				@size-change="handleSizeChange"
				layout="total, sizes, prev, pager, next, jumper"
				background small
			/>
		</el-card>

		<el-dialog v-model="state.dialogVisible" :title="state.isEdit ? '编辑委托' : '新增委托'" width="500px" draggable>
			<el-form :model="state.form" ref="formRef" label-width="100px">
				<el-form-item label="被委托人Id" prop="targetUserId" :rules="[{ required: true, message: '请输入被委托人Id' }]">
					<el-input v-model.number="state.form.targetUserId" placeholder="请输入被委托人用户Id" />
				</el-form-item>
				<el-form-item label="指定流程">
					<el-input v-model.number="state.form.workflowId" placeholder="留空表示所有流程" />
				</el-form-item>
				<el-form-item label="开始时间" prop="startTime" :rules="[{ required: true, message: '请选择开始时间' }]">
					<el-date-picker v-model="state.form.startTime" type="datetime" placeholder="选择开始时间" style="width: 100%" />
				</el-form-item>
				<el-form-item label="结束时间" prop="endTime" :rules="[{ required: true, message: '请选择结束时间' }]">
					<el-date-picker v-model="state.form.endTime" type="datetime" placeholder="选择结束时间" style="width: 100%" />
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="state.form.remark" type="textarea" :rows="3" placeholder="请输入备注" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.dialogVisible = false">取消</el-button>
				<el-button type="primary" @click="submitForm" :loading="state.submitLoading">确定</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="approvalFlowDelegation">
import { onMounted, reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';

const state = reactive({
	loading: false,
	tableData: [] as any[],
	tableParams: { page: 1, pageSize: 20, total: 0 },
	dialogVisible: false,
	isEdit: false,
	submitLoading: false,
	form: { id: 0, targetUserId: null as number | null, workflowId: null as number | null, startTime: '', endTime: '', remark: '' },
});

onMounted(() => { handleQuery(); });

const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await fetch('/api/approvalDelegation/page', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ page: state.tableParams.page, pageSize: state.tableParams.pageSize }),
		});
		const data = await res.json();
		if (data.code === 200) { state.tableData = data.result?.items || []; state.tableParams.total = data.result?.total || 0; }
	} catch (e) { console.error(e); }
	state.loading = false;
};

const openAdd = () => {
	state.isEdit = false;
	state.form = { id: 0, targetUserId: null, workflowId: null, startTime: '', endTime: '', remark: '' };
	state.dialogVisible = true;
};

const openEdit = (row: any) => {
	state.isEdit = true;
	state.form = { ...row };
	state.dialogVisible = true;
};

const submitForm = async () => {
	if (!state.form.targetUserId || !state.form.startTime || !state.form.endTime) {
		ElMessage.warning('请填写必填项');
		return;
	}
	state.submitLoading = true;
	try {
		const url = state.isEdit ? '/api/approvalDelegation/update' : '/api/approvalDelegation/add';
		const res = await fetch(url, {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify(state.form),
		});
		const data = await res.json();
		if (data.code === 200) { ElMessage.success(state.isEdit ? '编辑成功' : '新增成功'); state.dialogVisible = false; handleQuery(); }
		else { ElMessage.error(data.message || '操作失败'); }
	} catch (e) { ElMessage.error('操作失败'); }
	state.submitLoading = false;
};

const toggleStatus = async (row: any) => {
	try {
		await fetch('/api/approvalDelegation/toggleStatus', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ id: row.id }),
		});
	} catch (e) { console.error(e); }
};

const handleDelete = (row: any) => {
	ElMessageBox.confirm('确定要删除该委托规则吗？', '提示', { type: 'warning' }).then(async () => {
		try {
			const res = await fetch('/api/approvalDelegation/delete', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ id: row.id }),
			});
			const data = await res.json();
			if (data.code === 200) { ElMessage.success('删除成功'); handleQuery(); }
		} catch (e) { ElMessage.error('删除失败'); }
	}).catch(() => {});
};

const handleSizeChange = (val: number) => { state.tableParams.pageSize = val; handleQuery(); };
const handleCurrentChange = (val: number) => { state.tableParams.page = val; handleQuery(); };
</script>