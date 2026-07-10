<template>
	<div class="sys-open-access-container">

		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysOpenAccess" :buttonConfig="openAccessButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysOpenAccess" :disableAutoQuery="true" @query="handleAdvancedQuery"
					@reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.openAccessData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="accessKey" label="身份标识" header-align="center" show-overflow-tooltip />
				<el-table-column prop="accessSecret" label="密钥" header-align="center" show-overflow-tooltip />
				<el-table-column prop="bindUserAccount" label="绑定用户账号" header-align="center" show-overflow-tooltip />
				<el-table-column prop="bindTenantName" label="绑定租户名称" header-align="center" show-overflow-tooltip />
				<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<ModifyRecord :data="scope.row" />
					</template>
				</el-table-column>
				<el-table-column label="操作" width="200" fixed="right" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-button icon="ele-Edit" size="small" text type="primary"
							@click="openEditOpenAccess(scope.row)" v-auth="'sysOpenAccess:update'"
							:disabled="scope.row.status === 1"> 编辑 </el-button>
						<el-button icon="ele-Delete" size="small" text type="danger" @click="delOpenAccess(scope.row)"
							v-auth="'sysOpenAccess:delete'" :disabled="scope.row.status === 1"> 删除 </el-button>
						<el-button size="small" text @click="openGenerateSign(scope.row)"> 生成签名 </el-button>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination v-model:currentPage="state.tableParams.page" v-model:page-size="state.tableParams.pageSize"
				:total="state.tableParams.total" :page-sizes="[10, 20, 50, 100]" size="small" background
				@size-change="handleSizeChange" @current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper" />
		</el-card>

		<EditOpenAccess ref="editOpenAccessRef" :title="state.editOpenAccessTitle"
			@handleQuery="handleAdvancedQuery(state.advancedConditions)" />
		<HelpView ref="helpViewRef" />
		<GenerateSign ref="generateSignRef" />
	</div>
</template>

<script lang="ts" setup name="sysOpenAccess">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import EditOpenAccess from '/@/views/system/openAccess/component/editOpenAccess.vue';
import HelpView from '/@/views/system/openAccess/component/helpView.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import GenerateSign from '/@/views/system/openAccess/component/generateSign.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

import { getAPI } from '/@/utils/axios-utils';
import { SysOpenAccessApi } from '/@/api-services/api';
import { OpenAccessOutput } from '/@/api-services/models';

const editOpenAccessRef = ref<InstanceType<typeof EditOpenAccess>>();
const helpViewRef = ref<InstanceType<typeof HelpView>>();
const generateSignRef = ref<InstanceType<typeof GenerateSign>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '身份标识', prop: 'accessKey', type: 'string' },
];

// 关键字搜索字段列表
const keywordFields = ['accessKey'];

// 表格选中
const handleRowClick = (row: any) => {
	const table = tableRef.value;
	if (!table) return;
	table.toggleRowSelection(row);
};

const handleSelectionChange = (rows: any[]) => {
	selectRows.value = rows;
};

const state = reactive({
	loading: false,
	openAccessData: [] as Array<OpenAccessOutput>,
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editOpenAccessTitle: '',
});

// 按钮栏配置
const openAccessButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			add: { type: 'button' as const, label: '新增', icon: 'ele-Plus', color: 'primary' as const },
			update: { type: 'button' as const, label: '修改', icon: 'ele-Edit', color: 'success' as const },
			delete: { type: 'button' as const, label: '删除', icon: 'ele-Delete', color: 'danger' as const },
		}
	},
	tool: {
		type: 'group' as const,
		childs: {
			help: { type: 'button' as const, label: '说明', icon: 'ele-QuestionFilled', color: 'default' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddOpenAccess(); break;
		case 'help': openHelp(); break;
		case 'update': handleBatchUpdate(); break;
		case 'delete': handleBatchDelete(); break;
	}
};

// 校验选中行
const validateSelection = (minCount = 1, maxCount?: number): boolean => {
	if (selectRows.value.length < minCount) {
		ElMessage.warning(`请至少选择${minCount}条记录`);
		return false;
	}
	if (maxCount && selectRows.value.length > maxCount) {
		ElMessage.warning(`最多选择${maxCount}条记录`);
		return false;
	}
	return true;
};

// 批量编辑
const handleBatchUpdate = () => {
	if (!validateSelection(1, 1)) return;
	openEditOpenAccess(selectRows.value[0]);
};

// 批量删除
const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const names = selectRows.value.map((r: any) => r.accessKey).join('、');
	ElMessageBox.confirm(`确定要删除开放接口身份「${names}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysOpenAccessApi).apiSysOpenAccessDeletePost({ id })));
		await handleAdvancedQuery(state.advancedConditions);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
};

onMounted(async () => {
	await handleAdvancedQuery([]);
});

// 高级查询（所有查询统一走此方法）
const handleAdvancedQuery = async (conditions: QueryCondition[]) => {
	state.advancedConditions = conditions;
	state.loading = true;

	const keywordValue = searchRef.value?.getKeyword?.() || '';

	let params = {
		page: state.tableParams.page,
		pageSize: state.tableParams.pageSize,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysOpenAccessApi).apiSysOpenAccessPageAdvancedPost(params as any);
		state.openAccessData = res.data.result?.items ?? [];
		state.tableParams.total = res.data.result?.total;
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

// 高级查询重置
const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

// 打开新增页面
const openAddOpenAccess = () => {
	state.editOpenAccessTitle = '添加开放接口身份';
	editOpenAccessRef.value?.openDialog({ type: 1 });
};

// 打开编辑页面
const openEditOpenAccess = (row: any) => {
	state.editOpenAccessTitle = '编辑开放接口身份';
	editOpenAccessRef.value?.openDialog(row);
};

// 删除
const delOpenAccess = (row: any) => {
	ElMessageBox.confirm(`确定删除开放接口身份：【${row.accessKey}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysOpenAccessApi).apiSysOpenAccessDeletePost({ id: row.id });
			handleAdvancedQuery(state.advancedConditions);
			ElMessage.success('删除成功');
		})
		.catch(() => { });
};

// 改变页面容量
const handleSizeChange = async (val: number) => {
	state.tableParams.pageSize = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};

// 改变页码序号
const handleCurrentChange = async (val: number) => {
	state.tableParams.page = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};

// 打开说明页面
const openHelp = () => {
	helpViewRef.value?.openDialog();
};

// 打开生成签名
const openGenerateSign = (row: any) => {
	generateSignRef.value?.openDialog(row);
};
</script>

<style scoped lang="scss">
.sys-open-access-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
