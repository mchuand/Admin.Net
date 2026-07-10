<template>
	<div class="sys-region-container">
		<splitpanes class="default-theme">
			<pane size="20">
				<RegionTree ref="regionTreeRef" @node-click="nodeClick" />
			</pane>
			<pane size="80" style="overflow: auto;">
				<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
					<template #header>
						<!-- 按钮栏组件 -->
						<ButtonBar mode="sysRegion" :buttonConfig="regionButtonConfig" displayStyle="inline"
							:onButtonClick="handleButtonClick" />

						<!-- 高级查询组件 -->
						<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
							mode="sysRegion" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
					</template>

					<el-table ref="tableRef" :data="state.regionData" style="width: 100%" v-loading="state.loading"
						row-key="id" default-expand-all :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
						border @selection-change="handleSelectionChange" @row-click="handleRowClick">
						<el-table-column type="selection" width="55" align="center" fixed />
						<el-table-column prop="name" label="行政名称" align="center" show-overflow-tooltip />
						<el-table-column prop="code" label="行政代码" align="center" show-overflow-tooltip />
						<el-table-column prop="cityCode" label="区号" align="center" show-overflow-tooltip />
						<el-table-column prop="orderNo" label="排序" width="70" align="center" show-overflow-tooltip />
						<el-table-column prop="remark" label="备注" header-align="center" show-overflow-tooltip />
						<el-table-column label="操作" width="140" fixed="right" align="center" show-overflow-tooltip>
							<template #default="scope">
								<el-button icon="ele-Edit" size="small" text type="primary"
									@click="openEditRegion(scope.row)" v-auth="'sysRegion:update'"> 编辑 </el-button>
								<el-button icon="ele-Delete" size="small" text type="danger"
									@click="delRegion(scope.row)" v-auth="'sysRegion:delete'"> 删除 </el-button>
							</template>
						</el-table-column>
					</el-table>
				</el-card>
			</pane>
		</splitpanes>

		<EditRegion ref="editRegionRef" :title="state.editRegionTitle"
			@handleQuery="handleAdvancedQuery(state.advancedConditions)" />
	</div>
</template>

<script lang="ts" setup name="sysRegion">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage, ElNotification } from 'element-plus';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';

import RegionTree from '/@/views/system/region/component/regionTree.vue';
import EditRegion from '/@/views/system/region/component/editRegion.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

import { getAPI } from '/@/utils/axios-utils';
import { SysRegionApi } from '/@/api-services/api';
import { SysRegion } from '/@/api-services/models';

const editRegionRef = ref<InstanceType<typeof EditRegion>>();
const regionTreeRef = ref<InstanceType<typeof RegionTree>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '行政名称', prop: 'name', type: 'string' },
	{ label: '行政代码', prop: 'code', type: 'string' },
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
	regionData: [] as Array<SysRegion>,
	queryParams: {
		id: -1,
		pid: undefined as number | undefined,
	},
	advancedConditions: [] as QueryCondition[],
	editRegionTitle: '',
});

// 按钮栏配置
const regionButtonConfig = {
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
			sync: { type: 'button' as const, label: '同步统计局', icon: 'ele-Lightning', color: 'danger' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddRegion(); break;
		case 'sync': handlSync(); break;
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
	openEditRegion(selectRows.value[0]);
};

// 批量删除
const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const names = selectRows.value.map((r: any) => r.name).join('、');
	ElMessageBox.confirm(`确定要删除行政区域「${names}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysRegionApi).apiSysRegionDeletePost({ id })));
		await handleAdvancedQuery(state.advancedConditions);
		selectRows.value = [];
		regionTreeRef.value?.initTreeData();
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
		page: 1,
		pageSize: 9999,
		pid: state.queryParams.pid,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysRegionApi).apiSysRegionPageAdvancedPost(params as any);
		state.regionData = res.data.result?.items ?? [];
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

// 高级查询重置
const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	state.queryParams.pid = undefined;
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

// 打开新增页面
const openAddRegion = () => {
	state.editRegionTitle = '添加行政区域';
	editRegionRef.value?.openDialog({ orderNo: 100 });
};

// 打开编辑页面
const openEditRegion = (row: any) => {
	state.editRegionTitle = '编辑行政区域';
	editRegionRef.value?.openDialog(row);
};

// 删除
const delRegion = (row: any) => {
	ElMessageBox.confirm(`确定删除行政区域：【${row.name}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysRegionApi).apiSysRegionDeletePost({ id: row.id });
			await handleAdvancedQuery(state.advancedConditions);
			regionTreeRef.value?.initTreeData();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 树组件点击
const nodeClick = async (node: any) => {
	state.queryParams.pid = node.id;
	await handleAdvancedQuery(state.advancedConditions);
};

// 同步国家统计局操作
const handlSync = async () => {
	ElMessageBox.confirm('确认同步国家统计局行政区域数据？', '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			ElNotification({
				title: '提示',
				message: '后台努力同步中...',
				type: 'success',
				position: 'bottom-right',
			});
			await getAPI(SysRegionApi).apiSysRegionSyncPost({ timeout: 1000 * 60 * 30 });
		})
		.catch(() => {});
};
</script>

<style scoped lang="scss">
.sys-region-container {
	height: 100%;
	display: flex;
	flex-direction: row !important;
}

:deep(.splitpanes) {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}

:deep(.splitpanes__pane) {
	overflow: hidden;
	display: flex;
	flex-direction: column;
}
</style>
