<template>
	<div class="sys-vislog-container">
		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysVislog" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table :data="state.logData" style="width: 100%" v-loading="state.loading" border>
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="displayTitle" label="显示名称" width="150" align="center" show-overflow-tooltip />
				<el-table-column prop="actionName" label="方法名称" width="150" header-align="center" show-overflow-tooltip />
				<el-table-column prop="account" label="账号名称" width="100" align="center" show-overflow-tooltip />
				<el-table-column prop="realName" label="真实姓名" width="100" align="center" show-overflow-tooltip />
				<el-table-column prop="remoteIp" label="IP地址" min-width="120" align="center" show-overflow-tooltip />
				<el-table-column prop="location" label="登录地点" min-width="150" align="center" show-overflow-tooltip />
				<el-table-column prop="longitude" label="经度" min-width="100" align="center" show-overflow-tooltip />
				<el-table-column prop="latitude" label="纬度" min-width="100" align="center" show-overflow-tooltip />
				<el-table-column prop="browser" label="浏览器" min-width="150" align="center" show-overflow-tooltip />
				<el-table-column prop="os" label="操作系统" width="120" align="center" show-overflow-tooltip />
				<el-table-column prop="status" label="状态" width="70" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-tag type="success" v-if="scope.row.status === '200'">成功</el-tag>
						<el-tag type="danger" v-else>失败</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="elapsed" label="耗时(ms)" width="90" align="center" show-overflow-tooltip />
				<el-table-column prop="logDateTime" label="日志时间" width="160" align="center" fixed="right" show-overflow-tooltip />
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
	</div>
</template>

<script lang="ts" setup name="sysVisLog">
import { onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysLogVisApi, SysTenantApi } from '/@/api-services/api';
import { SysLogVis } from '/@/api-services/models';
import { useUserInfo } from "/@/stores/userInfo";
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

const userStore = useUserInfo();
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
	logData: [] as Array<SysLogVis>,
});

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '租户', prop: 'tenantId', type: 'select', options: [], visible: userStore.userInfos.accountType == 999 },
	{ label: '日志时间', prop: 'logDateTime', type: 'datetimeRange' },
	{ label: '方法名称', prop: 'actionName', type: 'string' },
	{ label: '账号名称', prop: 'account', type: 'string' },
	{
		label: '状态', prop: 'status', type: 'select', options: [
			{ label: '成功', value: '200' },
			{ label: '失败', value: '400' },
		]
	},
	{ label: '耗时', prop: 'elapsed', type: 'number' },
	{ label: 'IP地址', prop: 'remoteIp', type: 'string' },
];

// 关键字搜索字段列表
const keywordFields = ['displayTitle', 'actionName', 'account', 'remoteIp'];

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

	let params = {
		page: state.tableParams.page,
		pageSize: state.tableParams.pageSize,
		tenantId: state.queryParams.tenantId,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysLogVisApi).apiSysLogVisPageAdvancedPost(params as any);
		state.logData = res.data.result?.items ?? [];
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
.sys-vislog-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>