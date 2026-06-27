<template>
	<div class="sysLdap-container">

		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysLdap" :buttonConfig="ldapButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysLdap" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.tableData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="host" label="主机" min-width="150" show-overflow-tooltip />
				<el-table-column prop="port" label="端口" show-overflow-tooltip />
				<el-table-column prop="baseDn" label="用户搜索基准" show-overflow-tooltip />
				<el-table-column prop="bindDn" label="绑定DN" show-overflow-tooltip />
				<el-table-column prop="bindPass" label="绑定密码" min-width="200" show-overflow-tooltip />
				<el-table-column prop="authFilter" label="用户过滤规则" show-overflow-tooltip />
				<el-table-column prop="version" label="Ldap版本" show-overflow-tooltip />
				<el-table-column prop="status" label="状态" width="80" align="center" show-overflow-tooltip>
					<template #default="scope">
            <g-sys-dict v-model="scope.row.status" code="StatusEnum" />
					</template>
				</el-table-column>
				<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<ModifyRecord :data="scope.row" />
					</template>
				</el-table-column>
			</el-table>
			<el-pagination v-model:currentPage="state.tableParams.page"
				v-model:page-size="state.tableParams.pageSize" :total="state.tableParams.total"
				:page-sizes="[10, 20, 50, 100, 200, 500]" size="small" background @size-change="handleSizeChange"
				@current-change="handleCurrentChange" layout="total, sizes, prev, pager, next, jumper" />
		</el-card>
		<EditLdap ref="editLdapRef" :title="state.dialogTitle" @handleQuery="handleAdvancedQuery(state.advancedConditions)" />
	</div>
</template>

<script lang="ts" setup name="sysLdap">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { SysLdapApi, SysTenantApi } from '/@/api-services/api';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import EditLdap from './component/editLdap.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';
import { useUserInfo } from "/@/stores/userInfo";

const userStore = useUserInfo();
const editLdapRef = ref<InstanceType<typeof EditLdap>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '租户', prop: 'tenantId', type: 'select', options: [], required: true },
	{ label: '主机', prop: 'host', type: 'string' },
];

// 关键字搜索字段列表
const keywordFields = ['host', 'baseDn'];

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
	tenantList: [] as Array<any>,
	tableData: [] as any,
	queryParams: {
		tenantId: undefined as number | undefined,
	},
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	dialogTitle: '',
});

// 按钮栏配置
const ldapButtonConfig = {
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
			syncUser: { type: 'button' as const, label: '同步域账户', icon: 'ele-Refresh', color: 'primary' as const },
			syncOrg: { type: 'button' as const, label: '同步域组织', icon: 'ele-Refresh', color: 'primary' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddSysLdap(); break;
		case 'update': handleBatchUpdate(); break;
		case 'delete': handleBatchDelete(); break;
		case 'syncUser': handleBatchSyncUser(); break;
		case 'syncOrg': handleBatchSyncOrg(); break;
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
	openEditSysLdap(selectRows.value[0]);
};

// 批量删除
const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const hosts = selectRows.value.map((r: any) => r.host).join('、');
	ElMessageBox.confirm(`确定要删除域登录信息配置「${hosts}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysLdapApi).apiSysLdapDeletePost({ id })));
		await handleAdvancedQuery(state.advancedConditions);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
};

// 批量同步域账户
const handleBatchSyncUser = () => {
	if (!validateSelection(1, 1)) return;
	syncDomainUser(selectRows.value[0]);
};

// 批量同步域组织
const handleBatchSyncOrg = () => {
	if (!validateSelection(1, 1)) return;
	syncDomainOrg(selectRows.value[0]);
};

onMounted(async () => {
	if (userStore.userInfos.accountType == 999) {
		state.tenantList = await getAPI(SysTenantApi).apiSysTenantListGet().then(res => res.data.result ?? []);
		
		const tenantField = searchFields.find(f => f.prop === 'tenantId');
		if (tenantField) {
			tenantField.options = state.tenantList.map(item => ({
				label: `${item.label} (${item.host})`,
				value: item.value
			}));
		}
		
		if (state.tenantList.length > 0) {
			state.queryParams.tenantId = state.tenantList[0].value;
			if (searchRef.value) {
				searchRef.value.setQueryParams({ tenantId: state.tenantList[0].value });
			}
		}
	}
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
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysLdapApi).apiSysLdapPageAdvancedPost(params);
		state.tableData = res.data.result?.items ?? [];
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
const openAddSysLdap = () => {
	state.dialogTitle = '添加系统域登录信息配置';
	const tenantId = searchRef.value?.getQueryParams?.().find((c: any) => c.field === 'tenantId')?.value ?? state.queryParams.tenantId;
	editLdapRef.value?.openDialog({ tenantId });
};

// 打开编辑页面
const openEditSysLdap = (row: any) => {
	state.dialogTitle = '编辑系统域登录信息配置';
	editLdapRef.value?.openDialog(row);
};

// 同步域账户
const syncDomainUser = (row: any) => {
	ElMessageBox.confirm(`确定要同步域账户吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysLdapApi).apiSysLdapSyncUserPost({ id: row.id });
			await handleAdvancedQuery(state.advancedConditions);
			ElMessage.success('同步成功');
		})
		.catch(() => {});
};

// 同步域组织
const syncDomainOrg = (row: any) => {
	ElMessageBox.confirm(`确定要同步域组织架构吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysLdapApi).apiSysLdapSyncDeptPost({ id: row.id });
			await handleAdvancedQuery(state.advancedConditions);
			ElMessage.success('同步成功');
		})
		.catch(() => {});
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
.sysLdap-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
