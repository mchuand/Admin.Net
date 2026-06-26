<template>
	<div class="sys-role-container">

		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysRole" :buttonConfig="roleButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysRole" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.roleData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="name" label="角色名称" align="center" show-overflow-tooltip />
				<el-table-column prop="code" label="角色编码" align="center" show-overflow-tooltip />
				<el-table-column label="数据范围" align="center" show-overflow-tooltip>
					<template #default="scope">
						<g-sys-dict v-model="scope.row.dataScope" code="DataScopeEnum" />
					</template>
				</el-table-column>
				<el-table-column prop="orderNo" label="排序" width="70" align="center" show-overflow-tooltip />
				<el-table-column label="状态" width="70" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-switch v-model="scope.row.status" :active-value="1" :inactive-value="2" size="small"
							@change="changeStatus(scope.row)" v-auth="'sysRole:setStatus'" />
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
				:page-sizes="[10, 20, 50, 100]" size="small" background @size-change="handleSizeChange"
				@current-change="handleCurrentChange" layout="total, sizes, prev, pager, next, jumper" />
		</el-card>
		<EditRole ref="editRoleRef" :title="state.editRoleTitle" @handleQuery="handleAdvancedQuery([])" />
		<GrantData ref="grantDataRef" @handleQuery="handleAdvancedQuery([])" />
	</div>
</template>

<script lang="ts" setup name="sysRole">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import EditRole from '/@/views/system/role/component/editRole.vue';
import GrantData from '/@/views/system/role/component/grantData.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';
import { getAPI } from '/@/utils/axios-utils';
import { SysRoleApi, SysTenantApi } from '/@/api-services/api';
import { SysRole } from '/@/api-services/models';
import { useUserInfo } from "/@/stores/userInfo";

const userStore = useUserInfo();
const editRoleRef = ref<InstanceType<typeof EditRole>>();
const grantDataRef = ref<InstanceType<typeof GrantData>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '角色名称', prop: 'name', type: 'string' },
	{ label: '角色编码', prop: 'code', type: 'string' },
	{
		label: '状态', prop: 'status', type: 'select', options: [
			{ label: '启用', value: 1 },
			{ label: '禁用', value: 2 },
		]
	},
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
	tenantList: [] as Array<any>,
	roleData: [] as Array<SysRole>,
	queryParams: {
		tenantId: undefined as number | undefined,
	},
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editRoleTitle: '',
});

// 按钮栏配置
const roleButtonConfig = {
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
			grantDataScope: { type: 'button' as const, label: '数据范围', icon: 'ele-OfficeBuilding', color: 'warning' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddRole(); break;
		case 'update': handleBatchUpdate(); break;
		case 'delete': handleBatchDelete(); break;
		case 'grantDataScope': handleBatchGrantData(); break;
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
	openEditRole(selectRows.value[0]);
};

// 批量删除
const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const names = selectRows.value.map((r: any) => r.name).join('、');
	ElMessageBox.confirm(`确定要删除角色「${names}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysRoleApi).apiSysRoleDeletePost({ id })));
		await handleAdvancedQuery([]);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
};

// 批量数据范围
const handleBatchGrantData = () => {
	if (!validateSelection(1, 1)) return;
	grantDataRef.value?.openDialog(selectRows.value[0]);
};

onMounted(async () => {
	if (userStore.userInfos.accountType == 999) {
		state.tenantList = await getAPI(SysTenantApi).apiSysTenantListGet().then(res => res.data.result ?? []);
		state.queryParams.tenantId = state.tenantList[0].value;
	}
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
		tenantId: state.queryParams.tenantId,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysRoleApi).apiSysRolePageAdvancedPost(params as any);
		state.roleData = res.data.result?.items ?? [];
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
const openAddRole = () => {
	state.editRoleTitle = '添加角色';
	editRoleRef.value?.openDialog({ id: undefined, status: 1, tenantId: state.queryParams.tenantId, orderNo: 100 });
};

// 打开编辑页面
const openEditRole = async (row: any) => {
	state.editRoleTitle = '编辑角色';
	editRoleRef.value?.openDialog(row);
};

// 修改状态
const changeStatus = async (row: any) => {
	await getAPI(SysRoleApi)
		.apiSysRoleSetStatusPost({ id: row.id, status: row.status })
		.then(() => {
			ElMessage.success('角色状态设置成功');
		})
		.catch(() => {
			row.status = row.status == 1 ? 2 : 1;
		});
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
.sys-role-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
