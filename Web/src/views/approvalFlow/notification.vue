<template>
	<div class="notification-container">
		<el-card class="full-table" shadow="hover">
			<template #header>
				<div style="display: flex; justify-content: space-between; align-items: center;">
					<span>审批通知</span>
					<el-button type="primary" size="small" @click="markAllRead">全部已读</el-button>
				</div>
			</template>
			<el-table :data="state.tableData" style="width: 100%" v-loading="state.loading" row-key="id" border>
				<el-table-column prop="type" label="类型" width="80" align="center">
					<template #default="scope">
						<el-tag :type="getTypeTag(scope.row.type)" size="small">{{ scope.row.type }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="title" label="标题" show-overflow-tooltip />
				<el-table-column prop="content" label="内容" show-overflow-tooltip />
				<el-table-column prop="isRead" label="状态" width="80" align="center">
					<template #default="scope">
						<el-tag :type="scope.row.isRead ? 'info' : 'danger'" size="small">{{ scope.row.isRead ? '已读' : '未读' }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="createTime" label="时间" width="160" align="center" />
				<el-table-column label="操作" width="120" align="center" fixed="right">
					<template #default="scope">
						<el-button v-if="!scope.row.isRead" icon="ele-Check" size="small" text type="primary" @click="markRead(scope.row)">标记已读</el-button>
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
				background small
			/>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="approvalFlowNotification">
import { onMounted, reactive } from 'vue';
import { ElMessage } from 'element-plus';

const state = reactive({
	loading: false,
	tableData: [] as any[],
	tableParams: { page: 1, pageSize: 20, total: 0 },
});

onMounted(() => { handleQuery(); });

const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await fetch('/api/approvalNotification/page', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ page: state.tableParams.page, pageSize: state.tableParams.pageSize }),
		});
		const data = await res.json();
		if (data.code === 200) { state.tableData = data.result?.items || []; state.tableParams.total = data.result?.total || 0; }
	} catch (e) { console.error(e); }
	state.loading = false;
};

const markRead = async (row: any) => {
	try {
		await fetch('/api/approvalNotification/markRead', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ id: row.id }),
		});
		row.isRead = true;
		ElMessage.success('已标记已读');
	} catch (e) { ElMessage.error('操作失败'); }
};

const markAllRead = async () => {
	try {
		await fetch('/api/approvalNotification/markAllRead', { method: 'POST' });
		ElMessage.success('全部已读');
		handleQuery();
	} catch (e) { ElMessage.error('操作失败'); }
};

const getTypeTag = (type: string) => ({ '待办': 'danger', '已办': 'success', '抄送': 'warning', '委托': 'info', '系统': '' })[type] || '';

const handleSizeChange = (val: number) => { state.tableParams.pageSize = val; handleQuery(); };
const handleCurrentChange = (val: number) => { state.tableParams.page = val; handleQuery(); };
</script>