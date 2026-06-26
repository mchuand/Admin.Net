<template>
	<div class="approval-done-container">
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
				<el-table-column prop="nodeName" label="审批节点" width="120" show-overflow-tooltip />
				<el-table-column prop="action" label="审批结果" width="100" align="center">
					<template #default="scope">
						<el-tag :type="getActionTagType(scope.row.action)" size="small">{{ getActionText(scope.row.action) }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="comment" label="审批意见" show-overflow-tooltip />
				<el-table-column prop="handleTime" label="处理时间" width="160" align="center" />
				<el-table-column prop="createTime" label="创建时间" width="160" align="center" />
				<el-table-column label="操作" width="100" align="center" fixed="right">
					<template #default="scope">
						<el-button icon="ele-View" size="small" text type="primary" @click="handleView(scope.row)">查看</el-button>
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

		<el-dialog v-model="state.detailVisible" title="审批详情" width="700px" draggable>
			<el-timeline>
				<el-timeline-item
					v-for="item in state.timeline"
					:key="item.nodeId"
					:timestamp="item.operateTime"
					:color="getTimelineColor(item.action)"
				>
					<p><strong>{{ item.nodeName }}</strong> - {{ item.operatorName }}</p>
					<p v-if="item.comment">意见: {{ item.comment }}</p>
					<p v-if="item.action !== undefined">
						<el-tag :type="getActionTagType(item.action)" size="small">{{ getActionText(item.action) }}</el-tag>
					</p>
				</el-timeline-item>
			</el-timeline>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="approvalFlowDone">
import { onMounted, reactive } from 'vue';

const state = reactive({
	loading: false,
	tableData: [] as any[],
	queryParams: {} as any,
	tableParams: { page: 1, pageSize: 20, total: 0 },
	detailVisible: false,
	timeline: [] as any[],
});

onMounted(() => { handleQuery(); });

const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await fetch('/api/workflowInstance/myDone', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ ...state.queryParams, page: state.tableParams.page, pageSize: state.tableParams.pageSize }),
		});
		const data = await res.json();
		if (data.code === 200) { state.tableData = data.result?.items || []; state.tableParams.total = data.result?.total || 0; }
	} catch (e) { console.error(e); }
	state.loading = false;
};

const handleView = async (row: any) => {
	try {
		const res = await fetch(`/api/workflowInstance/getTimeline?instanceId=${row.instanceId}`);
		const data = await res.json();
		if (data.code === 200) { state.timeline = data.result || []; }
	} catch (e) { console.error(e); }
	state.detailVisible = true;
};

const getActionText = (action: number) => ({ 0: '提交', 1: '通过', 2: '驳回', 3: '退回', 4: '转办', 5: '委托', 7: '撤回', 8: '终止' })[action] || '未知';
const getActionTagType = (action: number) => ({ 0: 'info', 1: 'success', 2: 'danger', 3: 'warning', 4: 'warning' })[action] || 'info';
const getTimelineColor = (action: number) => ({ 0: '#409EFF', 1: '#67C23A', 2: '#F56C6C', 3: '#E6A23C', 4: '#E6A23C' })[action] || '#409EFF';

const handleSizeChange = (val: number) => { state.tableParams.pageSize = val; handleQuery(); };
const handleCurrentChange = (val: number) => { state.tableParams.page = val; handleQuery(); };
</script>