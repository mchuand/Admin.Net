<template>
	<div class="labApprovalFlow-container">
		<!-- 使用通用高级查询组件 -->
		<AdvancedSearch
			ref="searchRef"
			:fields="searchFields"
			mode="approvalFlow"
			@query="handleQuery"
			@reset="handleReset"
		/>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<template #header>
				<div style="display: flex; justify-content: space-between; align-items: center;">
					<span></span>
					<el-button type="primary" icon="ele-Plus" @click="openAddApprovalFlow">新增</el-button>
				</div>
			</template>
			<el-table :data="state.tableData" style="width: 100%" v-loading="state.loading" row-key="id" border>
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="code" label="编号" width="140" show-overflow-tooltip />
				<el-table-column prop="name" label="名称" show-overflow-tooltip />
				<el-table-column prop="formJson" label="表单" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="openEditFormDialog(scope.row)"> 表单 </el-button>
					</template>
				</el-table-column>
				<el-table-column prop="flowJson" label="流程" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="openEditFlowDialog(scope.row)"> 流程 </el-button>
					</template>
				</el-table-column>
				<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<ModifyRecord :data="scope.row" />
					</template>
				</el-table-column>
				<el-table-column label="操作" width="200" align="center" fixed="right" show-overflow-tooltip>
					<template #default="scope">
						<el-button icon="ele-View" size="small" text type="primary" @click="openDetailDialog(scope.row)"> 查看 </el-button>
						<el-button icon="ele-Edit" size="small" text type="primary" @click="openEditApprovalFlow(scope.row)"> 编辑 </el-button>
						<el-button icon="ele-Delete" size="small" text type="primary" @click="delApprovalFlow(scope.row)"> 删除 </el-button>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination
				v-model:page-size="state.tableParams.pageSize"
				v-model:currentPage="state.tableParams.page"
				:page-sizes="[10, 20, 50, 100, 200, 500]"
				:total="state.tableParams.total"
				@current-change="handleCurrentChange"
				@size-change="handleSizeChange"
				layout="total, sizes, prev, pager, next, jumper"
				background
				small
			/>
		</el-card>

		<detailDialog ref="detailDialogRef" :title="state.dialogTitle" @reloadTable="handleReload" />
		<editDialog ref="editDialogRef" :title="state.dialogTitle" @reloadTable="handleReload" />
		<editFormDialog ref="editFormDialogRef" :title="state.dialogTitle" @reloadTable="handleReload" />
	</div>
</template>

<script lang="ts" setup name="approvalFlow">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

import editFormDialog from './component/editFormDialog.vue';
import detailDialog from './component/detailDialog.vue';
import editDialog from './component/editDialog.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { ApprovalFlowApi } from '/@/api-plugins/approvalFlow/api';
import { ApprovalFlowOutput } from '/@/api-plugins/approvalFlow/models';

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '关键字', prop: 'keyword', type: 'string', placeholder: '请输入模糊查询关键字' },
	{ label: '编号', prop: 'code', type: 'string' },
	{ label: '名称', prop: 'name', type: 'string' },
	{ label: '状态', prop: 'status', type: 'select', options: [
		{ label: '启用', value: 1 },
		{ label: '禁用', value: 0 },
	]},
	{ label: '备注', prop: 'remark', type: 'string' },
];

const searchRef = ref();
const detailDialogRef = ref();
const editFormDialogRef = ref();
const editDialogRef = ref();

const state = reactive({
	loading: false,
	tableData: [] as Array<ApprovalFlowOutput>,
	queryParams: {} as Record<string, any>,
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	dialogTitle: '',
});

onMounted(async () => {
	handleReload();
});

// 查询
const handleQuery = async (conditions: QueryCondition[]) => {
	state.loading = true;
console.log(conditions)
	// 将查询条件转换为参数对象
	const params: Record<string, any> = {};
	conditions.forEach((c) => {
		if (c.compare === 'between' && Array.isArray(c.value)) {
			params[`${c.field}Start`] = c.value[0];
			params[`${c.field}End`] = c.value[1];
		} else if (c.compare === 'in' && Array.isArray(c.value)) {
			params[c.field] = c.value.join(',');
		} else {
			params[c.field] = c.value;
		}
	});

	state.queryParams = params;
	let queryParams = { ...params, ...state.tableParams };
	var res = await getAPI(ApprovalFlowApi).apiApprovalFlowPagePost(queryParams);
	state.tableData = res.data.result?.items ?? [];
	state.tableParams.total = res.data.result?.total;
	state.loading = false;
};

// 重置
const handleReset = (conditions: QueryCondition[]) => {
	state.queryParams = {};
	handleReload();
};

// 重新加载
const handleReload = async () => {
	state.loading = true;
	let queryParams = { ...state.queryParams, ...state.tableParams };
	var res = await getAPI(ApprovalFlowApi).apiApprovalFlowPagePost(queryParams);
	state.tableData = res.data.result?.items ?? [];
	state.tableParams.total = res.data.result?.total;
	state.loading = false;
};

const openAddApprovalFlow = () => {
	state.dialogTitle = '添加审批流';
	editDialogRef.value.openDialog({ status: 1 });
};

const openEditApprovalFlow = (row: ApprovalFlowOutput) => {
	state.dialogTitle = '编辑审批流';
	editDialogRef.value.openDialog(row);
};

const openDetailDialog = (row: ApprovalFlowOutput) => {
	state.dialogTitle = '查看审批流';
	detailDialogRef.value.openDialog(row);
};

const openEditFormDialog = (row: ApprovalFlowOutput) => {
	state.dialogTitle = '编辑表单';
	editFormDialogRef.value.openDialog(row);
};

const openEditFlowDialog = (row: ApprovalFlowOutput) => {
	state.dialogTitle = '编辑流程';
	editFormDialogRef.value.openDialog(row);
};

const delApprovalFlow = (row: ApprovalFlowOutput) => {
	ElMessageBox.confirm(`确定要删除吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			if (row.id) {
				await getAPI(ApprovalFlowApi).apiApprovalFlowDeletePost({ id: row.id });
				handleReload();
				ElMessage.success('删除成功');
			}
		})
		.catch(() => {});
};

const handleSizeChange = (val: number) => {
	state.tableParams.pageSize = val;
	handleReload();
};

const handleCurrentChange = (val: number) => {
	state.tableParams.page = val;
	handleReload();
};
</script>

<style scoped>
:deep(.el-ipnut),
:deep(.el-select),
:deep(.el-input-number) {
	width: 100%;
}
</style>
