<template>
	<div class="sys-tenant-config-container">

		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysTenantConfig" :buttonConfig="configButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysTenantConfig" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.tableData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="name" label="配置名称" align="center" show-overflow-tooltip />
				<el-table-column prop="code" label="配置编码" align="center" show-overflow-tooltip />
				<el-table-column prop="value" label="属性值" align="center" show-overflow-tooltip />
				<el-table-column label="内置参数" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<g-sys-dict v-model="scope.row.sysFlag" code="YesNoEnum" />
					</template>
				</el-table-column>
				<el-table-column prop="groupCode" label="分组编码" align="center" show-overflow-tooltip />
				<el-table-column prop="orderNo" label="排序" width="80" align="center" show-overflow-tooltip />
				<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<ModifyRecord :data="scope.row" />
					</template>
				</el-table-column>
			</el-table>
			<el-pagination v-model:currentPage="state.tableParams.page"
				v-model:page-size="state.tableParams.pageSize" :total="state.tableParams.total"
				:page-sizes="[10, 20, 50, 100]" size="small" background @size-change="handleSizeChange"
				@current-change="handleCurrentChange" layout="total, sizes, prev, pager, next, jumper" />
		</el-card>
		<EditConfig ref="editConfigRef" :title="state.editConfigTitle" :groupList="state.groupList"
			@handleQuery="handleAdvancedQuery(state.advancedConditions)" />
	</div>
</template>

<script lang="ts" setup name="sysTenantConfig">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { SysTenantConfigApi } from '/@/api-services/api';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import EditConfig from '/@/views/system/tenantConfig/component/editConfig.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

const editConfigRef = ref<InstanceType<typeof EditConfig>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '配置名称', prop: 'name', type: 'string' },
	{ label: '配置编码', prop: 'code', type: 'string' },
	{ label: '分组编码', prop: 'groupCode', type: 'select', options: [] },
];

// 关键字搜索字段列表
const keywordFields = ['name', 'code'];

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
	tableData: [] as Array<any>,
	groupList: [] as Array<String>,
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editConfigTitle: '',
});

// 按钮栏配置
const configButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			add: { type: 'button' as const, label: '新增', icon: 'ele-Plus', color: 'primary' as const },
			update: { type: 'button' as const, label: '修改', icon: 'ele-Edit', color: 'success' as const },
			delete: { type: 'button' as const, label: '删除', icon: 'ele-Delete', color: 'danger' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddConfig(); break;
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
	openEditConfig(selectRows.value[0]);
};

// 批量删除
const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const names = selectRows.value.map((r: any) => r.name).join('、');
	ElMessageBox.confirm(`确定要删除配置「${names}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await getAPI(SysTenantConfigApi).apiSysTenantConfigBatchDeletePost(ids);
		await handleAdvancedQuery([]);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
};

// 获取分组列表
const getGroupList = async () => {
	const res = await getAPI(SysTenantConfigApi).apiSysTenantConfigGroupListGet();
	state.groupList = res.data.result ?? [];
	const options = (res.data.result ?? []).map((item: string) => ({ label: item, value: item }));
	const groupField = searchFields.find((item) => item.prop === 'groupCode');
	if (groupField) {
		groupField.options = options;
	}
};

onMounted(async () => {
	await getGroupList();
	await handleAdvancedQuery([]);
});

// 高级查询（所有查询统一走此方法）
const handleAdvancedQuery = async (conditions: QueryCondition[]) => {
	state.advancedConditions = conditions;
	state.loading = true;

	const keywordValue = searchRef.value?.getKeyword?.() || '';

	let params: any = {
		page: state.tableParams.page,
		pageSize: state.tableParams.pageSize,
		keyword: keywordValue,
	};

	try {
		let res = await getAPI(SysTenantConfigApi).apiSysTenantConfigPagePost(params);
		state.tableData = res.data.result?.items ?? [];
		state.tableParams.total = res.data.result?.total;
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

// 高级查询重置
const handleAdvancedReset = async () => {
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

// 打开新增页面
const openAddConfig = () => {
	state.editConfigTitle = '添加配置';
	editConfigRef.value?.openDialog({ sysFlag: 2, orderNo: 100 });
};

// 打开编辑页面
const openEditConfig = (row: any) => {
	state.editConfigTitle = '编辑配置';
	editConfigRef.value?.openDialog(row);
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
</script>

<style scoped lang="scss">
.sys-tenant-config-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
