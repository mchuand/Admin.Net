<template>
	<div class="sys-org-container">
		<splitpanes class="default-theme">
			<pane size="20">
				<OrgTree ref="orgTreeRef" @node-click="nodeClick" />
			</pane>
			<pane size="80" style="overflow: auto;">
				<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
					<template #header>
						<!-- 按钮栏组件 -->
						<ButtonBar mode="sysOrg" :buttonConfig="orgButtonConfig" displayStyle="inline"
							:onButtonClick="handleButtonClick" />

						<!-- 高级查询组件 -->
						<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
							mode="sysOrg" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
					</template>

					<el-table :data="state.orgData" style="width: 100%" v-loading="state.loading" row-key="id"
						default-expand-all :tree-props="{ children: 'children', hasChildren: 'hasChildren' }" border>
						<el-table-column prop="name" label="机构名称" min-width="160" header-align="center" show-overflow-tooltip />
						<el-table-column prop="code" label="机构编码" align="center" show-overflow-tooltip />
						<el-table-column prop="level" label="级别" width="70" align="center" show-overflow-tooltip />
						<el-table-column prop="type" label="机构类型" align="center" show-overflow-tooltip>
							<template #default="scope">
								<g-sys-dict v-model="scope.row.type" code="org_type" />
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
			</pane>
		</splitpanes>

		<EditOrg ref="editOrgRef" :title="state.editOrgTitle" :orgData="state.orgTreeData"
			@reload="handleAdvancedQuery([])" />
	</div>
</template>

<script lang="ts" setup name="sysOrg">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import { getAPI } from '/@/utils/axios-utils';
import { SysOrgApi } from '/@/api-services/api';
import { SysOrg, UpdateOrgInput } from '/@/api-services/models';
import OrgTree from '/@/views/system/org/component/orgTree.vue';
import EditOrg from '/@/views/system/org/component/editOrg.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

const editOrgRef = ref<InstanceType<typeof EditOrg>>();
const orgTreeRef = ref<InstanceType<typeof OrgTree>>();
const searchRef = ref();

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '机构名称', prop: 'name', type: 'string' },
	{ label: '机构类型', prop: 'type', type: 'dicRange', dicCode: 'org_type' },
];

// 关键字搜索字段列表
const keywordFields = ['name', 'code'];

const state = reactive({
	loading: false,
	orgData: [] as Array<SysOrg>,
	orgTreeData: [] as Array<SysOrg>,
	queryParams: {
		id: 0,
	},
	advancedConditions: [] as QueryCondition[],
	tenantId: undefined,
	editOrgTitle: '',
});

// 按钮栏配置
const orgButtonConfig = {
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
		case 'add': openAddOrg(); break;
	}
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
		id: state.queryParams.id,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysOrgApi).apiSysOrgPageAdvancedPost(params as any);
		state.orgData = res.data.result ?? [];
		// 更新编辑页面机构列表树
		if (state.queryParams.id == 0 && conditions.length == 0 && !keywordValue) {
			state.orgTreeData = state.orgData;
		}
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

// 高级查询重置
const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	state.queryParams.id = 0;
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

// 打开新增页面
const openAddOrg = () => {
	state.editOrgTitle = '添加机构';
	editOrgRef.value?.openDialog({ status: 1, orderNo: 100, tenantId: state.tenantId });
};

// 树组件点击
const nodeClick = async (node: any) => {
	state.queryParams.id = node.id;
	state.tenantId = node.tenantId;
	await handleAdvancedQuery([]);
};
</script>

<style scoped lang="scss">
.sys-org-container {
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
