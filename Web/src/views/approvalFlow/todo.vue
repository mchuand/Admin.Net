<template>
	<div class="approval-todo-container">
		<el-card shadow="hover" :body-style="{ paddingBottom: '0' }">
			<el-form :model="state.queryParams" ref="queryForm">
				<el-row :gutter="35">
					<el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10">
						<el-form-item label="标题">
							<el-input v-model="state.queryParams.title" placeholder="请输入标题" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10">
						<el-form-item>
							<el-button type="primary" icon="ele-Search" @click="handleQuery">查询</el-button>
							<el-button icon="ele-Refresh" @click="() => (state.queryParams = {})">重置</el-button>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
		</el-card>
		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<el-table :data="state.tableData" style="width: 100%" v-loading="state.loading" row-key="id" border>
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="instanceTitle" label="审批标题" show-overflow-tooltip />
				<el-table-column prop="workflowName" label="流程名称" width="150" show-overflow-tooltip />
				<el-table-column prop="nodeName" label="当前节点" width="120" show-overflow-tooltip />
				<el-table-column prop="starterName" label="发起人" width="100" align="center" />
				<el-table-column prop="delegatorName" label="委托人" width="100" align="center">
					<template #default="scope">
						<el-tag v-if="scope.row.delegatorName" type="warning" size="small">{{ scope.row.delegatorName }}</el-tag>
						<span v-else>-</span>
					</template>
				</el-table-column>
				<el-table-column prop="dueTime" label="到期时间" width="160" align="center">
					<template #default="scope">
						<span :class="{ 'text-danger': isOverdue(scope.row.dueTime) }">
							{{ scope.row.dueTime || '-' }}
						</span>
					</template>
				</el-table-column>
				<el-table-column prop="createTime" label="创建时间" width="160" align="center" />
				<el-table-column label="操作" width="280" align="center" fixed="right">
					<template #default="scope">
						<el-button icon="ele-View" size="small" text type="primary" @click="handleView(scope.row)">查看</el-button>
						<el-button icon="ele-Check" size="small" text type="success" @click="handleApprove(scope.row)">通过</el-button>
						<el-button icon="ele-Close" size="small" text type="danger" @click="handleReject(scope.row)">驳回</el-button>
						<el-button icon="ele-Switch" size="small" text type="warning" @click="handleTransfer(scope.row)">转办</el-button>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination
				v-model:page-size="state.tableParams.pageSize"
				v-model:currentPage="state.tableParams.page"
				:page-sizes="[10, 20, 50, 100]"
				:total="state.tableParams.total"
				@current-change="handleCurrentChange"
				@size-change="handleSizeChange"
				layout="total, sizes, prev, pager, next, jumper"
				background
				small
			/>
		</el-card>

		<!-- 审批弹窗 -->
		<el-dialog v-model="state.dialogVisible" :title="state.dialogTitle" width="500px" draggable>
			<el-form :model="state.actionForm" ref="actionFormRef" label-width="80px">
				<el-form-item label="审批意见">
					<el-input v-model="state.actionForm.comment" type="textarea" :rows="4" placeholder="请输入审批意见" />
				</el-form-item>
				<el-form-item v-if="state.actionForm.action === 4" label="转办人">
					<el-input v-model="state.actionForm.targetUserId" placeholder="请输入转办人用户Id" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.dialogVisible = false">取消</el-button>
				<el-button type="primary" @click="submitAction" :loading="state.submitLoading">确定</el-button>
			</template>
		</el-dialog>

		<!-- 详情弹窗 -->
		<el-dialog v-model="state.detailVisible" title="审批详情" width="700px" draggable>
			<el-descriptions :column="2" border>
				<el-descriptions-item label="审批标题">{{ state.detailData.instanceTitle }}</el-descriptions-item>
				<el-descriptions-item label="流程名称">{{ state.detailData.workflowName }}</el-descriptions-item>
				<el-descriptions-item label="当前节点">{{ state.detailData.nodeName }}</el-descriptions-item>
				<el-descriptions-item label="发起人">{{ state.detailData.starterName }}</el-descriptions-item>
			</el-descriptions>
			<el-divider content-position="left">审批时间线</el-divider>
			<el-timeline>
				<el-timeline-item
					v-for="item in state.timeline"
					:key="item.nodeId"
					:timestamp="item.operateTime"
					:color="getTimelineColor(item.action)"
				>
					<p><strong>{{ item.nodeName }}</strong> - {{ item.operatorName }}</p>
					<p v-if="item.comment">意见: {{ item.comment }}</p>
					<p v-if="item.action !== undefined && item.action !== null">
						<el-tag :type="getActionTagType(item.action)" size="small">{{ getActionText(item.action) }}</el-tag>
					</p>
				</el-timeline-item>
			</el-timeline>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="approvalFlowTodo">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';

const state = reactive({
	loading: false,
	tableData: [] as any[],
	queryParams: {} as any,
	tableParams: { page: 1, pageSize: 20, total: 0 },
	dialogVisible: false,
	dialogTitle: '',
	submitLoading: false,
	detailVisible: false,
	detailData: {} as any,
	timeline: [] as any[],
	actionForm: { taskId: 0, action: 1, comment: '', targetUserId: null as number | null, targetNodeId: '' },
});

onMounted(() => { handleQuery(); });

const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await fetch('/api/workflowInstance/myTodo', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ ...state.queryParams, page: state.tableParams.page, pageSize: state.tableParams.pageSize }),
		});
		const data = await res.json();
		if (data.code === 200) {
			state.tableData = data.result?.items || [];
			state.tableParams.total = data.result?.total || 0;
		}
	} catch (e) { console.error(e); }
	state.loading = false;
};

const handleView = async (row: any) => {
	state.detailData = row;
	try {
		const res = await fetch(`/api/workflowInstance/getTimeline?instanceId=${row.instanceId}`);
		const data = await res.json();
		if (data.code === 200) { state.timeline = data.result || []; }
	} catch (e) { console.error(e); }
	state.detailVisible = true;
};

const handleApprove = (row: any) => {
	state.actionForm = { taskId: row.id, action: 1, comment: '', targetUserId: null, targetNodeId: '' };
	state.dialogTitle = '审批通过';
	state.dialogVisible = true;
};

const handleReject = (row: any) => {
	state.actionForm = { taskId: row.id, action: 2, comment: '', targetUserId: null, targetNodeId: '' };
	state.dialogTitle = '审批驳回';
	state.dialogVisible = true;
};

const handleTransfer = (row: any) => {
	state.actionForm = { taskId: row.id, action: 4, comment: '', targetUserId: null, targetNodeId: '' };
	state.dialogTitle = '审批转办';
	state.dialogVisible = true;
};

const submitAction = async () => {
	if (state.actionForm.action === 4 && !state.actionForm.targetUserId) {
		ElMessage.warning('请输入转办人用户Id');
		return;
	}
	state.submitLoading = true;
	try {
		const res = await fetch('/api/workflowInstance/handleAction', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify(state.actionForm),
		});
		const data = await res.json();
		if (data.code === 200) {
			ElMessage.success('操作成功');
			state.dialogVisible = false;
			handleQuery();
		} else {
			ElMessage.error(data.message || '操作失败');
		}
	} catch (e) { ElMessage.error('操作失败'); }
	state.submitLoading = false;
};

const isOverdue = (dueTime: string) => dueTime && new Date(dueTime) < new Date();
const getActionText = (action: number) => ({ 0: '提交', 1: '通过', 2: '驳回', 3: '退回', 4: '转办', 5: '委托', 7: '撤回', 8: '终止' })[action] || '未知';
const getActionTagType = (action: number) => ({ 0: 'info', 1: 'success', 2: 'danger', 3: 'warning', 4: 'warning', 7: 'info', 8: 'danger' })[action] || 'info';
const getTimelineColor = (action: number) => ({ 0: '#409EFF', 1: '#67C23A', 2: '#F56C6C', 3: '#E6A23C', 4: '#E6A23C' })[action] || '#409EFF';

const handleSizeChange = (val: number) => { state.tableParams.pageSize = val; handleQuery(); };
const handleCurrentChange = (val: number) => { state.tableParams.page = val; handleQuery(); };
</script>

<style scoped>
.text-danger { color: #F56C6C; }
:deep(.el-textarea__inner) { resize: none; }
</style>