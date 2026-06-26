<template>
	<div class="sys-pos-container">
		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysPos" :buttonConfig="posButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysPos" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.posData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="name" label="职位名称" align="center" show-overflow-tooltip />
				<el-table-column prop="code" label="职位编码" align="center" show-overflow-tooltip />
				<el-table-column prop="userList" label="在职人数" width="70" align="center" show-overflow-tooltip>
					<template #default="scope">{{ scope.row.userList?.length }}</template>
				</el-table-column>
				<el-table-column prop="userList" label="人员明细" width="120" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-popover placement="bottom" width="280" trigger="hover" v-if="scope.row.userList?.length">
							<template #reference>
								<el-text type="primary" class="cursor-default">
									<el-icon><ele-InfoFilled /></el-icon>人员明细
								</el-text>
							</template>
							<el-table :data="scope.row.userList" stripe border>
								<el-table-column type="index" label="序号" width="55" align="center" />
								<el-table-column prop="account" label="账号" />
								<el-table-column prop="realName" label="姓名" />
							</el-table>
						</el-popover>
					</template>
				</el-table-column>
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
			</el-table>
		</el-card>

		<EditPos ref="editPosRef" :title="state.editPosTitle" @handleQuery="handleAdvancedQuery([])" />
	</div>
</template>

<script lang="ts" setup name="sysPos">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import EditPos from '/@/views/system/pos/component/editPos.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';
import { getAPI } from '/@/utils/axios-utils';
import { SysPosApi, SysTenantApi } from '/@/api-services/api';
import { SysPos, UpdatePosInput } from '/@/api-services/models';
import { useUserInfo } from "/@/stores/userInfo";

const userStore = useUserInfo();
const editPosRef = ref<InstanceType<typeof EditPos>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '职位名称', prop: 'name', type: 'string' },
	{ label: '职位编码', prop: 'code', type: 'string' },
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
	posData: [] as Array<SysPos>,
	tenantList: [] as Array<any>,
	queryParams: {
		tenantId: undefined as number | undefined,
	},
	advancedConditions: [] as QueryCondition[],
	editPosTitle: '',
});

// 按钮栏配置
const posButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			add: { type: 'button' as const, label: '新增', icon: 'ele-Plus', color: 'primary' as const },
			update: { type: 'button' as const, label: '修改', icon: 'ele-Edit', color: 'success' as const },
			copy: { type: 'button' as const, label: '复制', icon: 'ele-CopyDocument', color: 'success' as const },
			delete: { type: 'button' as const, label: '删除', icon: 'ele-Delete', color: 'danger' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddPos(); break;
		case 'update': handleBatchUpdate(); break;
		case 'copy': handleBatchCopy(); break;
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
	openEditPos(selectRows.value[0]);
};

// 批量复制
const handleBatchCopy = () => {
	if (!validateSelection(1, 1)) return;
	openCopyMenu(selectRows.value[0]);
};

// 批量删除
const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const names = selectRows.value.map((r: any) => r.name).join('、');
	ElMessageBox.confirm(`确定要删除职位「${names}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysPosApi).apiSysPosDeletePost({ id })));
		await handleAdvancedQuery([]);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
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
		page: 1,
		pageSize: 9999,
		tenantId: state.queryParams.tenantId,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysPosApi).apiSysPosPageAdvancedPost(params as any);
		state.posData = res.data.result?.items ?? [];
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
const openAddPos = () => {
	state.editPosTitle = '添加职位';
	editPosRef.value?.openDialog({ status: 1, tenantId: state.queryParams.tenantId, orderNo: 100 });
};

// 打开编辑页面
const openEditPos = (row: any) => {
	state.editPosTitle = '编辑职位';
	editPosRef.value?.openDialog(row);
};

// 打开复制页面
const openCopyMenu = (row: any) => {
	state.editPosTitle = '复制职位';
	var copyRow = JSON.parse(JSON.stringify(row)) as UpdatePosInput;
	copyRow.id = 0;
	copyRow.name = '';
	editPosRef.value?.openDialog(copyRow);
};
</script>

<style scoped lang="scss">
.sys-pos-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
