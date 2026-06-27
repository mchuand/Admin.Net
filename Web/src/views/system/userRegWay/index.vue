<template>
	<div class="sys-user-reg-way-container">
		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysUserRegWay" :buttonConfig="regWayButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysUserRegWay" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table :data="state.regWayData" style="width: 100%" v-loading="state.loading" border>
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="name" label="名称" align="center" show-overflow-tooltip />
				<el-table-column prop="orgName" label="机构" align="center" show-overflow-tooltip />
				<el-table-column prop="roleName" label="角色" align="center" show-overflow-tooltip />
				<el-table-column prop="posName" label="职位" align="center" show-overflow-tooltip />
				<el-table-column prop="orderNo" label="排序" width="70" show-overflow-tooltip />
				<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<ModifyRecord :data="scope.row" />
					</template>
				</el-table-column>
				<el-table-column label="操作" width="200" fixed="right" align="center" show-overflow-tooltip v-if="auths(['sysUserRegWay:update', 'sysUserRegWay:delete'])">
					<template #default="scope">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="openEditRegWay(scope.row)" v-auth="'sysUserRegWay:update'"> 编辑 </el-button>
						<el-button icon="ele-Delete" size="small" text type="danger" @click="delRegWay(scope.row)" v-auth="'sysUserRegWay:delete'"> 删除 </el-button>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination v-model:currentPage="state.tableParams.page" v-model:page-size="state.tableParams.pageSize"
				:total="state.tableParams.total" :page-sizes="[10, 20, 50, 100]" size="small" background
				@size-change="handleSizeChange" @current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper" />
		</el-card>
		<EditRegWay ref="editRegWayRef" :title="state.editRegWayTitle" @handleQuery="handleAdvancedQuery(state.advancedConditions)" />
	</div>
</template>

<script lang="ts" setup name="sysUserRegWay">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { UserRegWayOutput } from '/@/api-services/models';
import { SysTenantApi, SysUserRegWayApi } from '/@/api-services/api';
import { auths } from "/@/utils/authFunction";
import { useUserInfo } from "/@/stores/userInfo";
import EditRegWay from './component/editRegWay.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

const userStore = useUserInfo();
const editRegWayRef = ref<InstanceType<typeof EditRegWay>>();
const searchRef = ref();
const state = reactive({
	loading: false,
	tenantList: [] as Array<any>,
	queryParams: {
		tenantId: undefined as number | undefined,
	},
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	regWayData: [] as Array<UserRegWayOutput>,
	editRegWayTitle: '',
});

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '租户', prop: 'tenantId', type: 'select', options: [], visible: userStore.userInfos.accountType == 999 },
	{ label: '名称', prop: 'name', type: 'string' },
];

// 关键字搜索字段列表
const keywordFields = ['name', 'orgName', 'roleName', 'posName'];

// 按钮栏配置
const regWayButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			add: { type: 'button' as const, label: '新增', icon: 'ele-Plus', color: 'primary' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddRegWay(); break;
	}
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
		let res = await getAPI(SysUserRegWayApi).apiSysUserRegWayPageAdvancedPost(params);
		state.regWayData = res.data.result?.items ?? [];
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
const openAddRegWay = () => {
	state.editRegWayTitle = '添加注册方案';
	const tenantId = searchRef.value?.getQueryParams?.().find((c: any) => c.field === 'tenantId')?.value ?? state.queryParams.tenantId;
	editRegWayRef.value?.openDialog({ tenantId, orderNo: 100 });
};

// 打开编辑页面
const openEditRegWay = (row: any) => {
	state.editRegWayTitle = '编辑注册方案';
	editRegWayRef.value?.openDialog(row);
};

// 删除
const delRegWay = (row: any) => {
	ElMessageBox.confirm(`确定删除方案：【${row.name}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		await getAPI(SysUserRegWayApi).apiSysUserRegWayDeletePost({ id: row.id });
		await handleAdvancedQuery(state.advancedConditions);
		ElMessage.success('删除成功');
	}).catch(() => { });
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
.sys-user-reg-way-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>