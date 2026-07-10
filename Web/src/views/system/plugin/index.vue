<template>
	<div class="sys-plugin-container">
		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<ButtonBar mode="sysPlugin" :buttonConfig="pluginButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysPlugin" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.tableData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="name" label="功能名称" header-align="center" show-overflow-tooltip />
				<el-table-column prop="assemblyName" label="程序集名称" header-align="center" show-overflow-tooltip />
				<el-table-column prop="orderNo" label="排序" width="70" align="center" show-overflow-tooltip />
				<el-table-column label="状态" width="70" align="center" show-overflow-tooltip>
					<template #default="scope">
            <g-sys-dict v-model="scope.row.status" code="StatusEnum" />
					</template>
				</el-table-column>
				<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<ModifyRecord :data="scope.row" />
					</template>
				</el-table-column>
				<el-table-column label="操作" width="140" fixed="right" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="openEditPlugin(scope.row)" v-auth="'sysPlugin:update'"> 编辑 </el-button>
						<el-button icon="ele-Delete" size="small" text type="danger" @click="delPlugin(scope.row)" v-auth="'sysPlugin:delete'"> 删除 </el-button>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination
				v-model:currentPage="state.tableParams.page"
				v-model:page-size="state.tableParams.pageSize"
				:total="state.tableParams.total"
				:page-sizes="[10, 20, 50, 100]"
				size="small"
				background
				@size-change="handleSizeChange"
				@current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper"
			/>
		</el-card>

		<EditPlugin ref="editPluginRef" :title="state.editPluginTitle" @handleQuery="handleAdvancedQuery(state.advancedConditions)" />
	</div>
</template>

<script lang="ts" setup name="sysPlugin">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import EditPlugin from '/@/views/system/plugin/component/editPlugin.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';
import { getAPI } from '/@/utils/axios-utils';
import { SysPluginApi, SysTenantApi } from '/@/api-services/api';
import { SysPlugin } from '/@/api-services/models';
import { useUserInfo } from "/@/stores/userInfo";

const userStore = useUserInfo();
const editPluginRef = ref<InstanceType<typeof EditPlugin>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

const searchFields = ref<SearchField[]>([
	{ label: '功能名称', prop: 'name', type: 'string' },
]);

const keywordFields = ['name', 'assemblyName'];

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
	tenantList: [] as Array<any>,
	tableData: [] as Array<SysPlugin>,
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editPluginTitle: '',
});

const pluginButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			add: { type: 'button' as const, label: '新增', icon: 'ele-Plus', color: 'primary' as const },
			update: { type: 'button' as const, label: '修改', icon: 'ele-Edit', color: 'success' as const },
			delete: { type: 'button' as const, label: '删除', icon: 'ele-Delete', color: 'danger' as const },
		}
	},
};

const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddPlugin(); break;
		case 'update': handleBatchUpdate(); break;
		case 'delete': handleBatchDelete(); break;
	}
};

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

const handleBatchUpdate = () => {
	if (!validateSelection(1, 1)) return;
	openEditPlugin(selectRows.value[0]);
};

const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const names = selectRows.value.map((r: any) => r.name).join('、');
	ElMessageBox.confirm(`确定删除动态插件：【${names}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysPluginApi).apiSysPluginDeletePost({ id })));
		await handleAdvancedQuery(state.advancedConditions);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
};

onMounted(async () => {
	if (userStore.userInfos.accountType == 999) {
		state.tenantList = await getAPI(SysTenantApi).apiSysTenantListGet().then(res => res.data.result ?? []);
		// 添加租户字段到高级查询
		searchFields.value = [
			{ label: '租户', prop: 'tenantId', type: 'select', options: state.tenantList.map((t: any) => ({ label: `${t.label} (${t.host})`, value: t.value })) },
			{ label: '功能名称', prop: 'name', type: 'string' },
		];
	} else {
		searchFields.value = [
			{ label: '功能名称', prop: 'name', type: 'string' },
		];
	}
	await handleAdvancedQuery([]);
});

const handleAdvancedQuery = async (conditions: QueryCondition[]) => {
	state.advancedConditions = conditions;
	state.loading = true;

	const keywordValue = searchRef.value?.getKeyword?.() || '';

	let params: any = {
		page: state.tableParams.page,
		pageSize: state.tableParams.pageSize,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions,
	};

	try {
		let res = await getAPI(SysPluginApi).apiSysPluginPageAdvancedPost(params);
		state.tableData = res.data.result?.items ?? [];
		state.tableParams.total = res.data.result?.total;
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

const openAddPlugin = () => {
	state.editPluginTitle = '添加动态插件';
	// 从高级查询条件中提取租户ID，或使用第一个租户
	let tenantId = state.tenantList[0]?.value;
	const tenantCondition = state.advancedConditions.find(c => c.field === 'tenantId');
	if (tenantCondition) {
		tenantId = tenantCondition.value;
	}
	editPluginRef.value?.openDialog({ orderNo: 100, tenantId: tenantId, status: 1 });
};

const openEditPlugin = (row: any) => {
	state.editPluginTitle = '编辑动态插件';
	editPluginRef.value?.openDialog(row);
};

const delPlugin = (row: any) => {
	ElMessageBox.confirm(`确定删除动态插件：【${row.name}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysPluginApi).apiSysPluginDeletePost({ id: row.id });
			await handleAdvancedQuery(state.advancedConditions);
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

const handleSizeChange = async (val: number) => {
	state.tableParams.pageSize = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};

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
.sys-plugin-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
